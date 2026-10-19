using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Punishes fighting. Every gunshot the player fires adds "heat". Heat drains slowly, so a single
/// quiet shot is fine but a sustained firefight is not. When heat passes the trigger, a wave of
/// reinforcements is called: the player gets a warning, then the wave appears at a spawn point
/// that is out of sight, and rushes to the sound of the fighting.
///
/// There is NO hand-made path. The reinforcements are sent to the position of the last gunshot
/// ("go and investigate that") and the A* pathfinding finds the way, through doors and around walls.
/// While the player keeps firing, the units are re-sent to the newest position every few seconds,
/// so they home in on a moving fight. They never know where the player is, only where he last fired.
///
/// One per scene (put it on an empty object). Use it on Level 2, or on both levels.
public class ReinforcementSpawner : MonoBehaviour
{
    [Serializable]
    public class Wave
    {
        [Tooltip("Enemy prefabs spawned in this wave. Repeat a prefab to get several of the same.")]
        public EnemyController[] units;
    }

    [Header("Where they come from")]
    [SerializeField] ReinforcementSpawnPoint[] spawnPoints;
    [Tooltip("Spawn points closer to the player than this are never used.")]
    [SerializeField] float minSpawnDistance = 14f;
    [Tooltip("Pick randomly among valid spawn points instead of the nearest one.")]
    [SerializeField] bool pickRandomly;

    [Header("Waves (used in order)")]
    [SerializeField] Wave[] waves;
    [Tooltip("Seconds between the warning and the units appearing: the player's chance to move.")]
    [SerializeField] float arrivalDelay = 5f;
    [Tooltip("Minimum time between one wave being called and the next.")]
    [SerializeField] float waveCooldown = 30f;
    [Tooltip("Never more than this many reinforcements alive at once.")]
    [SerializeField] int maxAlive = 6;

    [Header("What triggers them")]
    [Tooltip("Heat needed to call a wave. Each gunshot adds 1.")]
    [SerializeField] float triggerHeat = 7f;
    [SerializeField] float heatDecayPerSecond = 0.5f;
    [Tooltip("Only noises at least this loud count (gunshots do, footsteps and lockpicks do not).")]
    [SerializeField] float gunshotMinRadius = 8f;

    [Header("How they hunt")]
    [SerializeField] float rushSpeedMultiplier = 1.4f;
    [Tooltip("After the last shot, units keep being sent to the fight for this long.")]
    [SerializeField] float huntSeconds = 20f;
    [SerializeField] float retaskInterval = 2.5f;
    [SerializeField] string warningMessage = "Reinforcements incoming!";

    [Header("Debug")]
    [SerializeField] bool showDebug;

    public float Heat => heat;

    readonly List<EnemyController> active = new List<EnemyController>();
    Transform player;
    float heat;
    Vector2 lastShotPosition;
    float lastShotTime = -100f;
    float nextAllowedTime;
    float nextRetask;
    int nextWaveIndex;
    bool spawning;

    void OnEnable() => NoiseSystem.NoiseMade += OnNoise;
    void OnDisable() => NoiseSystem.NoiseMade -= OnNoise;

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            PlayerController pc = p.GetComponentInParent<PlayerController>();
            if (pc != null) player = pc.transform;
        }
        if (player == null) Debug.LogWarning("ReinforcementSpawner: no 'Player' tagged object found.", this);
        if (spawnPoints == null || spawnPoints.Length == 0)
            Debug.LogWarning("ReinforcementSpawner: no spawn points assigned.", this);
    }

    void OnNoise(Vector2 position, float radius, Team source)
    {
        if (source != Team.Player || radius < gunshotMinRadius) return;

        heat += 1f;
        lastShotPosition = position;
        lastShotTime = Time.time;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null && gm.State != MissionState.Infiltrate && gm.State != MissionState.Escape) return;

        heat = Mathf.Max(0f, heat - heatDecayPerSecond * Time.deltaTime);

        if (!spawning && nextWaveIndex < waves.Length && Time.time >= nextAllowedTime && heat >= triggerHeat)
            StartCoroutine(SpawnWave(waves[nextWaveIndex++]));

        // Keep pointing the reinforcements at the fight while it is going on.
        if (Time.time - lastShotTime < huntSeconds && Time.time >= nextRetask)
        {
            nextRetask = Time.time + retaskInterval;
            PruneDead();
            foreach (EnemyController unit in active) unit.Investigate(lastShotPosition);
        }
    }

    IEnumerator SpawnWave(Wave wave)
    {
        spawning = true;
        heat = 0f;                                   // the next wave needs fresh fighting
        nextAllowedTime = Time.time + waveCooldown;

        if (GameManager.Instance != null) GameManager.Instance.ShowHint(warningMessage);

        yield return new WaitForSeconds(arrivalDelay);

        ReinforcementSpawnPoint point = ChooseSpawnPoint();
        if (point == null)
        {
            Debug.LogWarning("ReinforcementSpawner: no usable spawn point, wave cancelled.", this);
            spawning = false;
            yield break;
        }

        PruneDead();
        List<EnemyController> spawned = new List<EnemyController>();

        foreach (EnemyController prefab in wave.units)
        {
            if (prefab == null || active.Count + spawned.Count >= maxAlive) continue;

            Vector2 pos = (Vector2)point.transform.position + UnityEngine.Random.insideUnitCircle * 0.7f;
            EnemyController unit = Instantiate(prefab, pos, Quaternion.identity);
            unit.SpeedMultiplier = rushSpeedMultiplier;
            if (point.patrolRoute != null && point.patrolRoute.Length > 0) unit.SetWaypoints(point.patrolRoute);
            if (GameManager.Instance != null) GameManager.Instance.RegisterEnemy(unit);
            spawned.Add(unit);
        }
        active.AddRange(spawned);

        // Wait one frame so every unit's own Start() has run; otherwise it would overwrite our orders.
        yield return null;
        foreach (EnemyController unit in spawned)
            if (unit != null) unit.Investigate(lastShotPosition);

        spawning = false;
    }

    /// Nearest (or random) spawn point that is far enough away and NOT currently on screen.
    ReinforcementSpawnPoint ChooseSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return null;

        Vector2 from = player != null ? (Vector2)player.position : lastShotPosition;
        List<ReinforcementSpawnPoint> valid = new List<ReinforcementSpawnPoint>();

        foreach (ReinforcementSpawnPoint sp in spawnPoints)
        {
            if (sp == null) continue;
            if (Vector2.Distance(sp.transform.position, from) < minSpawnDistance) continue;
            if (IsOnScreen(sp.transform.position)) continue;
            valid.Add(sp);
        }

        if (valid.Count > 0)
        {
            if (pickRandomly) return valid[UnityEngine.Random.Range(0, valid.Count)];

            ReinforcementSpawnPoint nearest = valid[0];
            float best = float.MaxValue;
            foreach (ReinforcementSpawnPoint sp in valid)
            {
                float d = Vector2.Distance(sp.transform.position, from);
                if (d < best) { best = d; nearest = sp; }
            }
            return nearest;
        }

        // Nothing is both far and off-screen: use the one furthest from the player.
        ReinforcementSpawnPoint farthest = null;
        float far = -1f;
        foreach (ReinforcementSpawnPoint sp in spawnPoints)
        {
            if (sp == null) continue;
            float d = Vector2.Distance(sp.transform.position, from);
            if (d > far) { far = d; farthest = sp; }
        }
        return farthest;
    }

    static bool IsOnScreen(Vector3 worldPosition)
    {
        Camera cam = Camera.main;
        if (cam == null) return false;

        Vector3 v = cam.WorldToViewportPoint(worldPosition);
        return v.z > 0f && v.x > -0.05f && v.x < 1.05f && v.y > -0.05f && v.y < 1.05f;
    }

    void PruneDead() => active.RemoveAll(e => e == null || !e.enabled);   // a dead enemy's script is disabled

    void OnGUI()
    {
        if (!showDebug) return;
        GUI.Label(new Rect(10, 150, 500, 25),
            $"Heat {heat:0.0} / {triggerHeat:0}   wave {nextWaveIndex}/{waves.Length}   alive {active.Count}   {(spawning ? "SPAWNING" : "")}");
    }
}
