using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Object=UnityEngine.Object;

// Reshape only the existing red/ivory road-edge blocks near curved sections.
public static class SmoothHarborCurbs
{
    struct Block { public int start; public int index; public int sign; }
    static float Turn(Vector3[] centers,int i){int n=centers.Length;return Vector3.Angle(centers[i]-centers[(i+n-2)%n],centers[(i+2)%n]-centers[i]);}
    public static string Run()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.name!="HarborStreetScene"||scene.isDirty||EditorApplication.isPlaying)throw new Exception("Open saved HarborStreetScene outside Play Mode.");
        var road=GameObject.Find("Harbor Street asphalt").GetComponent<MeshFilter>().sharedMesh.vertices;int n=road.Length/2-1;
        var centers=Enumerable.Range(0,n).Select(i=>(road[2*i]+road[2*i+1])*.5f).ToArray();
        var side=Enumerable.Range(0,n).Select(i=>(road[2*i+1]-road[2*i]).normalized).ToArray();
        var corner=new bool[n];for(int i=0;i<n;i++)if(Turn(centers,i)>2.0f)for(int d=-5;d<=5;d++)corner[(i+d+n)%n]=true;
        var results=new List<string>();var planned=new List<(MeshFilter filter,Vector3[] vertices,Vector3[] normals,List<Block> blocks)>();
        foreach(string name in new[]{"Batched Racing red","Batched Barrier ivory"})
        {
            var filter=GameObject.Find(name).GetComponent<MeshFilter>();var mesh=filter.sharedMesh;var v=mesh.vertices;var normals=mesh.normals;
            if(v.Length%24!=0||normals.Length!=v.Length)throw new Exception("Unexpected batch layout: "+name);
            var blocks=new List<Block>();var used=new HashSet<int>();int expected=0;float maxDistance=0;
            for(int i=0;i<n;i+=2)foreach(int sign in new[]{-1,1})
            {
                if((name.Contains("red"))!=(i%4==0))continue;
                int j=(i+1)%n;var position=(centers[i]+centers[j])*.5f+side[i]*sign*5.45f+Vector3.up*.045f;
                int best=-1;float dist=float.MaxValue;for(int k=0;k<v.Length;k+=24){Vector3 c=Vector3.zero;for(int z=0;z<24;z++)c+=filter.transform.TransformPoint(v[k+z]);c/=24;float d=Vector3.Distance(c,position);if(d<dist){dist=d;best=k;}}
                if(dist>.02f||!used.Add(best))throw new Exception(name+" unmatched or duplicate road marking at "+i+" distance="+dist);
                expected++;maxDistance=Mathf.Max(dist,maxDistance);
                if(corner[i]||corner[(i+1)%n]||corner[(i+2)%n])blocks.Add(new Block{start=best,index=i,sign=sign});
            }
            if(expected!=n/2)throw new Exception("Unexpected marking count: "+expected);
            planned.Add((filter,v,normals,blocks));results.Add(name+": "+blocks.Count+" curved markings / "+expected+" total");
        }
        foreach(var item in planned)
        {
            var mesh=item.filter.sharedMesh;var vertices=item.vertices;var original=(Vector3[])vertices.Clone();
            var changed=new HashSet<int>();
            foreach(var block in item.blocks)
            {
                int i=block.index,prev=(i+n-1)%n,next=(i+1)%n,next2=(i+2)%n;
                var forward=(centers[next]-centers[prev]).normalized;var rotation=Quaternion.LookRotation(forward);var inverse=Quaternion.Inverse(rotation);
                var oldCenter=(centers[i]+centers[next])*.5f+side[i]*block.sign*5.45f+Vector3.up*.045f;
                for(int j=0;j<24;j++)
                {
                    int index=block.start+j;var local=inverse*(item.filter.transform.TransformPoint(original[index])-oldCenter);
                    bool end=local.z>0;int a=end?next:prev,b=end?next2:i;
                    var center=(centers[a]+centers[b])*.5f;var lateral=(side[a]+side[b]).normalized;
                    // Adjacent colored segments share these mitered cross-sections exactly.
                    float lateralOffset=block.sign*5.45f+(local.x>0?.175f:-.175f);
                    var p=center+lateral*lateralOffset+Vector3.up*(local.y>0?.09f:0f);
                    vertices[index]=item.filter.transform.InverseTransformPoint(p);changed.Add(index);
                }
            }
            for(int i=0;i<vertices.Length;i++)if(!changed.Contains(i)&&vertices[i]!=original[i])throw new Exception("Non-curb geometry changed");
            var temporary=Object.Instantiate(mesh);temporary.vertices=vertices;temporary.RecalculateNormals();var recalculated=temporary.normals;var normals=item.normals;
            foreach(int index in changed)normals[index]=recalculated[index];Object.DestroyImmediate(temporary);
            Undo.RegisterCompleteObjectUndo(mesh,"Smooth Harbor Street corner curbs");mesh.vertices=vertices;mesh.normals=normals;mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssetIfDirty(mesh);
        }
        return string.Join("; ",results)+". Other batched geometry, colors, colliders, road and scene objects preserved.";
    }
}
