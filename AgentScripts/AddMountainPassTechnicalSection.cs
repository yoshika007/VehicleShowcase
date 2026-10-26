using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

// Local, continuous deformation of the lower return straight. Road, terrain,
// collision meshes, markings, guardrails, scenery and the minimap stay together.
public static class AddMountainPassTechnicalSection
{
    const string Dir="Assets/Environments/MountainPass";
    static Vector3 Warp(Vector3 p)
    {
        float u=Mathf.InverseLerp(-70f,170f,p.x);
        if(u<=0||u>=1)return p;
        float weight=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-270,-325,p.z))
            *Mathf.SmoothStep(0,1,Mathf.InverseLerp(-500,-430,p.z));
        float envelope=Mathf.Sin(Mathf.PI*u);
        p.z+=24f*Mathf.Sin(3*Mathf.PI*u)*envelope*envelope*weight;
        return p;
    }
    static Vector3 WarpNormal(Vector3 position,Vector3 normal)
    {
        // A transformed surface normal is perpendicular to both transformed tangents.
        var tangent=Vector3.Cross(normal,Mathf.Abs(normal.y)<.9f?Vector3.up:Vector3.right).normalized;
        var bitangent=Vector3.Cross(normal,tangent).normalized;
        var origin=Warp(position);
        return Vector3.Cross(Warp(position+tangent*.1f)-origin,Warp(position+bitangent*.1f)-origin).normalized;
    }
    static Material mat(string name)=>AssetDatabase.LoadAssetAtPath<Material>(Dir+"/"+name+".mat");
    static GameObject Box(Transform root,string name,Vector3 p,Vector3 size,Material material)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(root,false);
        g.transform.position=p;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=material;
        UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());g.isStatic=true;return g;
    }
    static void Board(Transform root,string text,Vector3 p,Vector3 forward,float width=8)
    {
        var side=Vector3.Cross(Vector3.up,forward).normalized;
        var location=p+side*9.5f;
        Physics.Raycast(location+Vector3.up*20,Vector3.down,out var hit,50);
        if(hit.collider!=null)location.y=hit.point.y;
        Box(root,"Technical warning post",location+Vector3.up*1.7f,new Vector3(.16f,3.4f,.16f),mat("Galvanized rail"));
        var board=Box(root,"Technical corner board",location+Vector3.up*3.1f,new Vector3(width,1.65f,.15f),mat("Sign forest"));
        board.transform.rotation=Quaternion.LookRotation(forward);
        var g=new GameObject("Technical sign lettering",typeof(TextMeshPro));g.transform.SetParent(root,false);
        g.transform.SetPositionAndRotation(board.transform.position-forward*.1f,board.transform.rotation);
        var t=g.GetComponent<TextMeshPro>();t.text=text;t.fontSize=5.5f;t.color=mat("Safety gold").color;
        t.alignment=TextAlignmentOptions.Center;t.rectTransform.sizeDelta=new Vector2(width-.3f,1.5f);
    }
    public static string Run()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(EditorApplication.isPlaying||scene.isDirty||scene.name!="MountainPassScene")
            throw new Exception("Open saved MountainPassScene outside Play Mode.");
        if(GameObject.Find("Technical S bends")!=null)throw new Exception("Technical section already exists; refusing a second deformation.");
        var root=GameObject.Find("Mountain Pass Course").transform;
        var meshes=new HashSet<Mesh>();
        foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
        {
            var m=filter.sharedMesh;
            if(m!=null&&AssetDatabase.GetAssetPath(m).StartsWith(Dir+"/"))meshes.Add(m);
        }
        foreach(var collider in root.GetComponentsInChildren<MeshCollider>())
            if(collider.sharedMesh!=null)meshes.Add(collider.sharedMesh);
        int changed=0;
        foreach(var mesh in meshes)
        {
            var v=mesh.vertices;var normals=mesh.normals;bool touched=false;
            for(int i=0;i<v.Length;i++)
            {
                var next=Warp(v[i]);if((next-v[i]).sqrMagnitude<.00000001f)continue;
                if(normals.Length==v.Length)normals[i]=WarpNormal(v[i],normals[i]);
                v[i]=next;touched=true;
            }
            if(!touched)continue;
            mesh.vertices=v;if(normals.Length==v.Length)mesh.normals=normals;
            mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);changed++;
        }
        // Primitive colliders are absent here; batched scenery was warped with the road.
        foreach(var collider in root.GetComponentsInChildren<MeshCollider>())
        {var m=collider.sharedMesh;collider.sharedMesh=null;collider.sharedMesh=m;}
        var marker=new GameObject("Technical S bends").transform;marker.SetParent(root,false);
        Physics.SyncTransforms();
        var road=GameObject.Find("Mountain Pass asphalt").GetComponent<MeshCollider>().sharedMesh.vertices;
        var route=Enumerable.Range(0,road.Length/2-1).Select(i=>(road[2*i]+road[2*i+1])*.5f).ToArray();
        foreach(var spec in new[]{new {x=190f,text="S BENDS / SLOW",width=12f},new {x=115f,text=">>",width=4f},new {x=48f,text="<<",width=4f},new {x=-20f,text=">>",width=4f}})
        {
            int i=Enumerable.Range(0,route.Length).Where(j=>route[j].z< -300).OrderBy(j=>Mathf.Abs(route[j].x-spec.x)).First();
            Board(marker,spec.text,route[i],(route[(i+1)%route.Length]-route[i]).normalized,spec.width);
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return "Added a 240m technical S-bend zone on the flat lower return; "+changed+" meshes updated together. Existing tunnel, lights and hairpins preserved.";
    }
}
