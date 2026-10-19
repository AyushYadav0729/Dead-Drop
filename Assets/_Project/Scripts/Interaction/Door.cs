using System.Collections.Generic;
using UnityEngine;

/// An automatic sliding door. Two panels slide apart into the walls when the player or any guard
/// comes near, and slide shut a moment after everyone has gone.
///
/// While CLOSED it has a solid collider on the Walls layer, so it blocks guards' sight, vision
/// cones, bullets and the laser sight with no extra setup. While open the collider is off.
///
/// Guards can path through doors: PathGrid switches every door's collider off while it scans the
/// level, so a door never counts as a wall for pathfinding.
///
/// Layout: the Door object sits in the middle of a 2-tile-wide gap and is turned so its local X
/// axis runs ACROSS the gap (rotate it 90 degrees for a vertical passage).
public class Door : MonoBehaviour
{
    /// Every door in the scene. PathGrid uses it.
    public static readonly List<Door> All = new List<Door>();

    [Header("Parts")]
    [Tooltip("Slides toward -X (local) when opening.")]
    [SerializeField] Transform panelA;
    [Tooltip("Slides toward +X (local) when opening. Use a flipped copy of the same sprite.")]
    [SerializeField] Transform panelB;
    [Tooltip("The closed-door collider. It must be on the Walls layer. Defaults to the one on this object.")]
    [SerializeField] Collider2D blocker;

    [Header("Behaviour")]
    [Tooltip("How far each panel slides. 1 = a full tile, so it disappears into the wall.")]
    [SerializeField] float slideDistance = 1f;
    [Tooltip("Anyone this close to the door's centre opens it.")]
    [SerializeField] float openRadius = 2.2f;
    [SerializeField] float openSeconds = 0.25f;
    [Tooltip("Stays open this long after the last person walks away.")]
    [SerializeField] float closeDelay = 0.6f;
    [Tooltip("The collider switches off once the door is this far open.")]
    [SerializeField, Range(0.1f, 0.9f)] float passableAt = 0.35f;

    public bool IsOpen => openAmount >= passableAt;

    float openAmount;
    float lastNearTime = -100f;
    bool pathBuildMode;
    Vector3 panelAStart, panelBStart;
    PlayerController player;

    void Awake()
    {
        if (blocker == null) blocker = GetComponent<Collider2D>();
        if (panelA != null) panelAStart = panelA.localPosition;
        if (panelB != null) panelBStart = panelB.localPosition;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.GetComponentInParent<PlayerController>();

        ApplyCollider();
    }

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    void Update()
    {
        bool near = SomeoneIsNear();
        if (near) lastNearTime = Time.time;

        bool wantOpen = near || Time.time - lastNearTime < closeDelay;
        openAmount = Mathf.MoveTowards(openAmount, wantOpen ? 1f : 0f, Time.deltaTime / openSeconds);

        if (panelA != null) panelA.localPosition = panelAStart + Vector3.left * (openAmount * slideDistance);
        if (panelB != null) panelB.localPosition = panelBStart + Vector3.right * (openAmount * slideDistance);

        ApplyCollider();
    }

    bool SomeoneIsNear()
    {
        Vector2 centre = transform.position;
        float r2 = openRadius * openRadius;

        // A hidden player is inside a crate, so he does not open doors.
        if (player != null && !player.IsHidden &&
            ((Vector2)player.transform.position - centre).sqrMagnitude <= r2) return true;

        foreach (EnemyController e in EnemyController.All)
            if (e != null && ((Vector2)e.transform.position - centre).sqrMagnitude <= r2) return true;

        return false;
    }

    void ApplyCollider()
    {
        if (blocker != null) blocker.enabled = !pathBuildMode && openAmount < passableAt;
    }

    /// Called by PathGrid while it scans the level, so the closed door does not count as a wall.
    public void SetPathBuildMode(bool on)
    {
        pathBuildMode = on;
        ApplyCollider();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => All.Clear();

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.35f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, openRadius);
        Gizmos.color = new Color(0.3f, 0.9f, 1f, 0.8f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(2f, 1f, 0f));
    }
}
