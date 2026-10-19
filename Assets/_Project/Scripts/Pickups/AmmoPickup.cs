using UnityEngine;

/// Ammo box for ONE weapon type. Assign the player's Pistol or Rifle WeaponData asset
/// (not the enemy ones). Adds to that weapon's reserve, up to a cap. At the cap it stays put.
public class AmmoPickup : Pickup
{
    [Tooltip("Which weapon this ammo is for: the player's Pistol or Rifle WeaponData asset.")]
    [SerializeField] WeaponData weaponType;
    [SerializeField] int amount = 24;
    [Tooltip("The player cannot hold more reserve ammo than this.")]
    [SerializeField] int maxReserve = 120;

    protected override bool Collect(PlayerController player)
    {
        // includeInactive = true: the weapon that is not currently equipped is switched off.
        foreach (Weapon w in player.GetComponentsInChildren<Weapon>(true))
        {
            if (w.Data != weaponType) continue;

            int space = maxReserve - w.ReserveAmmo;
            if (space <= 0) return false;

            w.AddAmmo(Mathf.Min(amount, space));
            return true;
        }
        return false;
    }
}
