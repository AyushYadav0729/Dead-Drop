using System;
using System.Collections;
using UnityEngine;

/// Lives on a child of the shooter. Handles fire rate, magazine, reload and spawning bullets.
/// Enemies can use the same component with Infinite Ammo ticked.
public class Weapon : MonoBehaviour
{
    [SerializeField] WeaponData data;
    [SerializeField] Transform firePoint;
    [Tooltip("Walls only. Stops the muzzle spawning bullets inside a wall.")]
    [SerializeField] LayerMask wallMask;
    [SerializeField] int reserveAmmo = 48;
    [SerializeField] bool infiniteAmmo;

    public WeaponData Data => data;
    public int MagAmmo { get; private set; }
    public int ReserveAmmo => reserveAmmo;
    public bool IsReloading { get; private set; }
    /// Enemies raise this during the alarm so they shoot faster.
    public float FireRateMultiplier { get; set; } = 1f;
    public Team OwnerTeam => ownerHealth != null ? ownerHealth.team : Team.Enemy;
    public float ReloadProgress =>
        IsReloading ? Mathf.Clamp01(1f - (reloadEndTime - Time.time) / data.reloadTime) : 0f;

    public event Action AmmoChanged;
    /// Raised every time a shot is fired. Used for the muzzle flash and the shoot animation.
    public event Action Fired;
    /// Raised when the player pulls the trigger with an empty magazine.
    public event Action EmptyAttempted;

    float nextEmptySoundTime;

    Health ownerHealth;
    float nextFireTime;
    float reloadEndTime;

    void Awake()
    {
        ownerHealth = GetComponentInParent<Health>();
        MagAmmo = data.magazineSize;
    }

    // Switching weapons deactivates this object, which also stops the reload coroutine.
    void OnDisable() => IsReloading = false;

    public bool TryFire()
    {
        if (IsReloading || Time.time < nextFireTime) return false;

        if (MagAmmo <= 0 && !infiniteAmmo)
        {
            // Avoid playing the empty click every frame if the player keeps holding fire.
            if (OwnerTeam == Team.Player && Time.time >= nextEmptySoundTime)
            {
                nextEmptySoundTime = Time.time + 0.25f;
                EmptyAttempted?.Invoke();
            }
            StartReload(); // auto-reload when the player pulls the trigger on an empty mag
            return false;
        }

        nextFireTime = Time.time + 1f / (data.fireRate * FireRateMultiplier);
        if (!infiniteAmmo) MagAmmo--;

        SpawnBullet();
        Fired?.Invoke();
        NoiseSystem.Emit(firePoint.position, data.noiseRadius, OwnerTeam);
        AmmoChanged?.Invoke();
        return true;
    }

    public void StartReload()
    {
        if (infiniteAmmo || IsReloading) return;
        if (MagAmmo >= data.magazineSize || reserveAmmo <= 0) return;
        StartCoroutine(ReloadRoutine());
    }

    public void AddAmmo(int amount)
    {
        reserveAmmo += amount;
        AmmoChanged?.Invoke();
    }

    IEnumerator ReloadRoutine()
    {
        IsReloading = true;
        reloadEndTime = Time.time + data.reloadTime;
        AmmoChanged?.Invoke();

        yield return new WaitForSeconds(data.reloadTime);

        int moved = Mathf.Min(data.magazineSize - MagAmmo, reserveAmmo);
        MagAmmo += moved;
        reserveAmmo -= moved;
        IsReloading = false;
        AmmoChanged?.Invoke();
    }

    void SpawnBullet()
    {
        Vector2 origin = firePoint.position;
        Vector2 center = ownerHealth != null ? (Vector2)ownerHealth.transform.position
                                             : (Vector2)transform.position;

        // If the muzzle is poking through a wall, spawn the bullet at the wall instead.
        RaycastHit2D wall = Physics2D.Linecast(center, origin, wallMask);
        if (wall.collider != null) origin = wall.point + wall.normal * 0.05f;

        float angle = firePoint.eulerAngles.z +
                      UnityEngine.Random.Range(-data.spreadDegrees, data.spreadDegrees);
        Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad),
                                  Mathf.Sin(angle * Mathf.Deg2Rad));

        Bullet bullet = Instantiate(data.bulletPrefab, origin, Quaternion.identity);
        bullet.Launch(data, dir, OwnerTeam, center);
    }
}
