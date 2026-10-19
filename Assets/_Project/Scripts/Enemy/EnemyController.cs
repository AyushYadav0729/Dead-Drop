using System;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyState { Patrol, Suspicious, Chase, Search }

/// Enemy brain: a small state machine.
///
///   Patrol      walk between waypoints.
///   Suspicious  stop and stare at where something was seen (detection meter building).
///   Chase       go to where the player was last seen; stop and shoot when in range.
///   Search      walk to a point of interest (last seen spot, gunshot, alarm origin),
///               look around, then go back to Patrol.
///
/// Normal mode: a 0..1 detection meter fills while the player is visible (faster when close).
/// High alert (after the alarm): the cone is permanently red, enemies are faster and shoot
/// faster, and spotting the player takes only a short reaction time. They do NOT know where
/// the player is: they go to the alarm origin, then to wherever they last saw him.
[RequireComponent(typeof(Rigidbody2D), typeof(Health), typeof(VisionSensor))]
public class EnemyController : MonoBehaviour, IAlertness
{
    [Header("Patrol")]
    [Tooltip("Place these in the scene, NOT as children of the enemy.")]
    [SerializeField] Transform[] waypoints;
    [SerializeField] float patrolSpeed = 2f;
    [SerializeField] float waitAtWaypoint = 1f;

    [Header("Detection (normal mode)")]
    [Tooltip("Seconds to fully spot the player at the very edge of vision. Closer = faster.")]
    [SerializeField] float detectTime = 1.5f;
    [SerializeField, Range(0.1f, 0.9f)] float suspiciousThreshold = 0.35f;
    [SerializeField] float drainPerSecond = 0.4f;

    [Header("Chase and attack")]
    [SerializeField] float chaseSpeed = 3.5f;
    [Tooltip("Inside this distance (with line of sight) the enemy stops and shoots.")]
    [SerializeField] float attackRange = 5f;
    [SerializeField] float loseSightTime = 1.5f;
    [SerializeField] float aimToleranceDegrees = 10f;

    [Header("Search")]
    [SerializeField] float searchSpeed = 2.8f;
    [SerializeField] float searchLookTime = 4f;
    [Tooltip("Give up walking to the search point after this many seconds.")]
    [SerializeField] float searchTimeout = 8f;

    [Header("Squad call")]
    [Tooltip("When this enemy sees the player, allies within this distance go to that spot. 0 = off.")]
    [SerializeField] float squadCallRadius = 10f;
    [SerializeField] float squadCallInterval = 3f;

    [Header("High alert (after the file is taken)")]
    [SerializeField] float alarmSpeedMultiplier = 1.4f;
    [SerializeField] float alarmFireRateMultiplier = 1.5f;
    [Tooltip("Seconds the player must stay in view before the enemy reacts.")]
    [SerializeField] float alarmReactionTime = 0.3f;

    [Header("References")]
    [SerializeField] float turnSpeed = 360f;
    [SerializeField] Weapon weapon;
    [SerializeField] SpriteRenderer body;
    [Tooltip("Optional '!' object shown briefly when this enemy starts chasing. Can be a child; it stays upright.")]
    [SerializeField] GameObject spottedIcon;
    [SerializeField] float iconSeconds = 1f;
    [SerializeField] float iconHeight = 0.9f;

    public EnemyState State { get; private set; }

    /// Extra speed applied on top of everything else. Reinforcements use it to arrive quickly.
    public float SpeedMultiplier { get; set; } = 1f;

    /// 0..1, drives the vision cone colour. Always 1 (red) during the alarm.
    public float Alertness => AlarmSystem.Active ? 1f : meter;

    public event Action<EnemyState, EnemyState> StateChanged; // (previous, next)

    /// Every living enemy in the scene. HidingSpot uses it to see who is searching nearby.
    public static readonly List<EnemyController> All = new List<EnemyController>();

    Rigidbody2D rb;
    Health health;
    VisionSensor sensor;
    PathFollower pathFollower;
    Transform player;
    Health playerHealth;
    PlayerController playerCtrl;

    Vector2 desiredVelocity;
    Vector2 lastKnownPosition;
    float lastSeenTime;
    float meter;           // internal detection meter (normal mode only)
    float sightTimer;      // how long the player has been in view (high alert only)
    float nextCallTime;
    float iconHideTime;

    int patrolIndex;
    bool waiting;
    float waitUntil;

    bool searchGoing;
    float searchStartTime;
    float lookStartTime;
    float lookBaseAngle;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        sensor = GetComponent<VisionSensor>();
        pathFollower = GetComponent<PathFollower>(); // optional: straight-line movement without it

        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            playerHealth = p.GetComponentInParent<Health>();
            if (playerHealth != null) player = playerHealth.transform;
            playerCtrl = p.GetComponentInParent<PlayerController>();
        }
        if (player == null)
            Debug.LogWarning("EnemyController: no 'Player' tagged object with a Health component found.", this);

        if (spottedIcon != null) spottedIcon.SetActive(false);
    }

    void OnEnable()
    {
        All.Add(this);
        NoiseSystem.NoiseMade += OnNoise;
        AlarmSystem.Raised += OnAlarm;
        SquadCall.Called += OnSquadCall;
        health.Damaged += OnDamaged;
        health.Died += OnDied;
    }

    void OnDisable()
    {
        All.Remove(this);
        PlayerVisibility.Clear(this);
        NoiseSystem.NoiseMade -= OnNoise;
        AlarmSystem.Raised -= OnAlarm;
        SquadCall.Called -= OnSquadCall;
        if (health != null)
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }
    }

    void Start()
    {
        if (AlarmSystem.Active) OnAlarm();
    }

    void Update()
    {
        desiredVelocity = Vector2.zero;
        UpdateIcon();

        bool playerAlive = player != null && playerHealth != null && !playerHealth.IsDead;
        // A hidden player cannot be seen at all.
        bool hidden = playerCtrl != null && playerCtrl.IsHidden;
        bool sees = playerAlive && !hidden && sensor.CanSee(player.position);
        PlayerVisibility.Set(this, sees);

        if (sees)
        {
            lastKnownPosition = player.position;
            lastSeenTime = Time.time;
        }

        if (AlarmSystem.Active) UpdateHighAlertDetection(sees);
        else UpdateMeter(sees);

        switch (State)
        {
            case EnemyState.Patrol: DoPatrol(); break;
            case EnemyState.Suspicious: DoSuspicious(sees); break;
            case EnemyState.Chase: DoChase(sees, playerAlive); break;
            case EnemyState.Search: DoSearch(); break;
        }
    }

    void FixedUpdate() => SetVelocity(desiredVelocity);

    // ---------------------------------------------------------------- detection

    /// Normal mode: the green -> yellow -> red meter.
    void UpdateMeter(bool sees)
    {
        if (sees)
        {
            float dist01 = Mathf.Clamp01(Vector2.Distance(transform.position, player.position) / sensor.ViewDistance);
            float rate = Mathf.Lerp(2.5f, 1f, dist01) / detectTime; // point blank = 2.5x faster
            meter = Mathf.Min(1f, meter + rate * Time.deltaTime);
        }
        else if (State != EnemyState.Chase)
        {
            float floor = State == EnemyState.Search ? 0.5f : 0f;
            float drain = drainPerSecond * (State == EnemyState.Suspicious ? 0.5f : 1f);
            meter = Mathf.MoveTowards(meter, floor, drain * Time.deltaTime);
        }

        if (meter >= 1f && State != EnemyState.Chase) Enter(EnemyState.Chase);
    }

    /// High alert: no meter. Stay in view for a short reaction time and they start chasing.
    void UpdateHighAlertDetection(bool sees)
    {
        if (!sees) { sightTimer = 0f; return; }

        sightTimer += Time.deltaTime;
        if (sightTimer >= alarmReactionTime && State != EnemyState.Chase)
            Enter(EnemyState.Chase);
    }

    // ---------------------------------------------------------------- states

    void DoPatrol()
    {
        if (meter >= suspiciousThreshold) { Enter(EnemyState.Suspicious); return; }
        if (waypoints == null || waypoints.Length == 0) return;

        if (waiting)
        {
            if (Time.time >= waitUntil)
            {
                waiting = false;
                patrolIndex = (patrolIndex + 1) % waypoints.Length;
            }
            return;
        }

        Vector2 target = waypoints[patrolIndex].position;
        MoveToward(target, patrolSpeed);

        if (Vector2.Distance(transform.position, target) < 0.15f)
        {
            waiting = true;
            float wait = AlarmSystem.Active ? waitAtWaypoint * 0.25f : waitAtWaypoint; // tighter patrols
            waitUntil = Time.time + wait;
        }
    }

    void DoSuspicious(bool sees)
    {
        FaceTowards(lastKnownPosition);
        if (!sees && meter <= 0.05f) Enter(EnemyState.Patrol);
    }

    void DoChase(bool sees, bool playerAlive)
    {
        if (!playerAlive) return;

        float distToPlayer = Vector2.Distance(transform.position, player.position);

        // Tell nearby allies where the player is (not where he will be).
        if (sees && squadCallRadius > 0f && Time.time >= nextCallTime)
        {
            SquadCall.Make(transform.position, player.position, squadCallRadius, gameObject);
            nextCallTime = Time.time + squadCallInterval;
        }

        // In range and can see: stand still and shoot.
        if (sees && distToPlayer <= attackRange)
        {
            FaceTowards(player.position);
            if (AimedAt(player.position)) weapon.TryFire();
            return;
        }

        // Otherwise go to the last place the player was seen. (A* handles walls.)
        MoveToward(lastKnownPosition, chaseSpeed);

        if (!sees && Time.time - lastSeenTime >= loseSightTime)
            Enter(EnemyState.Search);
    }

    void DoSearch()
    {
        if (searchGoing)
        {
            MoveToward(lastKnownPosition, searchSpeed);

            bool arrived = Vector2.Distance(transform.position, lastKnownPosition) < 0.4f;
            bool timedOut = Time.time - searchStartTime > searchTimeout;
            if (arrived || timedOut)
            {
                searchGoing = false;
                lookStartTime = Time.time;
                lookBaseAngle = transform.eulerAngles.z;
            }
        }
        else
        {
            float sweep = Mathf.Sin((Time.time - lookStartTime) * 2f) * 70f;
            RotateTowardsAngle(lookBaseAngle + sweep);

            if (Time.time - lookStartTime >= searchLookTime) Enter(EnemyState.Patrol);
        }
    }

    void Enter(EnemyState next)
    {
        EnemyState previous = State;
        State = next;

        switch (next)
        {
            case EnemyState.Patrol:
                meter = 0f;
                waiting = false;
                patrolIndex = NearestWaypoint();
                break;

            case EnemyState.Search:
                searchGoing = true;
                searchStartTime = Time.time;
                break;

            case EnemyState.Chase:
                meter = 1f;
                lastSeenTime = Time.time;
                nextCallTime = 0f; // call allies immediately
                if (previous != EnemyState.Chase && spottedIcon != null)
                {
                    spottedIcon.SetActive(true);
                    iconHideTime = Time.time + iconSeconds;
                }
                break;
        }

        StateChanged?.Invoke(previous, next);
    }

    // ---------------------------------------------------------------- reactions

    void OnNoise(Vector2 position, float radius, Team source)
    {
        if (source == health.team || health.IsDead) return;
        if (State == EnemyState.Chase) return;
        if (Vector2.Distance(transform.position, position) > radius) return;

        lastKnownPosition = position;
        meter = Mathf.Max(meter, 0.5f);
        Enter(EnemyState.Search); // go and investigate the gunshot
    }

    void OnSquadCall(Vector2 callerPosition, Vector2 target, float radius, GameObject caller)
    {
        if (caller == gameObject || health.IsDead || State == EnemyState.Chase) return;
        if (Vector2.Distance(transform.position, callerPosition) > radius) return;

        lastKnownPosition = target;
        meter = Mathf.Max(meter, 0.5f);
        Enter(EnemyState.Search);
    }

    void OnDamaged(float amount, Vector2 from)
    {
        if (health.IsDead || State == EnemyState.Chase) return;
        lastKnownPosition = from;   // turn on whoever shot us
        Enter(EnemyState.Chase);
    }

    void OnAlarm()
    {
        if (health.IsDead) return;

        if (weapon != null) weapon.FireRateMultiplier = alarmFireRateMultiplier;
        meter = 0f;
        sightTimer = 0f;

        // Everyone heads to where the file was taken. They do not know where the player is now.
        if (State != EnemyState.Chase)
        {
            lastKnownPosition = AlarmSystem.Origin;
            Enter(EnemyState.Search);
        }
    }

    /// Sends this enemy to go and check a spot, like hearing a gunshot there. Ignored if it is already
    /// chasing. Used by reinforcements, which are not told where the player is, only where the fight is.
    public void Investigate(Vector2 position)
    {
        if (health.IsDead || State == EnemyState.Chase) return;
        lastKnownPosition = position;
        meter = Mathf.Max(meter, 0.5f);
        Enter(EnemyState.Search);
    }

    /// Gives a spawned enemy a patrol route to follow once it has finished searching.
    public void SetWaypoints(Transform[] points)
    {
        waypoints = points;
        patrolIndex = NearestWaypoint();
    }

    /// Another system (a hiding spot that was searched) tells this enemy exactly where the player is.
    public void AlertTo(Vector2 position)
    {
        if (health.IsDead) return;
        lastKnownPosition = position;
        lastSeenTime = Time.time;
        if (State != EnemyState.Chase) Enter(EnemyState.Chase);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => All.Clear();

    void OnDied()
    {
        desiredVelocity = Vector2.zero;
        SetVelocity(Vector2.zero);
        rb.simulated = false;                       // corpse no longer blocks or gets hit
        if (body != null) body.color = new Color(0.35f, 0.35f, 0.35f);
        if (spottedIcon != null) spottedIcon.SetActive(false);

        VisionCone cone = GetComponentInChildren<VisionCone>();
        if (cone != null) cone.gameObject.SetActive(false);

        enabled = false;
    }

    // ---------------------------------------------------------------- movement helpers
    // All movement goes through MoveToward, so pathfinding lives in one place.

    void MoveToward(Vector2 target, float speed)
    {
        Vector2 steer = pathFollower != null ? pathFollower.NextPoint(target) : target;

        Vector2 to = steer - (Vector2)transform.position;
        if (to.sqrMagnitude < 0.0001f) return;

        float multiplier = (AlarmSystem.Active ? alarmSpeedMultiplier : 1f) * SpeedMultiplier;
        desiredVelocity = to.normalized * speed * multiplier;
        FaceTowards(steer);
    }

    void FaceTowards(Vector2 point)
    {
        Vector2 to = point - (Vector2)transform.position;
        RotateTowardsAngle(Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg);
    }

    void RotateTowardsAngle(float targetAngle)
    {
        float z = Mathf.MoveTowardsAngle(transform.eulerAngles.z, targetAngle, turnSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, z);
    }

    bool AimedAt(Vector2 point)
    {
        Vector2 to = point - (Vector2)transform.position;
        float target = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;
        return Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.z, target)) <= aimToleranceDegrees;
    }

    int NearestWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return 0;

        int best = 0;
        float bestDist = float.MaxValue;
        for (int i = 0; i < waypoints.Length; i++)
        {
            float d = Vector2.Distance(transform.position, waypoints[i].position);
            if (d < bestDist) { bestDist = d; best = i; }
        }
        return best;
    }

    void UpdateIcon()
    {
        if (spottedIcon == null || !spottedIcon.activeSelf) return;

        if (Time.time >= iconHideTime) { spottedIcon.SetActive(false); return; }

        // Keep the icon above the enemy's head and upright, even though the enemy rotates.
        Transform t = spottedIcon.transform;
        t.position = transform.position + Vector3.up * iconHeight;
        t.rotation = Quaternion.identity;
    }

    void SetVelocity(Vector2 v)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = v;
#else
        rb.velocity = v;
#endif
    }

    void OnDrawGizmosSelected()
    {
        if (waypoints == null) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            Gizmos.DrawWireSphere(waypoints[i].position, 0.2f);
            Transform next = waypoints[(i + 1) % waypoints.Length];
            if (next != null) Gizmos.DrawLine(waypoints[i].position, next.position);
        }
    }
}
