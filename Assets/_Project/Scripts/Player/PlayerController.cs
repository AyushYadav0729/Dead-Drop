using System;
using UnityEngine;

/// Top-down movement (WASD), mouse aim, shooting, reload, weapon switch, laser toggle.
///
///   Walk   default, slower, silent.
///   Run    hold Shift. Faster, but footsteps make noise that nearby enemies can hear.
///          The noise radius is smaller than gunfire.
///   Dash   press Space. A short, fast burst of movement. Silent. 5 second cooldown.
///
/// Uses the legacy Input Manager (set Active Input Handling to "Both" or "Input Manager (Old)").
/// The player sprite must face RIGHT (+X) at rotation 0.
[RequireComponent(typeof(Rigidbody2D), typeof(Health))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float walkSpeed = 3.5f;
    [SerializeField] float runSpeed = 5f;

    [Header("Run noise")]
    [Tooltip("How far a footstep is heard by enemies while running. Gunshots are louder (see WeaponData).")]
    [SerializeField] float runNoiseRadius = 5f;
    [SerializeField] float footstepInterval = 0.35f;

    [Header("Dash (silent)")]
    [SerializeField] float dashSpeed = 14f;
    [SerializeField] float dashDuration = 0.15f;
    [SerializeField] float dashCooldown = 5f;

    [Header("Weapons")]
    [SerializeField] Weapon[] weapons;   // 1 = Pistol, 2 = Rifle
    [SerializeField] LaserSight laser;

    public Weapon CurrentWeapon => weapons[currentIndex];

    /// Set by the lockpick minigame (or any future menu) so the player cannot move, shoot or dash.
    public bool InputLocked { get; set; }

    public bool IsRunning { get; private set; }
    /// True while the player is inside a hiding spot: invisible, silent, cannot be hit.
    public bool IsHidden { get; private set; }
    public bool IsDashing => Time.time < dashEndTime;
    public bool DashReady => Time.time >= nextDashTime;
    public float DashCooldownRemaining => Mathf.Max(0f, nextDashTime - Time.time);
    /// 0 right after a dash, 1 when ready again. Handy for a cooldown bar.
    public float DashCharge01 => dashCooldown <= 0f ? 1f : 1f - DashCooldownRemaining / dashCooldown;

    /// Fired when a dash starts (for sound or effects later).
    public event Action Dashed;

    Rigidbody2D rb;
    Health health;
    Camera cam;
    Vector2 moveInput;
    int currentIndex;
    bool laserOn;

    Vector2 dashDirection;
    float dashEndTime;
    float nextDashTime;
    float nextFootstepTime;
    Vector2 preHidePosition;
    SpriteRenderer[] hiddenRenderers;
    bool[] hiddenRendererStates;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        cam = Camera.main;

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        health.Died += OnDied;
    }

    void OnDestroy() => health.Died -= OnDied;

    void Start() => Equip(0);

    void Update()
    {
        if (health.IsDead || Time.timeScale == 0f || InputLocked)
        {
            moveInput = Vector2.zero;
            IsRunning = false;
            return;
        }

        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;

        bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        IsRunning = shiftHeld && moveInput != Vector2.zero && !IsDashing;

        AimAtMouse();
        HandleDash();
        HandleFootsteps();
        HandleWeaponInput();
    }

    void FixedUpdate()
    {
        if (IsDashing) SetVelocity(dashDirection * dashSpeed);
        else SetVelocity(moveInput * (IsRunning ? runSpeed : walkSpeed));
    }

    // ---------------------------------------------------------------- movement abilities

    void HandleDash()
    {
        if (!Input.GetKeyDown(KeyCode.Space) || !DashReady) return;

        // Dash the way you are moving; if standing still, dash toward the aim direction.
        dashDirection = moveInput != Vector2.zero ? moveInput : (Vector2)transform.right;
        dashEndTime = Time.time + dashDuration;
        nextDashTime = Time.time + dashCooldown;   // cooldown counts from the start of the dash

        Dashed?.Invoke();
        // Deliberately no NoiseSystem call: the dash is silent.
    }

    void HandleFootsteps()
    {
        if (!IsRunning || Time.time < nextFootstepTime) return;

        nextFootstepTime = Time.time + footstepInterval;
        NoiseSystem.Emit(rb.position, runNoiseRadius, Team.Player);
    }

    // ---------------------------------------------------------------- hiding

    /// Called by HidingSpot. Hiding turns off the sprites and the physics body (so bullets and
    /// guards cannot touch the player) and locks input. Leaving puts the player back where they were.
    public void SetHidden(bool hidden, Vector2 hidePosition)
    {
        if (hidden == IsHidden || health.IsDead) return;

        IsHidden = hidden;
        InputLocked = hidden;

        if (hidden)
        {
            preHidePosition = rb.position;
            moveInput = Vector2.zero;
            SetVelocity(Vector2.zero);

            hiddenRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            hiddenRendererStates = new bool[hiddenRenderers.Length];
            for (int i = 0; i < hiddenRenderers.Length; i++)
            {
                hiddenRendererStates[i] = hiddenRenderers[i].enabled;
                hiddenRenderers[i].enabled = false;
            }

            laser.SetActive(false);
            rb.simulated = false;
            transform.position = hidePosition;
        }
        else
        {
            transform.position = preHidePosition;
            rb.simulated = true;

            if (hiddenRenderers != null)
                for (int i = 0; i < hiddenRenderers.Length; i++)
                    if (hiddenRenderers[i] != null) hiddenRenderers[i].enabled = hiddenRendererStates[i];

            laser.SetActive(laserOn);
        }
    }

    // ---------------------------------------------------------------- aiming and weapons

    void AimAtMouse()
    {
        Vector2 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 toMouse = mouseWorld - rb.position;
        float angle = Mathf.Atan2(toMouse.y, toMouse.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void HandleWeaponInput()
    {
        if ((Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) && weapons.Length > 0 && currentIndex != 0) Equip(0);
        if ((Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) && weapons.Length > 1 && currentIndex != 1) Equip(1);

        Weapon w = CurrentWeapon;
        bool wantsFire = w.Data.automatic ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);
        if (wantsFire) w.TryFire();

        if (Input.GetKeyDown(KeyCode.R)) w.StartReload();

        if (Input.GetMouseButtonDown(1))
        {
            laserOn = !laserOn;
            laser.SetActive(laserOn);
        }
    }

    void Equip(int index)
    {
        currentIndex = index;
        for (int i = 0; i < weapons.Length; i++)
            weapons[i].gameObject.SetActive(i == index);
        laser.Weapon = weapons[index];
    }

    void OnDied()
    {
        laser.SetActive(false);
        moveInput = Vector2.zero;
        IsRunning = false;
        dashEndTime = 0f;
        SetVelocity(Vector2.zero);
        enabled = false;
    }

    void SetVelocity(Vector2 v)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = v;
#else
        rb.velocity = v;
#endif
    }
}
