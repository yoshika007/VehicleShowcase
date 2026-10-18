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
        position.x = Mathf.Clamp(position.x, rect.xMin + 13f, rect.xMax - 13f);
        position.y = Mathf.Clamp(position.y, rect.yMin + 13f, rect.yMax - 13f);
        Vector2 forward = new Vector2(vehicle.forward.x, vehicle.forward.z).normalized;
        if (forward.sqrMagnitude < 0.01f) forward = Vector2.up;
        DrawArrow(mesh, position, forward, 1.25f, new Color(0.02f, 0.025f, 0.03f));
        DrawArrow(mesh, position, forward, 1f, markerColor);
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
