using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// Adds visual delineation using the current rail meshes, including the technical bends.
// Collision geometry and the road width stay unchanged. Safe to rerun.
public static class ImproveMountainPassRails
{
    const string Dir = "Assets/Environments/MountainPass/";
    static Material Material(string name, Color color, float emission)
    {
        string path = Dir + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.color = color;
        m.SetFloat("_Smoothness", .28f);
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", color * emission);
        EditorUtility.SetDirty(m);
        return m;
    }
    static void Quad(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        int n=v.Count; v.AddRange(new[]{a,b,c,d});
        t.AddRange(new[]{n,n+1,n+2,n+2,n+1,n+3,n+2,n+1,n,n+3,n+1,n+2});
    }
    public static string Run()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(EditorApplication.isPlaying || scene.isDirty || scene.name!="MountainPassScene")
            throw new Exception("Open a saved MountainPassScene outside Play Mode.");
        var silver=Material("Visible galvanized rail",new Color(.8f,.84f,.85f),.035f);
        var white=Material("Rail pale edge",new Color(.92f,.93f,.85f),.16f);
        var amber=Material("Rail amber reflector",new Color(1f,.60f,.12f),.4f);
        int markers=0;
        foreach(string name in new[]{"Left safety rail","Right safety rail"})
        {
            var rail=GameObject.Find(name);
            var points=rail.GetComponent<MeshFilter>().sharedMesh.vertices;
            rail.GetComponent<Renderer>().sharedMaterial=silver;
            var v=new List<Vector3>();var top=new List<int>();var reflectors=new List<int>();
            float distance=0;
            for(int i=0;i<points.Length/2-1;i++)
            {
                var a=points[2*i+1];var b=points[2*i+3];
                var tangent=(b-a).normalized;
                var normal=Vector3.Cross(Vector3.up,tangent).normalized*.025f;
                // Thin raised edging on both faces avoids coplanar flicker.
                foreach(float side in new[]{-1f,1f})
                    Quad(v,top,a+normal*side,b+normal*side,a+normal*side-Vector3.up*.1f,b+normal*side-Vector3.up*.1f);
                distance+=Vector3.Distance(a,b);
                if(distance<12)continue;
                distance-=12;markers++;
                var center=(a+b)*.5f-Vector3.up*.28f;
                foreach(float side in new[]{-1f,1f})
                    Quad(v,reflectors,center-tangent*.24f+Vector3.up*.13f+normal*side,
                        center+tangent*.24f+Vector3.up*.13f+normal*side,
                        center-tangent*.24f-Vector3.up*.13f+normal*side,
                        center+tangent*.24f-Vector3.up*.13f+normal*side);
            }
            string path=Dir+name+" delineation.asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!mesh){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
            mesh.name=name+" delineation";mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(v);mesh.subMeshCount=2;mesh.SetTriangles(top,0);mesh.SetTriangles(reflectors,1);
            mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
            var child=rail.transform.Find("Visible edge and reflectors");
            var go=child?child.gameObject:new GameObject("Visible edge and reflectors",typeof(MeshFilter),typeof(MeshRenderer));
            go.transform.SetParent(rail.transform,false);go.isStatic=true;
            go.GetComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterials=new[]{white,amber};
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return "Both rails brightened; pale upper edges and "+markers+" amber markers added, with no collision changes.";
    }
}
