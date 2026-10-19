using System.Collections.Generic;
using UnityEngine;

/// Add exactly one instance to each playable level. Duplicate instances disable themselves.
public class LevelAudioFeedback : MonoBehaviour
{
    public static LevelAudioFeedback Instance { get; private set; }

    [Header("Footsteps")]
    [SerializeField] float walkingFootstepInterval = 0.52f;
    [SerializeField] float runningFootstepInterval = 0.38f;
    [SerializeField] float movementThreshold = 0.0004f;

    PlayerController player;
    Health playerHealth;
    Weapon subscribedPlayerWeapon;
    bool wasReloading;
    bool lastFootstepModeWasRunning;
    bool hasFootstepMode;
    float nextFootstepTime;
    Rigidbody2D playerBody;
    float nextEnemyWeaponScanTime;
    Vector3 lastPlayerPosition;
    readonly Dictionary<Weapon, System.Action> enemyWeaponHandlers = new Dictionary<Weapon, System.Action>();
    readonly Dictionary<Door, bool> doorStates = new Dictionary<Door, bool>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate LevelAudioFeedback found; disabling the extra component.", this);
            enabled = false;
            Destroy(this);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.GetComponentInParent<PlayerController>();
            playerHealth = playerObject.GetComponentInParent<Health>();
            playerBody = playerObject.GetComponentInParent<Rigidbody2D>();
            lastPlayerPosition = playerObject.transform.position;
            if (player != null) player.Dashed += OnDash;
            if (playerHealth != null)
            {
                playerHealth.Damaged += OnPlayerDamaged;
                playerHealth.Healed += OnPlayerHealed;
                playerHealth.Died += OnPlayerDied;
            }
        }

        AlarmSystem.Raised += OnAlarmRaised;
        if (GameManager.Instance != null) GameManager.Instance.StateChanged += OnMissionStateChanged;
        foreach (Door door in Door.All)
            if (door != null) doorStates[door] = door.IsOpen;

        ScanForEnemyWeapons();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (AudioManager.Instance != null) AudioManager.Instance.StopFootsteps();
        if (player != null) player.Dashed -= OnDash;
        if (playerHealth != null)
        {
            playerHealth.Damaged -= OnPlayerDamaged;
            playerHealth.Healed -= OnPlayerHealed;
            playerHealth.Died -= OnPlayerDied;
        }
        UnsubscribePlayerWeapon();
        foreach (var pair in enemyWeaponHandlers)
            if (pair.Key != null) pair.Key.Fired -= pair.Value;
        AlarmSystem.Raised -= OnAlarmRaised;
        if (GameManager.Instance != null) GameManager.Instance.StateChanged -= OnMissionStateChanged;
    }

    void Update()
    {
        AudioManager audio = AudioManager.Instance;
        if (audio == null || Time.timeScale == 0f) return;

        Weapon current = player != null ? player.CurrentWeapon : null;
        if (current != subscribedPlayerWeapon)
        {
            UnsubscribePlayerWeapon();
            subscribedPlayerWeapon = current;
            if (subscribedPlayerWeapon != null)
            {
                subscribedPlayerWeapon.Fired += OnPlayerWeaponFired;
                subscribedPlayerWeapon.EmptyAttempted += OnPlayerEmptyAttempted;
            }
            wasReloading = false;
        }

        if (subscribedPlayerWeapon != null)
        {
            bool reloading = subscribedPlayerWeapon.IsReloading;
            if (reloading && !wasReloading) audio.PlaySfx(audio.reload);
            wasReloading = reloading;
        }

        if (Time.time >= nextEnemyWeaponScanTime)
        {
            nextEnemyWeaponScanTime = Time.time + 0.75f;
            ScanForEnemyWeapons();
        }

        if (player != null)
        {
            Vector3 currentPosition = player.transform.position;
            bool movedThisFrame = (currentPosition - lastPlayerPosition).sqrMagnitude > movementThreshold;
            lastPlayerPosition = currentPosition;

            // Rigidbody velocity is the main movement signal; position delta is a fallback.
            bool hasVelocity = playerBody != null && playerBody.linearVelocity.sqrMagnitude > 0.01f;
            bool moving = hasVelocity || movedThisFrame;
            bool canMakeFootsteps = moving && player.isActiveAndEnabled && !player.IsHidden &&
                                    !player.IsDashing && !player.InputLocked;

            if (!canMakeFootsteps)
            {
                // Cut the current clip immediately when the player stops or becomes unavailable.
                audio.StopFootsteps();
                nextFootstepTime = Time.time;
                hasFootstepMode = false;
            }
            else
            {
                bool running = player.IsRunning; // IsRunning is true only while Shift is held and moving.
                if (!hasFootstepMode || running != lastFootstepModeWasRunning)
                {
                    // Switching walk/run should immediately replace the old sound.
                    audio.StopFootsteps();
                    lastFootstepModeWasRunning = running;
                    hasFootstepMode = true;
                    nextFootstepTime = Time.time;
                }

                if (Time.time >= nextFootstepTime)
                {
                    nextFootstepTime = Time.time + (running ? runningFootstepInterval : walkingFootstepInterval);
                    AudioClip clip = running && audio.runningFootsteps != null ? audio.runningFootsteps : audio.footsteps;
                    audio.PlayFootstep(clip, running ? 0.7f : 0.45f);
                }
            }
        }
        else
        {
            audio.StopFootsteps();
        }

        foreach (Door door in Door.All)
        {
            if (door == null) continue;
            bool open = door.IsOpen;
            if (!doorStates.ContainsKey(door)) doorStates[door] = open;
            else if (open && !doorStates[door])
            {
                audio.PlaySfx(audio.door, 0.65f);
                doorStates[door] = true;
            }
            else if (!open && doorStates[door]) doorStates[door] = false;
        }
    }

    void UnsubscribePlayerWeapon()
    {
        if (subscribedPlayerWeapon == null) return;
        subscribedPlayerWeapon.Fired -= OnPlayerWeaponFired;
        subscribedPlayerWeapon.EmptyAttempted -= OnPlayerEmptyAttempted;
        subscribedPlayerWeapon = null;
    }

    void ScanForEnemyWeapons()
    {
        Weapon[] found = FindObjectsOfType<Weapon>(true);
        foreach (Weapon weapon in found)
        {
            if (weapon == null || weapon.OwnerTeam != Team.Enemy || enemyWeaponHandlers.ContainsKey(weapon)) continue;
            Weapon capturedWeapon = weapon;
            System.Action handler = () =>
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayWeaponShot(capturedWeapon);
            };
            enemyWeaponHandlers.Add(weapon, handler);
            weapon.Fired += handler;
        }
    }

    void OnPlayerWeaponFired()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayWeaponShot(subscribedPlayerWeapon);
    }
    void OnPlayerEmptyAttempted()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(AudioManager.Instance.emptyMagazine);
    }
    void OnDash()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(AudioManager.Instance.dash);
    }
    void OnPlayerDamaged(float amount, Vector2 fromPosition)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(AudioManager.Instance.playerHurt);
    }
    void OnPlayerHealed(float amount)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(AudioManager.Instance.healthPickup);
    }
    void OnPlayerDied()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(AudioManager.Instance.playerDeath);
    }
    void OnAlarmRaised()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(AudioManager.Instance.alarm);
    }
    void OnMissionStateChanged(MissionState state)
    {
        if (AudioManager.Instance == null) return;
        if (state == MissionState.Escape) AudioManager.Instance.PlaySfx(AudioManager.Instance.objective);
        else if (state == MissionState.Complete) AudioManager.Instance.PlaySfx(AudioManager.Instance.missionComplete);
        else if (state == MissionState.Failed) AudioManager.Instance.PlaySfx(AudioManager.Instance.missionFailed);
    }
}
