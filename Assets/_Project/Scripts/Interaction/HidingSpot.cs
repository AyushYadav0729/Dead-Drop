using UnityEngine;

/// A crate, almirah, locker... anything the player can climb into by pressing E.
///
/// While hidden the player is invisible, silent, cannot be shot and cannot act. Guards and
/// cameras cannot see him. Press E again to climb out.
///
/// Rules that keep hiding from being an exploit:
///   - You cannot hide while a guard or camera can see you. Break line of sight first.
///   - A guard that is SEARCHING close to an occupied spot will eventually find you,
///     push you out and start chasing. (Guards that merely patrol past will not.)
///
/// Give it a solid collider on the Walls layer so it blocks movement, bullets and pathfinding.
public class HidingSpot : MonoBehaviour
{
    [SerializeField] float interactRange = 1.6f;
    [Tooltip("Where the player appears while hidden. Defaults to this object's centre.")]
    [SerializeField] Transform hidePoint;

    [Header("Being found (set Discover Radius to 0 to turn off)")]
    [Tooltip("A guard in the Search state this close to the spot counts as checking it.")]
    [SerializeField] float discoverRadius = 1.4f;
    [Tooltip("Seconds a searching guard must stay close before finding you.")]
    [SerializeField] float discoverTime = 2f;

    [Header("Optional visuals")]
    [SerializeField] SpriteRenderer sprite;
    [SerializeField] Color occupiedTint = new Color(0.75f, 0.9f, 1f);
    [Tooltip("The spot turns this colour as a searching guard gets close to finding you.")]
    [SerializeField] Color dangerTint = new Color(1f, 0.45f, 0.4f);

    public bool IsOccupied { get; private set; }

    PlayerController player;
    Health playerHealth;
    Camera cam;
    Color baseColor = Color.white;
    float discoverTimer;
    bool inRange;
    GUIStyle promptStyle;

    Vector2 HidePosition => hidePoint != null ? (Vector2)hidePoint.position : (Vector2)transform.position;

    void Awake()
    {
        cam = Camera.main;
        if (sprite != null) baseColor = sprite.color;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.GetComponentInParent<PlayerController>();
            playerHealth = p.GetComponentInParent<Health>();
        }
        if (player == null)
            Debug.LogWarning("HidingSpot: no 'Player' tagged object with a PlayerController found.", this);
    }

    void Update()
    {
        inRange = false;
        if (player == null || Time.timeScale == 0f) return;
        if (playerHealth != null && playerHealth.IsDead) return;

        if (IsOccupied)
        {
            UpdateOccupied();
            return;
        }

        if (player.InputLocked) return; // busy (for example picking a lock)

        inRange = Vector2.Distance(player.transform.position, transform.position) <= interactRange;

        if (inRange && Input.GetKeyDown(KeyCode.E) && !PlayerVisibility.IsSeen)
            Enter();
    }

    void Enter()
    {
        IsOccupied = true;
        discoverTimer = 0f;
        player.SetHidden(true, HidePosition);
        if (sprite != null) sprite.color = occupiedTint;
    }

    void Leave()
    {
        IsOccupied = false;
        discoverTimer = 0f;
        player.SetHidden(false, Vector2.zero);
        if (sprite != null) sprite.color = baseColor;
    }

    void UpdateOccupied()
    {
        if (!player.IsHidden)   // something else released the player
        {
            IsOccupied = false;
            if (sprite != null) sprite.color = baseColor;
            return;
        }

        if (Input.GetKeyDown(KeyCode.E)) { Leave(); return; }
        if (discoverRadius <= 0f) return;

        EnemyController finder = null;
        foreach (EnemyController e in EnemyController.All)
        {
            if (e == null || !e.enabled || e.State != EnemyState.Search) continue;
            if (Vector2.Distance(e.transform.position, HidePosition) <= discoverRadius) { finder = e; break; }
        }

        if (finder != null) discoverTimer += Time.deltaTime;
        else discoverTimer = Mathf.Max(0f, discoverTimer - Time.deltaTime);

        if (sprite != null)
            sprite.color = Color.Lerp(occupiedTint, dangerTint, Mathf.Clamp01(discoverTimer / discoverTime));

        if (finder != null && discoverTimer >= discoverTime)
        {
            Leave();
            finder.AlertTo(player.transform.position); // found you
        }
    }

    void OnGUI()
    {
        if (cam == null || (!IsOccupied && !inRange)) return;

        string text = IsOccupied ? "[E]  Leave"
                    : PlayerVisibility.IsSeen ? "You're being watched!"
                    : "[E]  Hide";

        if (promptStyle == null)
            promptStyle = new GUIStyle(GUI.skin.box) { fontSize = 16, alignment = TextAnchor.MiddleCenter };

        Vector3 screen = cam.WorldToScreenPoint(transform.position + Vector3.up * 1.1f);
        GUI.Box(new Rect(screen.x - 90f, Screen.height - screen.y - 16f, 180f, 32f), text, promptStyle);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.9f, 1f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, interactRange);
        Gizmos.color = new Color(1f, 0.4f, 0.3f, 0.6f);
        Gizmos.DrawWireSphere(HidePosition, discoverRadius);
    }
}
