using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class RaiseDirtHillOne
{
    // Rounded 64m mound: flat joins, gentle approach, and enough crest curvature for a fast jump.
    public static float Height(float x) => 4.2f * Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Clamp01((x - 163f) / 64f)), 4);
    private static void Refresh(MeshFilter filter)
    {
        var mesh = filter.sharedMesh;
        mesh.RecalculateNormals(); mesh.RecalculateBounds(); mesh.UploadMeshData(false);
        EditorUtility.SetDirty(mesh);
        if (filter.TryGetComponent<MeshCollider>(out var collider))
        {
            collider.sharedMesh = null; collider.sharedMesh = mesh;
        }
    }
    public static string Main()
    {
        if (EditorApplication.isPlaying) throw new Exception("Edit Mode required");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/TestTrackScene.unity" || scene.isDirty) throw new Exception("Clean TestTrackScene required");
        var root = GameObject.Find("Offroad Course").transform;
        var hill = root.Find("Dirt Hill 1").GetComponent<MeshFilter>();
        var banks = root.GetComponentsInChildren<MeshFilter>().Single(f => f.name == "Rounded main loop banks");
        var bankCollider = banks.GetComponent<MeshCollider>();
        var old = hill.sharedMesh.vertices;
        var centers = Enumerable.Range(0, old.Length / 2).Select(i => hill.transform.TransformPoint((old[i * 2] + old[i * 2 + 1]) * .5f)).ToArray();
        Func<float,float> oldHeight = x =>
        {
            int row = 0;
            while (row < centers.Length - 2 && centers[row + 1].x < x) row++;
            return Mathf.Lerp(centers[row].y, centers[row + 1].y, Mathf.InverseLerp(centers[row].x, centers[row + 1].x, x));
        };
        var markers = root.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Trail edge marker"
            && t.position.x >= 155 && t.position.x <= 235 && t.position.z < -8).ToArray();
        var oldGround = markers.Select(t => bankCollider.Raycast(new Ray(t.position + Vector3.up * 30, Vector3.down), out var hit, 60) ? hit.point.y : float.NaN).ToArray();
        Undo.RecordObject(hill.sharedMesh, "Raise Dirt Hill 1");
        var roadVertices = hill.sharedMesh.vertices;
        for (int i = 0; i < roadVertices.Length; i++)
        {
            var p = hill.transform.TransformPoint(roadVertices[i]);
            p.y = .04f + Height(p.x);
            roadVertices[i] = hill.transform.InverseTransformPoint(p);
        }
        hill.sharedMesh.vertices = roadVertices; Refresh(hill);
        Undo.RecordObject(banks.sharedMesh, "Blend raised hill into existing banks");
        var vertices = banks.sharedMesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            var p = banks.transform.TransformPoint(vertices[i]);
            if (p.x < 155 || p.x > 235 || p.z >= -8) continue;
            float before = oldHeight(p.x);
            float blend = Mathf.Clamp01((p.y + .055f) / (before + .03f));
            p.y += (.04f + Height(p.x) - before) * blend;
            vertices[i] = banks.transform.InverseTransformPoint(p);
        }
        banks.sharedMesh.vertices = vertices; Refresh(banks);
        Physics.SyncTransforms();
        int moved = 0;
        for (int i = 0; i < markers.Length; i++)
        {
            if (float.IsNaN(oldGround[i])) continue;
            var marker = markers[i];
            if (!bankCollider.Raycast(new Ray(marker.position + Vector3.up * 30, Vector3.down), out var hit, 60)) continue;
            Undo.RecordObject(marker, "Keep trail markers on raised banks");
            marker.position += Vector3.up * (hit.point.y - oldGround[i]);
            moved++;
        }
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        return $"Dirt Hill 1 raised from {centers.Max(p => p.y) - .04f:0.0}m to 4.2m; blended banks and {moved} marker heights updated. Marker active states and vehicle tuning preserved.";
    }
}
