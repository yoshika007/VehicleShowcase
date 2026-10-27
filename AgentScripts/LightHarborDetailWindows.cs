using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class LightHarborDetailWindows
{
    const string Dir="Assets/Environments/HarborStreet/StreetDetails";
    static Mesh WindowSubset(Mesh source,int[] selected,string name)
    {
        var vertices=source.vertices;var normals=source.normals;var uv=source.uv;var tangents=source.tangents;var triangles=source.triangles;
        var v=new List<Vector3>();var n=new List<Vector3>();var u=new List<Vector2>();var tan=new List<Vector4>();var t=new List<int>();
        foreach(int window in selected)
        {
            int first=window*24,start=v.Count;
            for(int j=0;j<24;j++)
            {
                v.Add(vertices[first+j]);
                if(normals.Length==vertices.Length)n.Add(normals[first+j]);
                if(uv.Length==vertices.Length)u.Add(uv[first+j]);
                if(tangents.Length==vertices.Length)tan.Add(tangents[first+j]);
            }
            for(int j=0;j<36;j++)t.Add(start+triangles[window*36+j]-first);
        }
        var mesh=new Mesh{name=name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
        mesh.SetVertices(v);mesh.SetTriangles(t,0);if(n.Count>0)mesh.SetNormals(n);if(u.Count>0)mesh.SetUVs(0,u);if(tan.Count>0)mesh.SetTangents(tan);
        mesh.RecalculateBounds();return mesh;
    }
    public static string Run()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.name!="HarborStreetScene"||scene.isDirty||EditorApplication.isPlaying)throw new Exception("Use saved HarborStreetScene outside Play Mode");
        var root=GameObject.Find("Harbor street details").transform;
        if(root.Find("Lit background windows"))throw new Exception("Already configured");
        var renderer=root.Find("Distant blue window street details").GetComponent<MeshRenderer>();
        var source=renderer.GetComponent<MeshFilter>().sharedMesh;int count=source.vertexCount/24;
        if(source.vertexCount%24!=0||source.triangles.Length!=count*36)throw new Exception("Unexpected window format");
        var order=Enumerable.Range(0,count).ToArray();var random=new System.Random(7347);
        for(int i=order.Length-1;i>0;i--){int j=random.Next(i+1);int temp=order[i];order[i]=order[j];order[j]=temp;}
        var chosen=new HashSet<int>(order.Take(Mathf.RoundToInt(count*.6f)));
        var lit=WindowSubset(source,Enumerable.Range(0,count).Where(chosen.Contains).ToArray(),"Lit background windows - 60 percent");
        var dark=WindowSubset(source,Enumerable.Range(0,count).Where(i=>!chosen.Contains(i)).ToArray(),"Dark background windows - 40 percent");
        if(lit.vertexCount+dark.vertexCount!=source.vertexCount)throw new Exception("Window count changed");
        AssetDatabase.CreateAsset(lit,Dir+"/Background window pattern.asset");AssetDatabase.AddObjectToAsset(dark,lit);
        Undo.RecordObject(renderer.GetComponent<MeshFilter>(),"Randomize background window lighting");renderer.GetComponent<MeshFilter>().sharedMesh=dark;
        var go=new GameObject("Lit background windows",typeof(MeshFilter),typeof(MeshRenderer));Undo.RegisterCreatedObjectUndo(go,"Add background night windows");go.transform.SetParent(root,false);go.isStatic=renderer.gameObject.isStatic;go.GetComponent<MeshFilter>().sharedMesh=lit;
        var litRenderer=go.GetComponent<MeshRenderer>();litRenderer.sharedMaterials=renderer.sharedMaterials;litRenderer.shadowCastingMode=renderer.shadowCastingMode;litRenderer.receiveShadows=renderer.receiveShadows;
        var controller=GameObject.Find("Harbor Street Course").GetComponent<LakeTownNightLights>();var data=new SerializedObject(controller);var windows=data.FindProperty("windows");
        int oldCount=windows.arraySize;windows.arraySize=oldCount+1;windows.GetArrayElementAtIndex(oldCount).objectReferenceValue=litRenderer;data.ApplyModifiedProperties();
        if(!data.FindProperty("nightWindows").objectReferenceValue)throw new Exception("Missing existing night material");
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return count+" new background windows preserved: "+chosen.Count+" lit, "+(count-chosen.Count)+" dark; seed 7347, same existing night material/controller; existing "+oldCount+" window batch preserved.";
    }
    public static async Task<string> Check()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Check in Play Mode");int before=Time.frameCount;
        for(int i=0;i<5;i++){EditorApplication.Step();await Task.Delay(30);}
        var root=GameObject.Find("Harbor street details").transform;
        var lit=root.Find("Lit background windows").GetComponent<MeshRenderer>();var dark=root.Find("Distant blue window street details").GetComponent<MeshRenderer>();
        var data=new SerializedObject(GameObject.Find("Harbor Street Course").GetComponent<LakeTownNightLights>());
        var expected=TestDriveSelection.IsNight?(Material)data.FindProperty("nightWindows").objectReferenceValue:AssetDatabase.LoadAssetAtPath<Material>(Dir+"/Distant blue window.mat");
        if(lit.sharedMaterial!=expected||dark.sharedMaterial!=AssetDatabase.LoadAssetAtPath<Material>(Dir+"/Distant blue window.mat"))throw new Exception("Wrong day/night materials");
        if(Time.frameCount<=before)throw new Exception("Frames did not advance");
        return (TestDriveSelection.IsNight?"Night":"Day")+": lit subset material="+lit.sharedMaterial.name+", dark subset remains blue; frame "+before+" -> "+Time.frameCount;
    }
    public static async Task<string> Night()
    {
        TestDriveSelection.Select(0,lighting:TestDriveSelection.TimeOfDay.Night,course:TestDriveSelection.CourseKind.HarborStreet);
        UnityEngine.SceneManagement.SceneManager.LoadScene("HarborStreetScene");
        for(int i=0;i<8;i++){EditorApplication.Step();await Task.Delay(30);}
        return await Check();
    }
}
