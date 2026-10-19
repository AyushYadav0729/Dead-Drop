using UnityEngine;

/// Smooth follow with a slight pull toward the mouse so the player sees further in the aim direction.
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float smoothing = 8f;
    [SerializeField, Range(0f, 0.5f)] float mouseLookAhead = 0.2f;

    Camera cam;

    void Awake() => cam = GetComponent<Camera>();

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector3 desired = Vector3.Lerp(target.position, mouseWorld, mouseLookAhead);
        desired.z = transform.position.z;

        float t = 1f - Mathf.Exp(-smoothing * Time.deltaTime); // frame-rate independent smoothing
        transform.position = Vector3.Lerp(transform.position, desired, t);
    }
}
