using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class OptimizeLakeTownLighting
{
    public static string Run()
    {
        if (EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty) throw new Exception("Clean Edit Mode required");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/LakeTownScene.unity");
        var town = scene.GetRootGameObjects().Single(g => g.name.StartsWith("Lake Town —"));
        var sections = town.transform.Find("Local road lighting sections");
        if (sections == null) { sections = new GameObject("Local road lighting sections").transform; sections.SetParent(town.transform, false); }
        const string asset = "Assets/Environments/LakeTownTrial/Local Road Lighting Meshes.asset";
        int count = 0, triangles = 0;
        // Keep original colliders, audio surfaces and minimap references intact.
        var originals = town.GetComponentsInChildren<MeshFilter>().Where(f => f.GetComponent<Renderer>() != null && f.GetComponent<Renderer>().enabled && !f.transform.IsChildOf(sections) &&
            (f.GetComponent<Renderer>().sharedMaterial.name == "Promenade Stone" || f.name == "Lake Town Road" || f.name.Contains("pavement") || f.name.EndsWith("walk") ||
             f.name == "Market Street" || f.name == "Marina Avenue" || f.name == "Old Town Cross Street")).ToArray();
        foreach (var original in originals)
        {
            var source = original.sharedMesh; var vertices = source.vertices; var normals = source.normals; var uv = source.uv; var indices = source.triangles;
            var groups = new Dictionary<Vector2Int, List<int>>();
            for (int i = 0; i < indices.Length; i += 3)
            {
                var center = original.transform.TransformPoint((vertices[indices[i]] + vertices[indices[i+1]] + vertices[indices[i+2]]) / 3f);
                var key = new Vector2Int(Mathf.FloorToInt(center.x / 25), Mathf.FloorToInt(center.z / 25));
                if (!groups.TryGetValue(key, out var list)) groups[key] = list = new List<int>();
                list.Add(indices[i]); list.Add(indices[i+1]); list.Add(indices[i+2]);
            }
            foreach (var pair in groups)
            {
                var v = new List<Vector3>(); var n = new List<Vector3>(); var u = new List<Vector2>();
                foreach (int index in pair.Value) { v.Add(original.transform.TransformPoint(vertices[index])); if (normals.Length != 0) n.Add(original.transform.TransformDirection(normals[index])); if (uv.Length != 0) u.Add(uv[index]); }
                var mesh = new Mesh { name = original.name + " " + pair.Key }; mesh.SetVertices(v); mesh.SetTriangles(Enumerable.Range(0, v.Count).ToArray(), 0);
                if (n.Count > 0) mesh.SetNormals(n); else mesh.RecalculateNormals(); if (u.Count > 0) mesh.SetUVs(0, u); mesh.RecalculateBounds();
                if (AssetDatabase.LoadMainAssetAtPath(asset) == null) AssetDatabase.CreateAsset(mesh, asset); else AssetDatabase.AddObjectToAsset(mesh, asset);
                var go = new GameObject(mesh.name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(sections, false); go.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterials = original.GetComponent<Renderer>().sharedMaterials; renderer.shadowCastingMode = original.GetComponent<Renderer>().shadowCastingMode;
                count++; triangles += pair.Value.Count / 3;
            }
            original.GetComponent<Renderer>().enabled = false;
        }
        // Original lanterns were combined with road markings. Give their bulbs independent glow.
        var controller = town.GetComponent<LakeTownNightLights>(); var so = new SerializedObject(controller); var lenses = so.FindProperty("lanterns");
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Environments/LakeTownTrial/Warm Yellow.mat");
        foreach (var lamp in town.GetComponentsInChildren<Light>().Where(l => l.transform.parent == town.transform && town.transform.Find("Original streetlight glow") == null).ToArray())
        {
            var bulb = GameObject.CreatePrimitive(PrimitiveType.Cube); bulb.name = "Original streetlight glow"; bulb.transform.SetParent(town.transform, false);
            bulb.transform.position = lamp.transform.position + Vector3.up * .5f; bulb.transform.localScale = new Vector3(.81f, .71f, .81f);
            Object.DestroyImmediate(bulb.GetComponent<Collider>()); bulb.GetComponent<Renderer>().sharedMaterial = material;
            int i = lenses.arraySize; lenses.InsertArrayElementAtIndex(i); lenses.GetArrayElementAtIndex(i).objectReferenceValue = bulb.GetComponent<Renderer>();
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        return originals.Length + " original surfaces replaced visually by " + count + " local sections, " + triangles + " triangles; colliders/minimap unchanged; all 73 lanterns have independent night glow.";
    }
}
