using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Builds matching timber fences and continuous, two-sided collision boundaries.
public static class AddPineFence
{
    const string DirectoryPath = "Assets/Environments/PineRidge/Fence";
    static Mesh SaveMesh(string path, string name, Action<Mesh> build)
    {
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null)
        {
            mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            AssetDatabase.CreateAsset(mesh, path);
        }
        else mesh.Clear();
        build(mesh); EditorUtility.SetDirty(mesh); return mesh;
    }
    static void BuildSide(Scene scene, MeshFilter bank, Vector3[] vertices, Mesh cube, Material material, bool outside)
    {
        string sideName = outside ? "Outer" : "Inner";
        string name = sideName + " Trail Wooden Fence";
        var existing = scene.GetRootGameObjects().FirstOrDefault(g => g.name == name);
        int rows = vertices.Length / 4 - 1;
        var posts = new List<Vector3>();
        for (int i = 0; i < rows; i += 3)
        {
            Vector3 edge = bank.transform.TransformPoint(vertices[i * 4 + (outside ? 3 : 0)]);
            Vector3 adjacent = bank.transform.TransformPoint(vertices[i * 4 + (outside ? 2 : 1)]);
            Vector3 outward = edge - adjacent; outward.y = 0; outward.Normalize();
            Vector3 p = edge + outward * .35f;
            var ground = Physics.RaycastAll(p + Vector3.up * 30f, Vector3.down, 60f)
                .Where(hit => hit.collider.gameObject.scene == scene &&
                    (hit.collider.name == "Forest terrain" || hit.collider.name == "Trail earth banks"))
                .OrderByDescending(hit => hit.point.y).FirstOrDefault();
            if (ground.collider == null) throw new Exception("Could not find ground below " + sideName + " fence post " + i);
            p.y = ground.point.y - .06f; posts.Add(p);
        }
        var visual = new List<CombineInstance>();
        foreach (var p in posts)
            visual.Add(new CombineInstance { mesh = cube, transform = Matrix4x4.TRS(p + Vector3.up * .72f, Quaternion.identity, new Vector3(.22f, 1.44f, .22f)) });
        var wallVertices = new List<Vector3>(); var wallTriangles = new List<int>();
        for (int i = 0; i < posts.Count; i++)
        {
            Vector3 a = posts[i], b = posts[(i + 1) % posts.Count], delta = b - a;
            if (delta.sqrMagnitude < .001f) throw new Exception("Duplicate " + sideName + " fence posts.");
            foreach (float height in new[] { .55f, 1.12f })
                visual.Add(new CombineInstance { mesh = cube, transform = Matrix4x4.TRS((a + b) * .5f + Vector3.up * height, Quaternion.LookRotation(delta), new Vector3(.14f, .16f, delta.magnitude + .12f)) });
            // The invisible collision wall extends slightly below ground and above
            // the rails, preventing the chassis slipping through or under them.
            int n = wallVertices.Count;
            wallVertices.Add(a - Vector3.up * .5f); wallVertices.Add(a + Vector3.up * 2f);
            wallVertices.Add(b - Vector3.up * .5f); wallVertices.Add(b + Vector3.up * 2f);
            wallTriangles.AddRange(new[] { n,n+1,n+2,n+1,n+3,n+2, n+2,n+1,n,n+2,n+3,n+1 });
        }
        string prefix = DirectoryPath + "/" + sideName + " trail ";
        var visualMesh = SaveMesh(prefix + "wooden fence.asset", sideName + " trail wooden fence", m => m.CombineMeshes(visual.ToArray()));
        var wallMesh = SaveMesh(prefix + "fence collision.asset", sideName + " trail fence collision", m =>
        {
            m.SetVertices(wallVertices); m.SetTriangles(wallTriangles, 0); m.RecalculateBounds();
        });
        var root = existing != null ? existing : new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        if (existing == null) SceneManager.MoveGameObjectToScene(root, scene);
        root.GetComponent<MeshFilter>().sharedMesh = visualMesh;
        root.GetComponent<MeshRenderer>().sharedMaterial = material;
        var collider = root.GetComponent<MeshCollider>();
        if (collider == null) collider = root.AddComponent<MeshCollider>();
        collider.sharedMesh = null; collider.sharedMesh = wallMesh; collider.convex = false;
        root.isStatic = true;
    }
    public static string Run()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != "Assets/Scenes/PineRidgeScene.unity")
            throw new Exception("Open Pine Ridge in Edit Mode.");
        var trail = scene.GetRootGameObjects().Single(g => g.name == "Pine Ridge Trail");
        var bank = trail.transform.Find("Trail earth banks").GetComponent<MeshFilter>();
        var vertices = bank.sharedMesh.vertices;
        if (vertices.Length < 12 || vertices.Length % 4 != 0) throw new Exception("Unexpected bank mesh layout.");
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Environments/PineRidge/Details/Trail timber.mat");
        if (material == null) throw new Exception("Trail timber material not found.");
        if (!AssetDatabase.IsValidFolder(DirectoryPath)) AssetDatabase.CreateFolder("Assets/Environments/PineRidge", "Fence");
        var primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var cube = primitive.GetComponent<MeshFilter>().sharedMesh; Object.DestroyImmediate(primitive);
        BuildSide(scene, bank, vertices, cube, material, true);
        BuildSide(scene, bank, vertices, cube, material, false);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        return "Built grounded inner and outer timber fences, each with a continuous two-sided collision wall.";
    }
}
