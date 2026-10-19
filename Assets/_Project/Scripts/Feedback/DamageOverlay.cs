using UnityEngine;

/// Red screen flash when the player is hit, and a slow red pulse while health is low.
/// Put it on an empty object. It finds the player by tag if the slot is empty.
/// Placeholder drawing: replace with the Figma damage vignette later.
public class DamageOverlay : MonoBehaviour
{
    [SerializeField] Health playerHealth;
    [SerializeField] Color hitColor = new Color(0.9f, 0.05f, 0.05f);
    [SerializeField] float flashSeconds = 0.35f;
    [SerializeField, Range(0f, 1f)] float maxFlashAlpha = 0.35f;
    [Tooltip("Below this fraction of full health the screen pulses red.")]
    [SerializeField, Range(0f, 1f)] float lowHealthFraction = 0.3f;

    float flashStart = -100f;

    void Start()
    {
        if (playerHealth == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerHealth = p.GetComponentInParent<Health>();
        }
        if (playerHealth != null) playerHealth.Damaged += OnDamaged;
    }

    void OnDestroy()
    {
        if (playerHealth != null) playerHealth.Damaged -= OnDamaged;
    }

    void OnDamaged(float amount, Vector2 from) => flashStart = Time.unscaledTime;

    void OnGUI()
    {
        if (Event.current.type != EventType.Repaint || playerHealth == null) return;

        float alpha = 0f;

        float since = Time.unscaledTime - flashStart;
        if (since < flashSeconds) alpha = maxFlashAlpha * (1f - since / flashSeconds);

        if (!playerHealth.IsDead && playerHealth.Current / playerHealth.Max <= lowHealthFraction)
            alpha = Mathf.Max(alpha, 0.10f + 0.06f * Mathf.Sin(Time.unscaledTime * 6f));

        if (alpha <= 0.001f) return;

        Color old = GUI.color;
        GUI.color = new Color(hitColor.r, hitColor.g, hitColor.b, alpha);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = old;
    }
}
