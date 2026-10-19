using UnityEngine;

/// Medkit. Restores health up to the maximum. If the player is already at full health it stays
/// in place, so it is not wasted.
public class HealthPickup : Pickup
{
    [SerializeField] float healAmount = 40f;

    protected override bool Collect(PlayerController player)
    {
        Health health = player.GetComponent<Health>();
        if (health == null || health.IsDead || health.IsFull) return false;

        health.Heal(healAmount);
        return true;
    }
}
