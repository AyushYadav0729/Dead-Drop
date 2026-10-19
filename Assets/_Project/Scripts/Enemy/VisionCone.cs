using UnityEngine;

/// Anything that can report how alert it is (0 = calm, 1 = fully alerted).
public interface IAlertness
{
    float Alertness { get; }
}

/// Draws the visible vision cone as a mesh fan, clipped by walls, and colours it
/// green -> yellow -> red from the owner's Alertness. Put it on a child object that has a
/// MeshFilter + MeshRenderer. Purely visual: all detection logic is in VisionSensor.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VisionCone : MonoBehaviour
{
    [SerializeField] int rayCount = 40;
    [SerializeField] int sortingOrder = 1;
    [SerializeField] Color calm = new Color(0.2f, 0.9f, 0.3f, 0.28f);
    [SerializeField] Color suspicious = new Color(1f, 0.85f, 0.1f, 0.32f);
    [SerializeField] Color alert = new Color(1f, 0.15f, 0.1f, 0.38f);

    VisionSensor sensor;
    IAlertness alertness;
    Mesh mesh;
    Vector3[] vertices;
    Color[] colors;

    void Awake()
    {
        sensor = GetComponentInParent<VisionSensor>();
        alertness = GetComponentInParent<IAlertness>();

        vertices = new Vector3[rayCount + 2];
        colors = new Color[vertices.Length];
        int[] triangles = new int[rayCount * 3];
        for (int i = 0; i < rayCount; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 2;   // clockwise winding
            triangles[i * 3 + 2] = i + 1;
        }

        mesh = new Mesh { name = "VisionCone" };
        mesh.MarkDynamic();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().sortingOrder = sortingOrder;
    }

    void LateUpdate()
    {
        Vector2 origin = sensor.transform.position;
        float baseAngle = sensor.transform.eulerAngles.z;
        float fov = sensor.ViewAngle;

        vertices[0] = transform.InverseTransformPoint(origin);

        for (int i = 0; i <= rayCount; i++)
        {
            float a = (baseAngle - fov * 0.5f + fov * i / rayCount) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));

            RaycastHit2D hit = Physics2D.Raycast(origin, dir, sensor.ViewDistance, sensor.WallMask);
            Vector2 point = hit.collider != null ? hit.point : origin + dir * sensor.ViewDistance;

            vertices[i + 1] = transform.InverseTransformPoint(point);
        }

        Color c = ColorFor(alertness.Alertness);
        for (int i = 0; i < colors.Length; i++) colors[i] = c;

        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.RecalculateBounds();
    }

    Color ColorFor(float t)
    {
        return t < 0.5f
            ? Color.Lerp(calm, suspicious, t / 0.5f)
            : Color.Lerp(suspicious, alert, (t - 0.5f) / 0.5f);
    }
}
