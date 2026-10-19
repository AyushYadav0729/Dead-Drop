using UnityEngine;

/// Put this on the object that holds the character's SpriteRenderer, as a CHILD of the object
/// that rotates to aim (the Player, or an enemy).
///
/// The aiming root keeps rotating toward the mouse or target, because bullets, the laser sight and
/// vision cones all depend on it. This script makes the SPRITE ignore that rotation: it always stays
/// upright and is mirrored when the character faces left. A small tilt toward the aim direction is
/// optional, so a character aiming up or down leans slightly.
public class FacingSprite : MonoBehaviour
{
    [Tooltip("The object whose rotation says where the character is aiming. Defaults to the parent.")]
    [SerializeField] Transform aimRoot;
    [Tooltip("How far the sprite turns toward the aim direction, in degrees. 90 = the gun always points exactly at the cursor " +
             "(the character is never upside down because it is mirrored instead). Lower values keep the body more upright " +
             "but the gun then points less accurately. 0 = never turns.")]
    [SerializeField, Range(0f, 90f)] float maxTilt = 90f;

    public bool FacingLeft { get; private set; }

    Vector3 baseScale;

    void Awake()
    {
        if (aimRoot == null) aimRoot = transform.parent;
        baseScale = transform.localScale;
        baseScale.x = Mathf.Abs(baseScale.x);
    }

    void LateUpdate()
    {
        if (aimRoot == null) return;

        float aim = aimRoot.eulerAngles.z;
        float c = Mathf.Cos(aim * Mathf.Deg2Rad);

        // Small dead zone so aiming straight up or down does not flicker between left and right.
        if (c > 0.12f) FacingLeft = false;
        else if (c < -0.12f) FacingLeft = true;

        transform.localScale = new Vector3(FacingLeft ? -baseScale.x : baseScale.x, baseScale.y, baseScale.z);

        float tilt = 0f;
        if (maxTilt > 0f)
            tilt = Mathf.Clamp(Mathf.DeltaAngle(FacingLeft ? 180f : 0f, aim), -maxTilt, maxTilt);

        transform.rotation = Quaternion.Euler(0f, 0f, tilt);   // world rotation: ignores the parent's spin
    }
}