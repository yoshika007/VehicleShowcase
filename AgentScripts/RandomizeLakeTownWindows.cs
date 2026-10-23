using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class RandomizeLakeTownWindows
{
    static Mesh Subset(Mesh source, int[] windows, string name)
    {
        var vertices = source.vertices; var normals = source.normals; var uv = source.uv; var tangents = source.tangents; var triangles = source.triangles;
        var v = new List<Vector3>(); var n = new List<Vector3>(); var u = new List<Vector2>(); var t = new List<int>(); var tan = new List<Vector4>();
        foreach (int window in windows)
        {
            int start = v.Count, first = window * 24;
            for (int i = 0; i < 24; i++) { v.Add(vertices[first+i]); if(normals.Length>0)n.Add(normals[first+i]); if(uv.Length>0)u.Add(uv[first+i]); if(tangents.Length>0)tan.Add(tangents[first+i]); }
            for (int i = 0; i < 36; i++) t.Add(start + triangles[window * 36 + i] - first);
        }
        var mesh = new Mesh { name = name }; mesh.SetVertices(v); mesh.SetTriangles(t,0); if(n.Count>0)mesh.SetNormals(n); if(u.Count>0)mesh.SetUVs(0,u); if(tan.Count>0)mesh.SetTangents(tan); mesh.RecalculateBounds();return mesh;
    }
    public static string Run()
    {
        if(EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty)throw new Exception("Clean Edit Mode required");
        var scene = SceneManager.GetActiveScene(); if(scene.path != "Assets/Scenes/LakeTownScene.unity")throw new Exception("Open Lake Town first");
        var controller = Object.FindAnyObjectByType<LakeTownNightLights>(); var so = new SerializedObject(controller); var array = so.FindProperty("windows");
        var original = (Renderer)array.GetArrayElementAtIndex(0).objectReferenceValue;
        var source = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Environments/LakeTownTrial/SceneryBatch4.asset");
        if(source.name != "SceneryBatch4")throw new Exception("Windows already split or edited");
        int count = source.vertexCount/24; var triangles = source.triangles;
        if(source.vertexCount%24!=0 || triangles.Length!=count*36)throw new Exception("Unexpected window mesh format");
        for(int w=0;w<count;w++)for(int i=0;i<36;i++)if(triangles[w*36+i]<w*24 || triangles[w*36+i]>=w*24+24)throw new Exception("Unexpected window indices");
        var order = Enumerable.Range(0,count).ToArray(); var random = new System.Random(7347);
        for(int i=order.Length-1;i>0;i--){int j=random.Next(i+1);int temp=order[i];order[i]=order[j];order[j]=temp;}
        int litCount = Mathf.RoundToInt(count*.6f);
        var lit = Subset(source,order.Take(litCount).OrderBy(i=>i).ToArray(),"Lit house windows — 60 percent");
        var dark = Subset(source,order.Skip(litCount).OrderBy(i=>i).ToArray(),"Dark house windows — 40 percent");
        const string path="Assets/Environments/LakeTownTrial/House Window Pattern.asset";
        var savedLit = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        var savedDark = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().FirstOrDefault(m => m != savedLit);
        if (savedLit == null) { AssetDatabase.CreateAsset(lit,path); AssetDatabase.AddObjectToAsset(dark,path); }
        else
        {
            EditorUtility.CopySerialized(lit,savedLit); EditorUtility.CopySerialized(dark,savedDark);
            Object.DestroyImmediate(lit); Object.DestroyImmediate(dark); lit=savedLit; dark=savedDark;
            EditorUtility.SetDirty(lit); EditorUtility.SetDirty(dark);
        }
        var darkRenderer = GameObject.Find("Scenery batch 4").GetComponent<Renderer>();
        darkRenderer.GetComponent<MeshFilter>().sharedMesh=dark;
        var go=GameObject.Find("Lit house windows");
        if(go==null) { go=new GameObject("Lit house windows",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(darkRenderer.transform,false);go.isStatic=darkRenderer.gameObject.isStatic; }
        go.GetComponent<MeshFilter>().sharedMesh=lit;
        var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterials=darkRenderer.sharedMaterials;renderer.shadowCastingMode=darkRenderer.shadowCastingMode;renderer.receiveShadows=darkRenderer.receiveShadows;
        array.arraySize=1;array.GetArrayElementAtIndex(0).objectReferenceValue=renderer;so.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return litCount+" glowing windows, "+(count-litCount)+" dark windows; deterministic seed 7347, all "+count+" window meshes preserved.";
    }
}
