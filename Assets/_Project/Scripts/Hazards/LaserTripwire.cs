using UnityEngine;

/// A laser beam between two emitters. If the player breaks it, the player counts as spotted
/// (breaks Stealth, or Ghost Escape during the escape) and guards near the beam go to
/// investigate. It never hurts the player, and enemies walk through it freely.
///
/// Set Off Seconds above 0 to make the beam blink, which turns it into a timing puzzle.
[RequireComponent(typeof(LineRenderer))]
public class LaserTripwire : MonoBehaviour
{
    [Header("Emitters")]
    [SerializeField] Transform pointA;
    [SerializeField] Transform pointB;
    [Tooltip("Layer(s) that break the beam. Set to Player.")]
    [SerializeField] LayerMask detectMask;

    [Header("Timing (Off Seconds = 0 means always on)")]
    [SerializeField] float onSeconds = 3f;
    [SerializeField] float offSeconds = 0f;
    [SerializeField] float startOffset = 0f;

    [Header("When tripped")]
    [Tooltip("Guards within this distance of the beam go to investigate.")]
    [SerializeField] float callRadius = 20f;
    [SerializeField] float retriggerCooldown = 3f;

    [Header("Look")]
    [SerializeField] Color idleColor = new Color(1f, 0.3f, 0.2f, 0.9f);
    [SerializeField] Color trippedColor = Color.white;
    [SerializeField] float width = 0.05f;

    LineRenderer line;
    float nextTripTime;
    float flashUntil;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.startWidth = width;
        line.endWidth = width;
    }

    void Update()
    {
        bool on = IsOn();
        line.enabled = on;
        if (!on) return;

        Vector2 a = pointA.position;
        Vector2 b = pointB.position;
        line.SetPosition(0, a);
        line.SetPosition(1, b);

        Color c = Time.time < flashUntil ? trippedColor : idleColor;
        line.startColor = c;
        line.endColor = c;

        if (Time.time < nextTripTime) return;

        RaycastHit2D hit = Physics2D.Linecast(a, b, detectMask);
        if (hit.collider != null && hit.collider.GetComponentInParent<PlayerController>() != null)
            Trip(hit.point);
    }

    bool IsOn()
    {
        if (offSeconds <= 0f) return true;
        float cycle = onSeconds + offSeconds;
        return (Time.time + startOffset) % cycle < onSeconds;
    }

    void Trip(Vector2 point)
    {
        nextTripTime = Time.time + retriggerCooldown;
        flashUntil = Time.time + 0.5f;

        if (GameManager.Instance != null) GameManager.Instance.ReportSpotted();
        SquadCall.Make(point, point, callRadius, gameObject);
    }

    void OnDrawGizmos()
    {
        if (pointA == null || pointB == null) return;
        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.8f);
        Gizmos.DrawLine(pointA.position, pointB.position);
    }
}
