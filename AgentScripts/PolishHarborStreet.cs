using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class PolishHarborStreet
{
    const string Dir="Assets/Environments/HarborStreet/";
    public static string Run()
    {
        if(EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Use a saved scene outside Play Mode.");
        if(AssetDatabase.LoadAssetAtPath<Mesh>(Dir+"City terrace foundations.asset")!=null)throw new Exception("Polish already applied; refusing duplicate foundations.");
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/HarborStreetScene.unity");
        int fixedMeshes=0;
        foreach(string name in new[]{"City retaining walls","Left city safety wall","Right city safety wall","Seaside covered tunnel"})
        {
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Dir+name+".asset");var old=mesh.vertices;var triangles=mesh.triangles;
            var vertices=new Vector3[triangles.Length];var indices=new int[triangles.Length];
            for(int i=0;i<triangles.Length;i++){vertices[i]=old[triangles[i]];indices[i]=i;}
            mesh.Clear();mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.vertices=vertices;mesh.triangles=indices;
            mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssetIfDirty(mesh);fixedMeshes++;
        }
        var temp=GameObject.CreatePrimitive(PrimitiveType.Cube);var cube=temp.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(temp);
        var parts=new List<CombineInstance>();
        foreach(string facade in new[]{"Warm cream facades","Blush facades","Ochre facades","Pearl facades"})
        {
            var vertices=AssetDatabase.LoadAssetAtPath<Mesh>(Dir+facade+" scenery.asset").vertices;
            for(int i=0;i<vertices.Length;i+=24)
            {
                var b=new Bounds(vertices[i],Vector3.zero);for(int j=1;j<24;j++)b.Encapsulate(vertices[i+j]);
                if(b.min.y<.01f)continue;
                var center=new Vector3(b.center.x,(b.min.y-.4f)*.5f,b.center.z);
                parts.Add(new CombineInstance{mesh=cube,transform=Matrix4x4.TRS(center,Quaternion.identity,new Vector3(b.size.x,b.min.y+.4f,b.size.z))});
            }
        }
        var foundation=new Mesh{name="City terrace foundations",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};foundation.CombineMeshes(parts.ToArray());
        AssetDatabase.CreateAsset(foundation,Dir+"City terrace foundations.asset");
        var g=new GameObject("City terrace foundations",typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(GameObject.Find("Harbor Street Course").transform,false);g.isStatic=true;
        g.GetComponent<MeshFilter>().sharedMesh=foundation;g.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Dir+"Coastal concrete.mat");
        foreach(var c in GameObject.Find("Harbor Street Course").GetComponentsInChildren<MeshCollider>()){var mesh=c.sharedMesh;c.sharedMesh=null;c.sharedMesh=mesh;}
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssetIfDirty(foundation);
        return "Corrected "+fixedMeshes+" double-sided meshes; added "+parts.Count+" solid building foundations.";
    }
}
