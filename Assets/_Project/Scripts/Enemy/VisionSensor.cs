using UnityEngine;

/// Answers one question: "can I see this point right now?"
/// Three tests: within distance, inside the view angle (or the small all-round "near sense" radius),
/// and no wall in between. Security cameras will reuse this component too.
public class VisionSensor : MonoBehaviour
{
    [SerializeField] float viewDistance = 8f;
    [SerializeField, Range(10f, 180f)] float viewAngle = 80f;
    [Tooltip("Anything this close is noticed even from behind (if no wall is in the way).")]
    [SerializeField] float nearSenseRadius = 1.5f;
    [Tooltip("Walls only. These block line of sight.")]
    [SerializeField] LayerMask wallMask;

    public float ViewDistance => viewDistance;
    public float ViewAngle => viewAngle;
    public LayerMask WallMask => wallMask;

    public bool CanSee(Vector2 targetPosition)
    {
        Vector2 origin = transform.position;
        Vector2 toTarget = targetPosition - origin;
        float distance = toTarget.magnitude;

        if (distance > viewDistance) return false;

        if (distance > nearSenseRadius)
        {
            float angle = Vector2.Angle(transform.right, toTarget);
            if (angle > viewAngle * 0.5f) return false;
        }

        return Physics2D.Linecast(origin, targetPosition, wallMask).collider == null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 p = transform.position;
        float half = viewAngle * 0.5f;
        Vector3 left = Quaternion.Euler(0, 0, half) * transform.right * viewDistance;
        Vector3 right = Quaternion.Euler(0, 0, -half) * transform.right * viewDistance;
        Gizmos.DrawLine(p, p + left);
        Gizmos.DrawLine(p, p + right);
        Gizmos.DrawWireSphere(p, nearSenseRadius);
    }
}
