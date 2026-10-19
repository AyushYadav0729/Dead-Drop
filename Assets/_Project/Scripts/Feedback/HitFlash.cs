using UnityEngine;

/// Tints the sprite briefly when this object takes damage. Works on the player, enemies and
/// cameras: anything with a Health. Stops by itself on death, so a death tint set by another
/// script (the grey corpse) is not undone.
[RequireComponent(typeof(Health))]
public class HitFlash : MonoBehaviour
{
    [SerializeField] Color flashColor = new Color(1f, 0.25f, 0.25f);
    [SerializeField] float duration = 0.15f;
    [Tooltip("Leave empty to flash every sprite on this object and its children.")]
    [SerializeField] SpriteRenderer[] targets;

    Health health;
    Color[] original;
    float flashEnd;
    bool flashing;

    void Awake()
    {
        health = GetComponent<Health>();
        if (targets == null || targets.Length == 0)
            targets = GetComponentsInChildren<SpriteRenderer>(true);

        original = new Color[targets.Length];
        for (int i = 0; i < targets.Length; i++)
            original[i] = targets[i].color;
    }

    void OnEnable()
    {
        health.Damaged += OnDamaged;
        health.Died += OnDied;
    }

    void OnDisable()
    {
        health.Damaged -= OnDamaged;
        health.Died -= OnDied;
    }

    void OnDamaged(float amount, Vector2 from)
    {
        if (health.IsDead) return;
        flashing = true;
        flashEnd = Time.time + duration;
    }

    void OnDied()
    {
        flashing = false;
        enabled = false;
    }

    void Update()
    {
        if (!flashing) return;

        float t = Mathf.Clamp01((flashEnd - Time.time) / duration); // 1 right after the hit, 0 when done
        for (int i = 0; i < targets.Length; i++)
            if (targets[i] != null) targets[i].color = Color.Lerp(original[i], flashColor, t);

        if (t <= 0f) flashing = false;
    }
}
