using UnityEngine;

/// Moves by raycasting each frame instead of using physics colliders.
/// That gives an exact hit normal for bouncing and never tunnels through thin walls.
public class Bullet : MonoBehaviour
{
    [Tooltip("Layers the bullet can hit: Walls, Player, Enemy.")]
    [SerializeField] LayerMask hitMask;

    WeaponData data;
    Vector2 direction;
    Team ownerTeam;
    Vector2 shooterPosition;
    int bouncesLeft;
    float rangeLeft;

    public void Launch(WeaponData weapon, Vector2 dir, Team owner, Vector2 shooterPos)
    {
        data = weapon;
        direction = dir.normalized;
        ownerTeam = owner;
        shooterPosition = shooterPos;
        bouncesLeft = weapon.maxBounces;
        rangeLeft = weapon.range;
        FaceDirection();
    }

    void Update()
    {
        if (data == null) return;

        float step = Mathf.Min(data.bulletSpeed * Time.deltaTime, rangeLeft);
        Vector2 pos = transform.position;

        if (ProjectileCast.Cast(pos, direction, step, hitMask, ownerTeam, out RaycastHit2D hit))
        {
            Health target = hit.collider.GetComponentInParent<Health>();
            if (target != null)
            {
                target.TakeDamage(data.damage, shooterPosition);
                Destroy(gameObject);
                return;
            }

            // Hit a wall.
            if (bouncesLeft <= 0) { Destroy(gameObject); return; }

            bouncesLeft--;
            rangeLeft -= hit.distance;
            direction = Vector2.Reflect(direction, hit.normal).normalized;
            transform.position = hit.point + hit.normal * 0.02f; // nudge off the wall
            FaceDirection();
            return;
        }

        transform.position = pos + direction * step;
        rangeLeft -= step;
        if (rangeLeft <= 0f) Destroy(gameObject);
    }

    void FaceDirection()
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}
