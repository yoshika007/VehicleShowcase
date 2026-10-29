using System.Collections.Generic;
using UnityEngine;

// Runtime-only walls keep Challenge on the circuit without changing Free Run's authored exits.
public sealed class CleanLapCircuitBarrier : MonoBehaviour
{
    private const float EdgeOffset = 1.6f;
    private const float WallThickness = .6f;
    private const float WallHeight = 1.55f;
    private const float BelowRoad = .15f;
    private const float JointOverlap = .16f;
    private Mesh[] generatedMeshes;
    private Material[] generatedMaterials;

    // Static batching may replace MeshFilter.sharedMesh with a non-readable combined
    // render mesh. MeshCollider retains the original authored collision mesh.
    public static Mesh SourceMesh(MeshFilter filter)
    {
        var collider = filter != null ? filter.GetComponent<MeshCollider>() : null;
        var mesh = collider != null ? collider.sharedMesh : null;
        if (mesh == null || !mesh.isReadable)
            throw new System.InvalidOperationException("Original readable collision mesh is missing for " + (filter != null ? filter.name : "course"));
        return mesh;
    }
    public static GameObject Create(MeshFilter circuit)
    {
        var vertices = SourceMesh(circuit).vertices;
        if (vertices.Length < 8 || vertices.Length % 2 != 0) return null;
        int rows = vertices.Length / 2;
        // The authored ribbon repeats its first inner/outer pair at the end.
        if ((vertices[0] - vertices[vertices.Length - 2]).sqrMagnitude < .0001f
            && (vertices[1] - vertices[vertices.Length - 1]).sqrMagnitude < .0001f)
            rows--;
        if (rows < 3) return null;

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (shader == null) return null;
        var root = new GameObject("Challenge Circuit Barriers");
        var owner = root.AddComponent<CleanLapCircuitBarrier>();
        owner.generatedMaterials = new[]
        {
            MaterialFor(shader, "Challenge barrier concrete", new Color(.82f, .87f, .86f)),
            MaterialFor(shader, "Challenge barrier teal", new Color(.035f, .42f, .43f)),
            MaterialFor(shader, "Challenge barrier reflectors", new Color(1f, .72f, .17f))
        };
        owner.generatedMeshes = new Mesh[2];
        // Two source spans per section keeps the curved walls smooth with only 256 boxes.
        for (int edge = 0; edge < 2; edge++)
        {
            var positions = new Vector3[rows];
            for (int i = 0; i < rows; i++)
            {
                Vector3 localEdge = vertices[i * 2 + edge];
                Vector3 localOther = vertices[i * 2 + 1 - edge];
                Vector3 position = circuit.transform.TransformPoint(localEdge);
                Vector3 across = position - circuit.transform.TransformPoint(localOther);
                across.y = 0f;
                // Preserve authored curb/runoff clearance when the circuit is scaled.
                float localWidth = Vector3.Distance(localEdge, localOther);
                float scale = localWidth > .001f ? across.magnitude / localWidth : 1f;
                positions[i] = position + across.normalized * (EdgeOffset * scale);
            }
            var geometry = new WallMesh();
            var edgeRoot = new GameObject(edge == 0 ? "Inside circuit barrier" : "Outside circuit barrier");
            edgeRoot.transform.SetParent(root.transform, false);
            edgeRoot.layer = circuit.gameObject.layer;
            int section = 0;
            int stride = rows >= 128 ? 2 : 1;
            for (int i = 0; i < rows; i += stride)
            {
                int next = i + stride < rows ? i + stride : 0;
                Vector3 a = positions[i], b = positions[next];
                Vector3 forward = b - a;
                forward.y = 0f;
                float length = forward.magnitude;
                if (length < .001f) continue;
                forward /= length;
                Vector3 right = Vector3.Cross(Vector3.up, forward);
                Vector3 center = (a + b) * .5f;
                center.y += WallHeight * .5f - BelowRoad;
                // Overlap joints so the exit and both complete loops have no drive-through gaps.
                var colliderObject = new GameObject("Wall section " + section);
                colliderObject.layer = circuit.gameObject.layer;
                colliderObject.transform.SetParent(edgeRoot.transform, false);
                colliderObject.transform.SetPositionAndRotation(center, Quaternion.LookRotation(forward));
                colliderObject.AddComponent<BoxCollider>().size = new Vector3(WallThickness, WallHeight, length + JointOverlap);
                geometry.Box(center, right, forward, new Vector3(WallThickness, WallHeight, length + JointOverlap), 0);
                // A raised teal band and occasional gold reflectors remain legible at driving speed.
                Vector3 stripe = center + Vector3.up * (WallHeight * .5f - .22f);
                geometry.Box(stripe, right, forward, new Vector3(WallThickness + .018f, .16f, length + JointOverlap), 1);
                if (section % 4 == 0)
                    geometry.Box(center + Vector3.up * .08f, right, forward, new Vector3(WallThickness + .028f, .9f, .18f), 2);
                section++;
            }
            var mesh = geometry.Build(edge == 0 ? "Inner challenge wall mesh" : "Outer challenge wall mesh");
            owner.generatedMeshes[edge] = mesh;
            edgeRoot.AddComponent<MeshFilter>().sharedMesh = mesh;
            edgeRoot.AddComponent<MeshRenderer>().sharedMaterials = owner.generatedMaterials;
        }
        return root;
    }

    public static float GetInsideClearance(float roadScale) => Mathf.Max(0, EdgeOffset * roadScale - WallThickness * .5f);

    private static Material MaterialFor(Shader shader, string label, Color color)
    {
        var material = new Material(shader) { name = label, color = color };
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .16f);
        return material;
    }

    private void OnDestroy()
    {
        if (generatedMeshes != null)
            foreach (var mesh in generatedMeshes) Release(mesh);
        if (generatedMaterials != null)
            foreach (var material in generatedMaterials) Release(material);
    }

    private static void Release(Object resource)
    {
        if (resource == null) return;
        if (Application.isPlaying) Destroy(resource);
        else DestroyImmediate(resource);
    }

    private sealed class WallMesh
    {
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Vector2> uv = new List<Vector2>();
        private readonly List<int>[] triangles = { new List<int>(), new List<int>(), new List<int>() };
        private static readonly int[] Faces =
        {
            0, 3, 2, 1, 4, 5, 6, 7, 0, 4, 7, 3,
            1, 2, 6, 5, 0, 1, 5, 4, 3, 7, 6, 2
        };

        public void Box(Vector3 center, Vector3 right, Vector3 forward, Vector3 size, int material)
        {
            Vector3 r = right * (size.x * .5f), u = Vector3.up * (size.y * .5f), f = forward * (size.z * .5f);
            var corners = new[]
            {
                center-r-u-f, center+r-u-f, center+r+u-f, center-r+u-f,
                center-r-u+f, center+r-u+f, center+r+u+f, center-r+u+f
            };
            for (int face = 0; face < 6; face++)
            {
                int first = vertices.Count;
                for (int corner = 0; corner < 4; corner++) vertices.Add(corners[Faces[face * 4 + corner]]);
                uv.Add(Vector2.zero); uv.Add(Vector2.up); uv.Add(Vector2.one); uv.Add(Vector2.right);
                triangles[material].Add(first); triangles[material].Add(first + 1); triangles[material].Add(first + 2);
                triangles[material].Add(first); triangles[material].Add(first + 2); triangles[material].Add(first + 3);
            }
        }

        public Mesh Build(string label)
        {
            var mesh = new Mesh { name = label, subMeshCount = triangles.Length };
            if (vertices.Count > 65535) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            for (int i = 0; i < triangles.Length; i++) mesh.SetTriangles(triangles[i], i);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
