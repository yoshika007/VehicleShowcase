using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// Trees were batched as one 24-vertex trunk and two 24-vertex crowns each.
// Keep complete trees together when clearing the rock ridge and both portals.
public static class ClearMountainPassTunnelTrees
{
    static Mesh Pine => GameObject.Find("Batched Alpine pine").GetComponent<MeshFilter>().sharedMesh;
    static Mesh Bark => GameObject.Find("Batched Pine bark").GetComponent<MeshFilter>().sharedMesh;
    static Bounds Clearance()
    {
        var bounds=GameObject.Find("Summit rock ridge").GetComponent<Renderer>().bounds;
        bounds.Expand(new Vector3(8,8,8));
        return bounds;
    }
    static Bounds TreeBounds(int tree)
    {
        var v=Pine.vertices;
        var b=new Bounds(v[tree*48],Vector3.zero);
        for(int j=0;j<48;j++)b.Encapsulate(v[tree*48+j]);
        var trunks=Bark.vertices;
        for(int j=0;j<24;j++)b.Encapsulate(trunks[tree*24+j]);
        return b;
    }
    public static string Inspect()
    {
        if(Pine.vertexCount%48!=0 || Bark.vertexCount%24!=0 || Pine.vertexCount/48!=Bark.vertexCount/24)
            throw new Exception("Unexpected tree batching; aborting.");
        var clearance=Clearance();int count=Bark.vertexCount/24;
        var hits=Enumerable.Range(0,count).Where(i=>clearance.Intersects(TreeBounds(i))).ToArray();
        return "Trees="+count+", pine vertices="+Pine.vertexCount+", trunk vertices="+Bark.vertexCount+
            ", ridge clearance="+clearance+", overlapping whole trees="+hits.Length+", centers="+
            string.Join("; ",hits.Select(i=>TreeBounds(i).center.ToString()));
    }
    static void Filter(Mesh mesh,int stride,HashSet<int> remove)
    {
        var old=mesh.vertices;var normals=mesh.normals;var uv=mesh.uv;var tangents=mesh.tangents;var colors=mesh.colors;
        var keep=Enumerable.Range(0,old.Length).Where(i=>!remove.Contains(i/stride)).ToArray();
        var map=Enumerable.Repeat(-1,old.Length).ToArray();for(int i=0;i<keep.Length;i++)map[keep[i]]=i;
        var oldTriangles=mesh.triangles;var triangles=new List<int>();
        for(int i=0;i<oldTriangles.Length;i+=3)
        {
            int a=map[oldTriangles[i]],b=map[oldTriangles[i+1]],c=map[oldTriangles[i+2]];
            if(a>=0&&b>=0&&c>=0)triangles.AddRange(new[]{a,b,c});
            else if(a>=0||b>=0||c>=0)throw new Exception("Triangle crosses tree boundaries.");
        }
        mesh.Clear();mesh.vertices=keep.Select(i=>old[i]).ToArray();
        if(normals.Length==old.Length)mesh.normals=keep.Select(i=>normals[i]).ToArray();
        if(uv.Length==old.Length)mesh.uv=keep.Select(i=>uv[i]).ToArray();
        if(tangents.Length==old.Length)mesh.tangents=keep.Select(i=>tangents[i]).ToArray();
        if(colors.Length==old.Length)mesh.colors=keep.Select(i=>colors[i]).ToArray();
        mesh.triangles=triangles.ToArray();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
    }
    public static string Run()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(EditorApplication.isPlaying||scene.isDirty||scene.name!="MountainPassScene")
            throw new Exception("Open saved MountainPassScene outside Play Mode.");
        string before=Inspect();var clearance=Clearance();
        var remove=new HashSet<int>(Enumerable.Range(0,Bark.vertexCount/24).Where(i=>clearance.Intersects(TreeBounds(i))));
        Filter(Pine,48,remove);Filter(Bark,24,remove);
        AssetDatabase.SaveAssetIfDirty(Pine);AssetDatabase.SaveAssetIfDirty(Bark);
        return before+". Removed "+remove.Count+" complete trees. After: "+Inspect();
    }
}
