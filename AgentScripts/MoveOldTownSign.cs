using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class MoveOldTownSign
{
    public static string Run()
    {
        if (EditorApplication.isPlaying) throw new Exception("Edit Mode required");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/LakeTownScene.unity") throw new Exception("Open Lake Town first");
        var board = GameObject.Find("OLD TOWN board").transform;
        var post = GameObject.Find("OLD TOWN post").transform;
        if (Vector3.Distance(board.position, new Vector3(90,4,-22)) > .01f) throw new Exception("Sign already moved or manually edited");
        var batch = Object.FindAnyObjectByType<LakeTownNightLights>().GetComponentsInChildren<MeshFilter>().Single(f => f.sharedMesh.name == "SceneryBatch8");
        var mesh = batch.sharedMesh; var vertices = mesh.vertices; var normals = mesh.normals; var tangents = mesh.tangents;
        var indices = Enumerable.Range(0,vertices.Length).Where(i => { var p = board.InverseTransformPoint(batch.transform.TransformPoint(vertices[i])); return Mathf.Abs(p.x) < .501f && Mathf.Abs(p.y) < .501f && Mathf.Abs(p.z) < .501f; }).ToArray();
        if (indices.Length != 24) throw new Exception("Could not isolate board geometry");
        var oldMatrix = board.worldToLocalMatrix; var oldRotation = board.rotation;
        board.position = new Vector3(101,4,-30); board.rotation = Quaternion.Euler(0,45,0);
        post.position = new Vector3(101,2,-30);
        var rotation = board.rotation * Quaternion.Inverse(oldRotation);
        foreach (int i in indices)
        {
            vertices[i] = batch.transform.InverseTransformPoint(board.TransformPoint(oldMatrix.MultiplyPoint3x4(batch.transform.TransformPoint(vertices[i]))));
            if (normals.Length > 0) normals[i] = batch.transform.InverseTransformDirection(rotation * batch.transform.TransformDirection(normals[i]));
            if (tangents.Length > 0) { var t = tangents[i]; var d = batch.transform.InverseTransformDirection(rotation * batch.transform.TransformDirection(new Vector3(t.x,t.y,t.z))); tangents[i] = new Vector4(d.x,d.y,d.z,t.w); }
        }
        mesh.vertices = vertices; if (normals.Length > 0) mesh.normals = normals; if (tangents.Length > 0) mesh.tangents = tangents; mesh.RecalculateBounds();
        // Validate every visible board vertex against the actual driving surfaces.
        var roads = Object.FindAnyObjectByType<LakeTownNightLights>().GetComponentsInChildren<MeshCollider>().Where(c => c.GetComponent<TestDriveAudioSurface>() != null).ToArray();
        foreach (int i in indices) { var p = batch.transform.TransformPoint(vertices[i]); p.y=20; if (roads.Any(c => c.Raycast(new Ray(p,Vector3.down),out var hit,30))) throw new Exception("Board still overlaps a road"); }
        EditorUtility.SetDirty(mesh); AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        return "Moved post, board and lettering to (101, -30), yaw 45 degrees facing southwest toward northbound drivers. All board corners clear the road.";
    }
}
