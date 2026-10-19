using UnityEngine;

/// Marks a place where reinforcements can appear. Put it on an empty object in open floor
/// (at least 2 x 2 tiles of free space), away from where the player normally walks.
public class ReinforcementSpawnPoint : MonoBehaviour
{
    [Tooltip("Optional. Where units from this spawn point patrol once the fight is over. Leave empty to have them stand guard where they stop.")]
    public Transform[] patrolRoute;

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, 0.7f);
        Gizmos.DrawLine(transform.position + Vector3.left * 0.5f, transform.position + Vector3.right * 0.5f);
        Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, transform.position + Vector3.down * 0.5f);

        if (patrolRoute == null) return;
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.7f);
        Vector3 prev = transform.position;
        foreach (Transform t in patrolRoute)
        {
            if (t == null) continue;
            Gizmos.DrawLine(prev, t.position);
            prev = t.position;
        }
    }
}
