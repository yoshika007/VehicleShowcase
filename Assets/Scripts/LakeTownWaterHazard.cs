using UnityEngine;

// Sample the oriented chassis against the actual lake outline, including partial entry.
public class LakeTownWaterHazard : MonoBehaviour
{
    public enum Submersion { Dry, Shallow, Deep }

    [SerializeField] private Mesh waterOutline;
    [SerializeField, Min(0f)] private float entryDepth = .15f;
    [SerializeField, Min(0f)] private float deepDepth = .60f;
    private TestDriveMovement vehicle;
    private BoxCollider chassis;
    private Vector3[] vertices;
    private int[] triangles;
    private readonly Vector3[] corners = new Vector3[8];

    public void BindVehicle(TestDriveMovement target)
    {
        vehicle = target;
        chassis = target != null ? target.GetComponent<BoxCollider>() : null;
        if (vertices == null && waterOutline != null)
        {
            vertices = waterOutline.vertices;
            triangles = waterOutline.triangles;
        }
    }

    public Submersion GetSubmersion()
    {
        if (vehicle == null || !vehicle.isActiveAndEnabled || chassis == null ||
            !chassis.enabled || vertices == null || vertices.Length == 0) return Submersion.Dry;
        float waterHeight = transform.TransformPoint(vertices[0]).y;
        var center = chassis.transform.TransformPoint(chassis.center);
        if (IsUnderwater(center, waterHeight, Mathf.Max(entryDepth, deepDepth))) return Submersion.Deep;
        if (IsUnderwater(center, waterHeight, entryDepth)) return Submersion.Shallow;

        // Use the four lowest real corners, so rolled and upside-down cars also work.
        // An axis-aligned bounds corner can lie outside the actual rotated chassis.
        var extent = chassis.size * .5f;
        int index = 0;
        for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
                for (int z = -1; z <= 1; z += 2)
                {
                    var point = chassis.transform.TransformPoint(chassis.center +
                        Vector3.Scale(extent, new Vector3(x, y, z)));
                    int slot = index++;
                    while (slot > 0 && corners[slot - 1].y > point.y)
                    {
                        corners[slot] = corners[slot - 1];
                        slot--;
                    }
                    corners[slot] = point;
                }
        int wet = 0;
        for (int i = 0; i < 4; i++)
            if (IsUnderwater(corners[i], waterHeight, entryDepth) && ++wet >= 2)
                return Submersion.Shallow;
        return Submersion.Dry;
    }

    public bool IsSubmerged => GetSubmersion() != Submersion.Dry;

    private bool IsUnderwater(Vector3 worldPoint, float waterHeight, float depth)
    {
        if (worldPoint.y > waterHeight - depth) return false;
        var point = transform.InverseTransformPoint(worldPoint);
        var p = new Vector2(point.x, point.z);
        for (int i = 0; i < triangles.Length; i += 3)
        {
            var a = vertices[triangles[i]]; var b = vertices[triangles[i + 1]]; var c = vertices[triangles[i + 2]];
            // Degenerate triangles must not classify arbitrary points as lake water.
            if (Mathf.Abs(Cross(new Vector2(a.x, a.z), b, c)) < .000001f) continue;
            float x = Cross(p, a, b), y = Cross(p, b, c), z = Cross(p, c, a);
            if (!((x < 0f || y < 0f || z < 0f) && (x > 0f || y > 0f || z > 0f))) return true;
        }
        return false;
    }

    private static float Cross(Vector2 p, Vector3 a, Vector3 b)
        => (p.x - b.x) * (a.z - b.z) - (a.x - b.x) * (p.y - b.z);
}
