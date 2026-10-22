using UnityEngine;

// Draws the existing road mesh as a lightweight, north-up UI map.
public class TestDriveMinimap : UnityEngine.UI.MaskableGraphic
{
    [SerializeField] private Mesh roadMesh;
    [SerializeField] private Transform roadTransform;
    [SerializeField] private Transform vehicle;
    [SerializeField] private Vector3 startLineA;
    [SerializeField] private Vector3 startLineB;
    [SerializeField] private Color markerColor = new Color(0.98f, 0.77f, 0.25f);

    private Mesh cachedMesh;
    private Vector3[] roadVertices;
    private int[] roadTriangles;
    private Bounds worldBounds;
    private Vector3 previousPosition;
    private Vector3 previousForward;

    public void SetVehicle(Transform target)
    {
        vehicle = target;
        SetVerticesDirty();
    }

    private void LateUpdate()
    {
        if (vehicle == null) return;
        if (vehicle.position == previousPosition && vehicle.forward == previousForward) return;
        previousPosition = vehicle.position;
        previousForward = vehicle.forward;
        SetVerticesDirty();
    }

    private bool CacheRoad()
    {
        if (roadMesh == null || roadTransform == null) return false;
        if (cachedMesh == roadMesh) return true;
        cachedMesh = roadMesh;
        roadVertices = roadMesh.vertices;
        roadTriangles = roadMesh.triangles;
        worldBounds = new Bounds(roadTransform.TransformPoint(roadVertices[0]), Vector3.zero);
        foreach (var vertex in roadVertices)
            worldBounds.Encapsulate(roadTransform.TransformPoint(vertex));
        return true;
    }

    public Vector2 WorldToMapPosition(Vector3 worldPosition)
    {
        if (!CacheRoad()) return rectTransform.rect.center;
        Rect rect = rectTransform.rect;
        float scale = Mathf.Min((rect.width - 32f) / Mathf.Max(1f, worldBounds.size.x),
            (rect.height - 32f) / Mathf.Max(1f, worldBounds.size.z));
        Vector3 offset = worldPosition - worldBounds.center;
        return rect.center + new Vector2(offset.x, offset.z) * scale;
    }

    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
    {
        mesh.Clear();
        if (!CacheRoad()) return;

        foreach (var vertex in roadVertices)
            mesh.AddVert(WorldToMapPosition(roadTransform.TransformPoint(vertex)), color, Vector2.zero);
        for (int i = 0; i < roadTriangles.Length; i += 3)
            mesh.AddTriangle(roadTriangles[i], roadTriangles[i + 1], roadTriangles[i + 2]);

        DrawLine(mesh, WorldToMapPosition(startLineA), WorldToMapPosition(startLineB), 2f, Color.white);
        if (vehicle == null) return;

        Vector2 position = WorldToMapPosition(vehicle.position);
        Rect rect = rectTransform.rect;
        position.x = Mathf.Clamp(position.x, rect.xMin + 22f, rect.xMax - 22f);
        position.y = Mathf.Clamp(position.y, rect.yMin + 22f, rect.yMax - 22f);
        Vector2 forward = new Vector2(vehicle.forward.x, vehicle.forward.z).normalized;
        if (forward.sqrMagnitude < 0.01f) forward = Vector2.up;
        // A contrasting locator stays readable over both road lines and the dark map background.
        DrawDisc(mesh, position, 20f, new Color(1f, 1f, 1f, .95f));
        DrawDisc(mesh, position, 17f, new Color(.02f, .025f, .03f, .96f));
        DrawArrow(mesh, position, forward, 1.65f, Color.white);
        DrawArrow(mesh, position, forward, 1.4f, markerColor);
    }

    private static void DrawDisc(UnityEngine.UI.VertexHelper mesh, Vector2 center, float radius, Color tint)
    {
        const int segments = 32;
        int first = mesh.currentVertCount;
        mesh.AddVert(center, tint, Vector2.zero);
        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            mesh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, tint, Vector2.zero);
        }
        for (int i = 0; i < segments; i++)
            mesh.AddTriangle(first, first + 1 + i, first + 1 + (i + 1) % segments);
    }

    private static void DrawLine(UnityEngine.UI.VertexHelper mesh, Vector2 a, Vector2 b, float width, Color tint)
    {
        Vector2 side = new Vector2(-(b - a).y, (b - a).x).normalized * width * 0.5f;
        int index = mesh.currentVertCount;
        mesh.AddVert(a - side, tint, Vector2.zero);
        mesh.AddVert(a + side, tint, Vector2.zero);
        mesh.AddVert(b + side, tint, Vector2.zero);
        mesh.AddVert(b - side, tint, Vector2.zero);
        mesh.AddTriangle(index, index + 1, index + 2);
        mesh.AddTriangle(index, index + 2, index + 3);
    }

    private static void DrawArrow(UnityEngine.UI.VertexHelper mesh, Vector2 position, Vector2 forward, float scale, Color tint)
    {
        Vector2 right = new Vector2(forward.y, -forward.x);
        int index = mesh.currentVertCount;
        mesh.AddVert(position + forward * (10f * scale), tint, Vector2.zero);
        mesh.AddVert(position - forward * (7f * scale) - right * (6f * scale), tint, Vector2.zero);
        mesh.AddVert(position - forward * (3f * scale), tint, Vector2.zero);
        mesh.AddVert(position - forward * (7f * scale) + right * (6f * scale), tint, Vector2.zero);
        mesh.AddTriangle(index, index + 1, index + 2);
        mesh.AddTriangle(index, index + 2, index + 3);
    }
}
