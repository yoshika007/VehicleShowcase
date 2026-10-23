using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Remove only misplaced seating from the combined timber batch; keep waterfront benches.
public static class RemoveMisplacedLakeTownBenches
{
    public static string Run()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.isDirty || scene.path != "Assets/Scenes/LakeTownScene.unity")
            throw new Exception("Clean Lake Town Edit Mode required");
        var town = Object.FindAnyObjectByType<LakeTownNightLights>().transform;
        var batch = town.GetComponentsInChildren<MeshFilter>().Single(f => f.name == "Scenery batch 5");
        if (batch.GetComponent<Renderer>().sharedMaterial.name != "Timber") throw new Exception("Unexpected batch material");
        // The first is stranded on the town side; the second overlaps the main driving road.
        var positions = new[] { new Vector3(11, .55f, 96), new Vector3(73, .55f, 112) };
        var benches = positions.Select(p => town.GetComponentsInChildren<Transform>().Single(t =>
            t.name == "Promenade bench" && Vector3.Distance(t.position, p) < .01f)).ToArray();
        var primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var source = primitive.GetComponent<MeshFilter>().sharedMesh.vertices;
        Object.DestroyImmediate(primitive);
        var mesh = batch.sharedMesh;
        var vertices = mesh.vertices;
        var removed = new HashSet<int>();
        foreach (var bench in benches)
        {
            var expected = source.Select(v => batch.transform.InverseTransformPoint(bench.TransformPoint(v))).ToArray();
            var indices = Enumerable.Range(0, vertices.Length).Where(i => expected.Any(v => (v - vertices[i]).sqrMagnitude < .0000001f)).ToArray();
            if (indices.Length != 24) throw new Exception("Could not isolate complete bench at " + bench.position);
            removed.UnionWith(indices);
        }
        var original = mesh.triangles;
        var kept = new List<int>();
        int removedTriangles = 0;
        for (int i = 0; i < original.Length; i += 3)
        {
            int count = Enumerable.Range(0, 3).Count(j => removed.Contains(original[i + j]));
            if (count != 0 && count != 3) throw new Exception("Bench shares triangles with other scenery");
            if (count == 3) removedTriangles++;
            else kept.AddRange(new[] { original[i], original[i + 1], original[i + 2] });
        }
        if (removedTriangles != 24) throw new Exception("Expected exactly two complete cube benches");
        // Retain every vertex attribute and all other triangles, including manually adjusted trees.
        mesh.SetTriangles(kept, 0);
        if (!mesh.vertices.SequenceEqual(vertices) || !mesh.triangles.SequenceEqual(kept))
            throw new Exception("Unexpected mesh alteration");
        EditorUtility.SetDirty(mesh);
        foreach (var bench in benches) Object.DestroyImmediate(bench.gameObject);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "Removed two misplaced benches at (11,96) and (73,112), exactly 24 triangles. Four waterfront benches retained; all other timber geometry and vertex attributes preserved.";
    }
}
