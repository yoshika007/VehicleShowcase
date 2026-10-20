using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// Upgrade existing wet-trial shoulders so their visible soil also supports vehicles.
public static class FixWetTrialBanks
{
    static Vector3 MudCenter(float u) => new Vector3(155 + 80 * u, 0, -30 - 40 * Mathf.Pow(Mathf.Sin(Mathf.PI * u), 2));

    public static string Main()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.isDirty || scene.path != "Assets/Scenes/TestTrackScene.unity")
            throw new Exception("Clean TestTrackScene in Edit mode required");
        foreach (string name in new[] { "Outer wet trial bank", "Inner wet trial bank" })
        {
            var go = GameObject.Find("Offroad Course/Mud and Water Trial/" + name);
            if (go == null || go.GetComponent<MeshFilter>().sharedMesh == null)
                throw new Exception("Missing bank mesh: " + name);
        }
        foreach (string name in new[] { "Outer wet trial bank", "Inner wet trial bank" })
        {
            var go = GameObject.Find("Offroad Course/Mud and Water Trial/" + name);
            var mesh = go.GetComponent<MeshFilter>().sharedMesh;
            // Preserve the bank footprint and underlying terrain height, reducing only
            // the narrow decorative ridge that could high-center a low-clearance car.
            const int across = 4;
            var vertices = mesh.vertices;
            if (vertices.Length != 73 * across) throw new Exception("Unexpected bank topology: " + name);
            Undo.RecordObject(mesh, "Make wet-trial shoulder traversable");
            for (int i = 0; i <= 72; i++)
            {
                float u = Mathf.Lerp(.20f, .80f, i / 72f);
                float fade = Mathf.SmoothStep(0, 1, Mathf.Min((u - .20f) / .06f, (.80f - u) / .06f));
                for (int j = 0; j < across; j++)
                {
                    // Rebuild the base from unchanged edge vertices, avoiding cumulative lowering on reruns.
                    float baseHeight = Mathf.Lerp(vertices[i * across].y, vertices[i * across + 3].y,
                        (new[] { 6f, 7.3f, 9f, 11f }[j] - 6f) / 5f);
                    vertices[i * across + j].y = baseHeight + new[] { 0f, .13f, .08f, 0f }[j] * fade;
                }
            }
            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            var collider = go.GetComponent<MeshCollider>();
            if (collider == null) collider = Undo.AddComponent<MeshCollider>(go);
            Undo.RecordObject(collider, "Match bank collision to visible soil");
            collider.sharedMesh = null;
            collider.sharedMesh = mesh;
            collider.convex = false;
            collider.isTrigger = false;
            EditorUtility.SetDirty(collider);
        }
        // The original rectangular cutout extended beyond the mud and underneath
        // the banks. Keep the deep floor under the mud, but bring exposed dry
        // approaches back to ground level so cars cannot enter below the bank edge.
        var pocket = GameObject.Find("Mud pocket ground");
        var floor = pocket.GetComponent<MeshFilter>().sharedMesh;
        Undo.RecordObject(floor, "Support dry approaches around wet-trial banks");
        var floorVertices = floor.vertices;
        for (int i = 0; i < floorVertices.Length; i++)
        {
            Vector3 world = pocket.transform.TransformPoint(floorVertices[i]);
            float distance = float.PositiveInfinity;
            for (int j = 0; j < 80; j++)
            {
                Vector3 a = MudCenter(Mathf.Lerp(.29f, .44f, j / 80f));
                Vector3 b = MudCenter(Mathf.Lerp(.29f, .44f, (j + 1) / 80f));
                Vector3 horizontal = new Vector3(world.x, 0, world.z);
                Vector3 segment = b - a;
                Vector3 nearest = a + segment * Mathf.Clamp01(Vector3.Dot(horizontal - a, segment) / segment.sqrMagnitude);
                distance = Mathf.Min(distance, Vector3.Distance(horizontal, nearest));
            }
            world.y = Mathf.Max(world.y, Mathf.Lerp(-.65f, -.05f,
                Mathf.SmoothStep(0, 1, Mathf.InverseLerp(6f, 8f, distance))));
            floorVertices[i] = pocket.transform.InverseTransformPoint(world);
        }
        floor.vertices = floorVertices;
        floor.RecalculateNormals();
        floor.RecalculateBounds();
        EditorUtility.SetDirty(floor);
        var floorCollider = pocket.GetComponent<MeshCollider>();
        floorCollider.sharedMesh = null;
        floorCollider.sharedMesh = floor;
        Physics.SyncTransforms();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "Both wet-trial banks now have gentler crests and matching static collision. Mud, water, materials and vehicle tuning preserved.";
    }
}
