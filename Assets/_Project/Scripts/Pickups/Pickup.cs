using UnityEngine;

/// Base class for things the player walks over to collect. Handles the trigger setup, a gentle
/// pulse so it catches the eye, and "collect only once". Subclasses decide what collecting does,
/// and may refuse (return false) if the player has no use for it right now. A refused pickup
/// simply stays where it is.
[RequireComponent(typeof(Collider2D))]
public abstract class Pickup : MonoBehaviour
{
    [SerializeField] float pulseAmount = 0.06f;
    [SerializeField] float pulseSpeed = 4f;

    Vector3 baseScale;
    bool collected;

    protected virtual void Awake()
    {
        baseScale = transform.localScale;
        GetComponent<Collider2D>().isTrigger = true;
    }

    void Update()
    {
        transform.localScale = baseScale * (1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount);
    }

    void OnTriggerEnter2D(Collider2D other) => TryPickUp(other);

    // Stay as well as Enter, so a pickup that was refused earlier (e.g. full health) can still be
    // collected if the player is standing on it when they later need it.
    void OnTriggerStay2D(Collider2D other) => TryPickUp(other);

    void TryPickUp(Collider2D other)
    {
        if (collected) return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        if (Collect(player))
        {
            collected = true;
            Destroy(gameObject);
        }
    }

    /// Return true if the pickup was used up, false to leave it in the world.
    protected abstract bool Collect(PlayerController player);
}
