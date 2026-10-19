using UnityEngine;

/// A wall camera that sweeps left and right and watches for the player.
/// It reuses the guards' VisionSensor (can I see the player?) and VisionCone (the coloured
/// cone), and implements IAlertness so the cone can show its state.
///
///   Scanning   sweeps back and forth. The cone is green.
///   Seen       the moment the player is in view it FREEZES and the cone turns red.
///              If the player is still in view after Detect Time, it locks on.
///   Locked     stays frozen, reports the player as spotted and calls nearby guards.
///              When the player leaves its view (plus a short release delay) it resumes
///              sweeping from exactly where it stopped.
///
/// The sprite must face RIGHT at rotation 0, and the object's starting Z rotation is the centre
/// of the sweep. Keep the object's origin in open floor, not inside a wall collider, or its
/// line of sight will be blocked instantly.
[RequireComponent(typeof(VisionSensor))]
public class SecurityCamera : MonoBehaviour, IAlertness
{
    [Header("Sweep")]
    [Tooltip("How far it turns either side of its starting direction.")]
    [SerializeField] float sweepHalfAngle = 45f;
    [Tooltip("Seconds for one full left-right-left cycle.")]
    [SerializeField] float sweepPeriod = 6f;
    [Tooltip("Offset 0..1 so neighbouring cameras are not in sync.")]
    [SerializeField, Range(0f, 1f)] float startPhase = 0f;

    [Header("Detection")]
    [Tooltip("Seconds the player must stay in view, after the camera freezes, before it locks on.")]
    [SerializeField] float detectTime = 0.35f;
    [Tooltip("Cameras react this many times faster while the alarm is active.")]
    [SerializeField] float alarmDetectMultiplier = 2f;
    [Tooltip("After the player leaves view, wait this long before resuming the sweep. Stops flicker at the cone edge.")]
    [SerializeField] float lockReleaseDelay = 0.75f;

    [Header("When it locks on")]
    [SerializeField] float callRadius = 14f;
    [SerializeField] float callInterval = 3f;

    [Header("Optional: can be shot")]
    [Tooltip("Tinted grey when destroyed. Needs a Health (team Enemy) and a non-trigger collider on the Enemy layer.")]
    [SerializeField] SpriteRenderer body;

    /// Only ever 0 (green) or 1 (red): the cone goes straight to red, with no yellow stage.
    public float Alertness => (locked || playerInView) ? 1f : 0f;

    public bool IsLocked => locked;

    VisionSensor sensor;
    Health health;
    Transform player;
    Health playerHealth;
    PlayerController playerCtrl;

    float baseAngle;
    float phase;
    bool playerInView;
    bool locked;
    float sightTimer;
    float releaseTimer;
    float nextCallTime;

    void Awake()
    {
        sensor = GetComponent<VisionSensor>();
        health = GetComponent<Health>(); // optional
        baseAngle = transform.eulerAngles.z;
        phase = startPhase;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            playerHealth = p.GetComponentInParent<Health>();
            if (playerHealth != null) player = playerHealth.transform;
            playerCtrl = p.GetComponentInParent<PlayerController>();
        }
        if (player == null)
            Debug.LogWarning("SecurityCamera: no 'Player' tagged object with a Health component found.", this);

        if (health != null) health.Died += OnDied;
    }

    void OnDestroy()
    {
        if (health != null) health.Died -= OnDied;
    }

    void OnDisable() => PlayerVisibility.Clear(this);

    void Update()
    {
        bool playerAlive = player != null && playerHealth != null && !playerHealth.IsDead;
        bool hidden = playerCtrl != null && playerCtrl.IsHidden;   // a hidden player cannot be seen
        playerInView = playerAlive && !hidden && sensor.CanSee(player.position);
        PlayerVisibility.Set(this, playerInView);

        if (locked) UpdateLocked();
        else UpdateScanning();

        // Hold still while the player is in view or the camera is locked on. Otherwise sweep.
        bool holding = locked || playerInView;
        if (!holding) phase += Time.deltaTime / sweepPeriod;

        ApplyAngle();
    }

    void UpdateScanning()
    {
        if (!playerInView)
        {
            sightTimer = 0f;
            return;
        }

        float needed = detectTime / (AlarmSystem.Active ? alarmDetectMultiplier : 1f);
        sightTimer += Time.deltaTime;

        if (sightTimer >= needed) LockOn();
    }

    void UpdateLocked()
    {
        if (playerInView)
        {
            releaseTimer = 0f;
            OnTargetVisible();
            return;
        }

        releaseTimer += Time.deltaTime;
        if (releaseTimer >= lockReleaseDelay) Release();
    }

    void LockOn()
    {
        locked = true;
        releaseTimer = 0f;
        nextCallTime = 0f; // call guards immediately
    }

    void Release()
    {
        locked = false;
        sightTimer = 0f;
        // 'phase' was frozen while locked, so the sweep continues from exactly where it stopped.
    }

    /// Runs every frame while locked on AND the player is still visible.
    void OnTargetVisible()
    {
        // Safe to call repeatedly: it only ever sets a flag.
        if (GameManager.Instance != null) GameManager.Instance.ReportSpotted();

        if (callRadius > 0f && Time.time >= nextCallTime)
        {
            SquadCall.Make(transform.position, player.position, callRadius, gameObject);
            nextCallTime = Time.time + callInterval;
        }
    }

    void ApplyAngle()
    {
        // Sine sweep: slows naturally at each end, so the timing is easy to learn.
        float z = baseAngle + Mathf.Sin(phase * 2f * Mathf.PI) * sweepHalfAngle;
        transform.rotation = Quaternion.Euler(0f, 0f, z);
    }

    void OnDied()
    {
        if (body != null) body.color = new Color(0.35f, 0.35f, 0.35f);

        VisionCone cone = GetComponentInChildren<VisionCone>();
        if (cone != null) cone.gameObject.SetActive(false);

        enabled = false;
    }

    void OnDrawGizmosSelected()
    {
        // Shows the sweep range in the Scene view.
        float centre = Application.isPlaying ? baseAngle : transform.eulerAngles.z;
        Gizmos.color = Color.magenta;
        Vector3 p = transform.position;
        Gizmos.DrawLine(p, p + Quaternion.Euler(0, 0, centre + sweepHalfAngle) * Vector3.right * 3f);
        Gizmos.DrawLine(p, p + Quaternion.Euler(0, 0, centre - sweepHalfAngle) * Vector3.right * 3f);
    }
}
