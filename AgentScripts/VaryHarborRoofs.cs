using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

// Stable roof-by-roof palette assignment; leaves all building geometry intact.
public static class VaryHarborRoofs
{
    const string Dir="Assets/Environments/HarborStreet/Roofs";
    static readonly string[] Names={"Weathered terracotta","Clay red","Sandy tile","Slate blue","Charcoal slate","Patina green"};
    static readonly Color[] Colors={new Color(.51f,.24f,.17f),new Color(.39f,.15f,.12f),new Color(.56f,.43f,.29f),new Color(.25f,.31f,.36f),new Color(.24f,.25f,.27f),new Color(.28f,.37f,.32f)};
    public static string Run()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.name!="HarborStreetScene"||scene.isDirty||EditorApplication.isPlaying)throw new Exception("Expected saved HarborStreetScene in Edit Mode.");
        var renderer=Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include).Single(r=>r.name=="Batched Terracotta roofs");
        var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;
        if(mesh.subMeshCount!=1||mesh.vertexCount%24!=0)throw new Exception("Expected original cube roof batch.");
        var vertices=mesh.vertices;var triangles=mesh.triangles;int count=vertices.Length/24;
        if(triangles.Length!=count*36)throw new Exception("Unexpected roof topology.");
        var bins=Names.Select(_=>new List<int>()).ToArray();var counts=new int[Names.Length];var random=new System.Random(26026);
        var palette=Enumerable.Range(0,count).Select(i=>i%Names.Length).OrderBy(_=>random.Next()).ToArray();
        for(int i=0;i<triangles.Length;i+=3){int roof=triangles[i]/24;if(triangles[i+1]/24!=roof||triangles[i+2]/24!=roof)throw new Exception("Triangle crosses roofs.");bins[palette[roof]].AddRange(new[]{triangles[i],triangles[i+1],triangles[i+2]});}
        Capture("before");
        if(!AssetDatabase.IsValidFolder(Dir))AssetDatabase.CreateFolder("Assets/Environments/HarborStreet","Roofs");
        var original=renderer.sharedMaterial;var materials=new Material[Names.Length];
        for(int i=0;i<materials.Length;i++){materials[i]=new Material(original){name=Names[i],color=Colors[i]};AssetDatabase.CreateAsset(materials[i],Dir+"/"+Names[i]+".mat");counts[i]=palette.Count(p=>p==i);}
        Undo.RegisterCompleteObjectUndo(mesh,"Vary Harbor Street roof palette");Undo.RecordObject(renderer,"Assign Harbor roof palette");
        mesh.subMeshCount=materials.Length;for(int i=0;i<bins.Length;i++)mesh.SetTriangles(bins[i],i,false);renderer.sharedMaterials=materials;
        if(!mesh.vertices.SequenceEqual(vertices)||!mesh.triangles.OrderBy(i=>i).SequenceEqual(triangles.OrderBy(i=>i)))throw new Exception("Geometry changed unexpectedly.");
        EditorUtility.SetDirty(mesh);EditorUtility.SetDirty(renderer);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Capture("after");
        return count+" roofs; "+string.Join(", ",Names.Select((n,i)=>n+"="+counts[i]))+"; geometry unchanged; six material batches, one existing renderer.";
    }
    public static void Capture(string suffix)
    {
        var dir="/Users/yoshi/Documents/ChatGPT/Unity Project/Artifacts/HarborStreet/Roofs";Directory.CreateDirectory(dir);
        var go=new GameObject("Temporary roof preview camera",typeof(Camera));var cam=go.GetComponent<Camera>();cam.enabled=false;cam.fieldOfView=55;cam.nearClipPlane=.5f;cam.farClipPlane=1800;cam.aspect=1.6f;
        var rt=new RenderTexture(1600,1000,24){antiAliasing=4};var previous=RenderTexture.active;
        try{cam.targetTexture=rt;cam.transform.position=new Vector3(-260,230,-330);cam.transform.LookAt(new Vector3(-10,15,-20));cam.Render();RenderTexture.active=rt;var tex=new Texture2D(1600,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,1000),0,0);tex.Apply();File.WriteAllBytes(dir+"/"+suffix+".png",tex.EncodeToPNG());Object.DestroyImmediate(tex);}
        finally{RenderTexture.active=previous;Object.DestroyImmediate(go);rt.Release();Object.DestroyImmediate(rt);}
    }
}
