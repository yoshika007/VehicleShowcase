using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

// Separate the catalog's red rear lenses from the shared emissive slot at authoring time.
// Palette coordinates belong to this free model pack. Nothing is changed in its source meshes.
public static class SetupRearDrivingLights
{
    const string Folder = "Assets/Prefabs/TestDrive/Lighting";
    static List<int> RearTriangles(Mesh mesh)
    {
        var vertices = mesh.vertices;
        var uv = mesh.uv;
        var triangles = mesh.GetTriangles(1);
        var selected = new List<int>();
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
            Vector3 center = (vertices[a] + vertices[b] + vertices[c]) / 3f;
            Vector2 sample = (uv[a] + uv[b] + uv[c]) / 3f;
            bool redLens = sample.x > .75f && sample.x < .82f && sample.y < .08f;
            bool rearQuarter = center.z < mesh.bounds.center.z - mesh.bounds.size.z * .25f;
            bool belowRoof = center.y < mesh.bounds.min.y + mesh.bounds.size.y * .72f;
            if (redLens && rearQuarter && belowRoof) selected.AddRange(new[] { a, b, c });
        }
        if (selected.Count == 0 || !selected.Any(i => vertices[i].x < -.1f)
            || !selected.Any(i => vertices[i].x > .1f))
            throw new Exception("Expected paired red rear lenses in " + mesh.name);
        return selected;
    }
    public static string Main()
    {
        if (EditorApplication.isPlaying) throw new Exception("Edit mode required");
        var catalog = AssetDatabase.LoadAssetAtPath<TestDriveVehicleCatalog>("Assets/Prefabs/TestDrive/VehicleCatalog.asset");
        foreach (var prefab in catalog.drivingPrefabs)
        {
            if (prefab.GetComponent<TestDriveRearLights>() != null) throw new Exception("Already configured: " + prefab.name);
            RearTriangles(prefab.GetComponent<MeshFilter>().sharedMesh);
        }
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Prefabs/TestDrive", "Lighting");
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Rear Signal Lamps" };
        material.EnableKeyword("_EMISSION");
        material.SetColor("_BaseColor", new Color(.28f, .008f, .004f));
        material.SetColor("_EmissionColor", new Color(.084f, .0024f, .0012f));
        material.SetFloat("_Smoothness", .65f);
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        AssetDatabase.CreateAsset(material, Folder + "/Rear Signal Lamps.mat");
        var report = new List<string>();
        foreach (var prefab in catalog.drivingPrefabs)
        {
            string path = AssetDatabase.GetAssetPath(prefab);
            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var filter = contents.GetComponent<MeshFilter>();
                var original = filter.sharedMesh;
                var selected = RearTriangles(original);
                var body = Object.Instantiate(original);
                body.name = prefab.name + " body with independent rear lenses";
                var removed = new HashSet<int>();
                var triangles = original.GetTriangles(1);
                var vertices = original.vertices;
                var uv = original.uv;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    var center = (vertices[triangles[i]] + vertices[triangles[i+1]] + vertices[triangles[i+2]]) / 3f;
                    var sample = (uv[triangles[i]] + uv[triangles[i+1]] + uv[triangles[i+2]]) / 3f;
                    if (sample.x > .75f && sample.x < .82f && sample.y < .08f
                        && center.z < original.bounds.center.z - original.bounds.size.z * .25f
                        && center.y < original.bounds.min.y + original.bounds.size.y * .72f)
                        removed.Add(i / 3);
                }
                body.SetTriangles(Enumerable.Range(0, triangles.Length / 3).Where(i => !removed.Contains(i))
                    .SelectMany(i => new[] { triangles[i*3], triangles[i*3+1], triangles[i*3+2] }).ToArray(), 1);
                AssetDatabase.CreateAsset(body, Folder + "/" + prefab.name + " Body.asset");
                filter.sharedMesh = body;
                var lenses = new Mesh { name = prefab.name + " Rear Lenses" };
                lenses.vertices = selected.Select(i => vertices[i]).ToArray();
                lenses.normals = selected.Select(i => original.normals[i]).ToArray();
                lenses.triangles = Enumerable.Range(0, selected.Count).ToArray();
                lenses.RecalculateBounds();
                AssetDatabase.CreateAsset(lenses, Folder + "/" + prefab.name + " Rear Lenses.asset");
                var lamp = new GameObject("Brake and Reverse Lamps", typeof(MeshFilter), typeof(MeshRenderer));
                lamp.transform.SetParent(filter.transform, false);
                lamp.GetComponent<MeshFilter>().sharedMesh = lenses;
                var renderer = lamp.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                var component = contents.AddComponent<TestDriveRearLights>();
                var serialized = new SerializedObject(component);
                serialized.FindProperty("rearLamps").objectReferenceValue = renderer;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contents, path);
                report.Add(prefab.name + ": " + selected.Count / 3 + " rear lens triangles");
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }
        AssetDatabase.SaveAssets();
        return string.Join("\n", report);
    }
}
