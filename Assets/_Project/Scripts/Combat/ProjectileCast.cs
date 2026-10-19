using UnityEngine;

/// Raycast used by both Bullet and LaserSight, so the laser preview always matches
/// where bullets really go. Skips triggers and anything on the shooter's own team.
public static class ProjectileCast
{
    static readonly RaycastHit2D[] buffer = new RaycastHit2D[16];

    public static bool Cast(Vector2 origin, Vector2 direction, float distance,
                            LayerMask mask, Team ignoreTeam, out RaycastHit2D result)
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(mask);
        filter.useTriggers = false;

        int count = Physics2D.Raycast(origin, direction, filter, buffer, distance);

        bool found = false;
        float best = float.MaxValue;
        result = default;

        for (int i = 0; i < count; i++)
        {
            RaycastHit2D h = buffer[i];
            Health health = h.collider.GetComponentInParent<Health>();
            if (health != null && health.team == ignoreTeam) continue; // no friendly fire

            if (h.distance < best)
            {
                best = h.distance;
                result = h;
                found = true;
            }
        }
        return found;
    }
}
