using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Remove only the ground-floor front window crossing each centered entrance.
// Existing lit/dark assignments and all surviving vertex attributes stay intact.
public static class FixLakeTownDoorWindows
{
    const string Dir = "Assets/Environments/LakeTownTrial/";
    sealed class Plan
    {
        public Mesh mesh;
        public int[] keep;
        public int[] removedDoors;
    }
    static Bounds MarkerBounds(Transform marker)
    {
        var bounds = new Bounds(marker.TransformPoint(new Vector3(-.5f, -.5f, -.5f)), Vector3.zero);
        foreach (float x in new[] { -.5f, .5f })
            foreach (float y in new[] { -.5f, .5f })
                foreach (float z in new[] { -.5f, .5f })
                    bounds.Encapsulate(marker.TransformPoint(new Vector3(x, y, z)));
        return bounds;
    }
    static Plan Inspect(Mesh mesh, Transform frame, Bounds[] doors)
    {
        var vertices = mesh.vertices;
        var triangles = mesh.triangles;
        int count = vertices.Length / 24;
        if (vertices.Length % 24 != 0 || triangles.Length != count * 36 || mesh.subMeshCount != 1)
            throw new Exception("Unexpected cube window mesh layout: " + mesh.name);
        var keep = new List<int>();
        var removed = new List<int>();
        for (int block = 0; block < count; block++)
        {
            int first = block * 24;
            for (int i = block * 36; i < (block + 1) * 36; i++)
                if (triangles[i] < first || triangles[i] >= first + 24)
                    throw new Exception("Window shares indices with other geometry.");
            var bounds = new Bounds(frame.TransformPoint(vertices[first]), Vector3.zero);
            for (int v = 1; v < 24; v++) bounds.Encapsulate(frame.TransformPoint(vertices[first + v]));
            var matches = Enumerable.Range(0, doors.Length).Where(d => doors[d].Intersects(bounds)).ToArray();
            if (matches.Length > 1) throw new Exception("Window overlaps multiple doors.");
            if (matches.Length == 1) removed.Add(matches[0]); else keep.Add(block);
        }
        return new Plan { mesh = mesh, keep = keep.ToArray(), removedDoors = removed.ToArray() };
    }
    static void Apply(Plan plan)
    {
        var mesh = plan.mesh;
        var positions = mesh.vertices;
        var normals = mesh.normals;
        var tangents = mesh.tangents;
        var colors = mesh.colors;
        var uv = new List<Vector4>[8];
        var dimensions = new int[8];
        for (int channel = 0; channel < 8; channel++)
        {
            uv[channel] = new List<Vector4>(); mesh.GetUVs(channel, uv[channel]);
            dimensions[channel] = mesh.GetVertexAttributeDimension((UnityEngine.Rendering.VertexAttribute)((int)UnityEngine.Rendering.VertexAttribute.TexCoord0 + channel));
        }
        var oldTriangles = mesh.triangles;
        var indices = plan.keep.SelectMany(w => Enumerable.Range(w * 24, 24)).ToArray();
        var triangles = plan.keep.SelectMany((w, k) => Enumerable.Range(0, 36)
            .Select(t => oldTriangles[w * 36 + t] - w * 24 + k * 24)).ToArray();
        Undo.RecordObject(mesh, "Clear Lake Town entrance windows");
        mesh.Clear();
        mesh.vertices = indices.Select(i => positions[i]).ToArray();
        if (normals.Length > 0) mesh.normals = indices.Select(i => normals[i]).ToArray();
        if (tangents.Length > 0) mesh.tangents = indices.Select(i => tangents[i]).ToArray();
        if (colors.Length > 0) mesh.colors = indices.Select(i => colors[i]).ToArray();
        for (int channel = 0; channel < 8; channel++)
            if (uv[channel].Count > 0)
            {
                var values = indices.Select(i => uv[channel][i]).ToArray();
                if (dimensions[channel] == 2) mesh.SetUVs(channel, values.Select(v => new Vector2(v.x, v.y)).ToList());
                else if (dimensions[channel] == 3) mesh.SetUVs(channel, values.Select(v => new Vector3(v.x, v.y, v.z)).ToList());
                else mesh.SetUVs(channel, values.ToList());
            }
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        if (!mesh.vertices.SequenceEqual(indices.Select(i => positions[i])) ||
            (normals.Length > 0 && !mesh.normals.SequenceEqual(indices.Select(i => normals[i]))))
            throw new Exception("Surviving window vertices or normals changed.");
        EditorUtility.SetDirty(mesh);
    }
    public static string Run()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.isDirty || scene.path != "Assets/Scenes/LakeTownScene.unity")
            throw new Exception("Clean Lake Town Edit Mode required.");
        var town = Object.FindAnyObjectByType<LakeTownNightLights>().transform;
        var doors = town.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Front door").ToArray();
        if (doors.Length != 51) throw new Exception("Expected 51 front doors.");
        var bounds = doors.Select(MarkerBounds).ToArray();
        var markers = town.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Window" &&
            bounds.Any(b => b.Intersects(MarkerBounds(t)))).ToArray();
        if (markers.Any(t => t.GetComponent<Renderer>() != null) || markers.Length != 51)
            throw new Exception("Expected 51 already-batched entrance window markers.");
        var source = AssetDatabase.LoadAssetAtPath<Mesh>(Dir + "SceneryBatch4.asset");
        var filters = town.GetComponentsInChildren<MeshFilter>(true)
            .Where(f => AssetDatabase.GetAssetPath(f.sharedMesh) == Dir + "House Window Pattern.asset").ToArray();
        if (filters.Length != 2) throw new Exception("Expected the original lit and dark window batches.");
        var plans = filters.Select(f => Inspect(f.sharedMesh, f.transform, bounds)).ToArray();
        var sourceFrame = town.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Scenery batch 4");
        var sourcePlan = Inspect(source, sourceFrame, bounds);
        var liveRemoved = plans.SelectMany(p => p.removedDoors).OrderBy(i => i).ToArray();
        if (!liveRemoved.SequenceEqual(Enumerable.Range(0, 51)) ||
            !sourcePlan.removedDoors.OrderBy(i => i).SequenceEqual(Enumerable.Range(0, 51)))
            throw new Exception("Expected exactly one overlapping front window per house in source and active batches.");
        foreach (var plan in plans.Concat(new[] { sourcePlan })) Apply(plan);
        foreach (var marker in markers) Undo.DestroyObjectImmediate(marker.gameObject);
        foreach (var filter in filters)
            if (Inspect(filter.sharedMesh, filter.transform, bounds).removedDoors.Length != 0)
                throw new Exception("A front door still overlaps a rendered window.");
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        return "Cleared all 51 door overlaps. Remaining windows: " + string.Join(", ", plans.Select(p =>
            p.mesh.name + " = " + p.keep.Length)) + "; 459 total. Door geometry, side/rear/upstairs windows, and retained night assignments unchanged.";
    }
}
