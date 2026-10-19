using System;
using UnityEngine;

public enum Team { Player, Enemy }

/// Shared by the player, enemies, cameras and test dummies.
public class Health : MonoBehaviour
{
    public Team team = Team.Enemy;
    [SerializeField] float maxHealth = 100f;
    [SerializeField] bool destroyOnDeath;

    public float Current { get; private set; }
    public float Max => maxHealth;
    public bool IsDead => Current <= 0f;
    public bool IsFull => Current >= maxHealth;

    /// (damage amount, world position the shot came from)
    public event Action<float, Vector2> Damaged;
    public event Action<float> Healed;
    public event Action Died;

    void Awake() => Current = maxHealth;

    public void TakeDamage(float amount, Vector2 fromPosition)
    {
        if (IsDead) return;
        Current = Mathf.Max(0f, Current - amount);
        Damaged?.Invoke(amount, fromPosition);

        if (Current <= 0f)
        {
            Died?.Invoke();
            if (destroyOnDeath) Destroy(gameObject);
        }
    }

    /// Restores health up to the maximum. Does nothing if already dead.
    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;
        Current = Mathf.Min(maxHealth, Current + amount);
        Healed?.Invoke(amount);
    }
}
