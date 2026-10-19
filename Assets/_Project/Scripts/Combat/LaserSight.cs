using UnityEngine;

/// Draws the aim line, including the bounce if the current weapon has one.
/// Uses the same ProjectileCast as bullets, so the preview is always accurate.
[RequireComponent(typeof(LineRenderer))]
public class LaserSight : MonoBehaviour
{
    [SerializeField] Transform firePoint;
    [Tooltip("Walls + Enemy layers (not Player).")]
    [SerializeField] LayerMask hitMask;

    public Weapon Weapon { get; set; }

    LineRenderer line;
    readonly Vector3[] points = new Vector3[8];

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.enabled = false;
    }

    public void SetActive(bool on) => line.enabled = on;

    void LateUpdate()
    {
        if (!line.enabled || Weapon == null) return;

        WeaponData d = Weapon.Data;
        Vector2 pos = firePoint.position;
        Vector2 dir = firePoint.right;
        float remaining = d.range; // the laser shows exactly how far the bullet can go
        int count = 0;
        points[count++] = pos;

        for (int i = 0; i <= d.maxBounces && count < points.Length - 1; i++)
        {
            if (!ProjectileCast.Cast(pos, dir, remaining, hitMask, Weapon.OwnerTeam, out RaycastHit2D hit))
            {
                points[count++] = pos + dir * remaining;
                break;
            }

            points[count++] = hit.point;
            remaining -= hit.distance;

            if (hit.collider.GetComponentInParent<Health>() != null) break; // stops at enemies

            dir = Vector2.Reflect(dir, hit.normal);
            pos = hit.point + hit.normal * 0.02f;
        }

        line.positionCount = count;
        for (int i = 0; i < count; i++) line.SetPosition(i, points[i]);
    }
}
