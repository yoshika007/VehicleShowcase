using UnityEngine;

// Tests the real lake outline and chassis depth, without blocking driving or pier access.
public class LakeTownWaterHazard : MonoBehaviour
{
    [SerializeField] private Mesh waterOutline;
    private TestDriveMovement vehicle;
    private BoxCollider chassis;
    private Vector3[] vertices;
    private int[] triangles;

    public void BindVehicle(TestDriveMovement target)
    {
        vehicle = target;
        chassis = target != null ? target.GetComponent<BoxCollider>() : null;
        if (vertices == null)
        {
            vertices = waterOutline.vertices;
            triangles = waterOutline.triangles;
        }
    }

    public bool IsSubmerged
    {
        get
        {
            if (vehicle == null || !vehicle.isActiveAndEnabled || chassis == null || vertices == null) return false;
            float waterHeight = transform.TransformPoint(vertices[0]).y;
            if (chassis.bounds.min.y >= waterHeight - .08f) return false;
            var point = transform.InverseTransformPoint(chassis.bounds.center);
            var p = new Vector2(point.x, point.z);
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var a = vertices[triangles[i]]; var b = vertices[triangles[i + 1]]; var c = vertices[triangles[i + 2]];
                float x = Cross(p, a, b), y = Cross(p, b, c), z = Cross(p, c, a);
                if (!((x < 0f || y < 0f || z < 0f) && (x > 0f || y > 0f || z > 0f))) return true;
            }
            return false;
        }
    }

    private static float Cross(Vector2 p, Vector3 a, Vector3 b)
        => (p.x - b.x) * (a.z - b.z) - (a.x - b.x) * (p.y - b.z);
}
