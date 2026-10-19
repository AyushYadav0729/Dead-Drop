using UnityEngine;

/// One asset per weapon type (Pistol, Rifle...). Create via Assets > Create > Agent > Weapon Data.
[CreateAssetMenu(menuName = "Agent/Weapon Data", fileName = "NewWeapon")]
public class WeaponData : ScriptableObject
{
    public string displayName = "Pistol";
    public Bullet bulletPrefab;

    [Header("Damage and range")]
    public float damage = 25f;
    public float bulletSpeed = 20f;
    [Tooltip("Total distance a bullet can travel, including after bounces.")]
    public float range = 9f;
    [Tooltip("0 = no bounce. Rifles use 1.")]
    public int maxBounces = 0;

    [Header("Fire control")]
    [Tooltip("Shots per second.")]
    public float fireRate = 3.5f;
    public bool automatic = false;
    public float spreadDegrees = 1.5f;

    [Header("Ammo")]
    public int magazineSize = 12;
    public float reloadTime = 1.1f;

    [Header("Stealth")]
    [Tooltip("How far a gunshot is heard by enemies.")]
    public float noiseRadius = 12f;
}
