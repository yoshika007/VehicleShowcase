using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

// Adds only infield street visuals. Original road colliders and main-loop geometry are retained.
public static class DressLakeTownInfield
{
    const string Dir="Assets/Environments/LakeTownTrial/InfieldStreets";
    class Geometry { public List<Vector3> vertices=new List<Vector3>(); public List<int> triangles=new List<int>(); }
    static Dictionary<(Material,int,int),Geometry> groups;
    static Vector3[] loop;
    static Transform root;
    static Material ivory,yellow,paving,curb,gutter,seam;
    static bool meshAssetCreated;
    static float LoopDistance(Vector3 point)
    {
        float best=float.MaxValue;point.y=0;
        for(int i=0;i<loop.Length;i++)
        {
            var a=loop[i];var d=loop[(i+1)%loop.Length]-a;
            var closest=a+d*Mathf.Clamp01(Vector3.Dot(point-a,d)/d.sqrMagnitude);
            best=Mathf.Min(best,(closest-point).sqrMagnitude);
        }
        return Mathf.Sqrt(best);
    }
    static bool Inside(Vector3 p)
    {
        bool inside=false;
        for(int i=0,j=loop.Length-1;i<loop.Length;j=i++)
            if((loop[i].z>p.z)!=(loop[j].z>p.z) && p.x<(loop[j].x-loop[i].x)*(p.z-loop[i].z)/(loop[j].z-loop[i].z)+loop[i].x)inside=!inside;
        return inside;
    }
    static bool ClearOfMain(Vector3 p)=>Inside(p)&&LoopDistance(p)>12f;
    static Material Mat(string name,Color color)
    {
        var mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};mat.color=color;mat.SetFloat("_Smoothness",.12f);AssetDatabase.CreateAsset(mat,Dir+"/"+name+".mat");return mat;
    }
    static void Add(Material material,Vector3[] vertices,int[] triangles)
    {
        var center=vertices.Aggregate(Vector3.zero,(a,b)=>a+b)/vertices.Length;
        var key=(material,Mathf.FloorToInt(center.x/25),Mathf.FloorToInt(center.z/25));
        if(!groups.TryGetValue(key,out var geometry))groups[key]=geometry=new Geometry();
        int offset=geometry.vertices.Count;geometry.vertices.AddRange(vertices);geometry.triangles.AddRange(triangles.Select(t=>t+offset));
    }
    static void Patch(Material mat,Vector3 center,float width,float length,bool vertical,float height)
    {
        var along=vertical?Vector3.forward:Vector3.right;var across=vertical?Vector3.right:Vector3.forward;
        center.y=height;var a=across*width*.5f;var b=along*length*.5f;
        var vertices=new[]{center-a-b,center-a+b,center+a+b,center+a-b};
        if(vertices.Any(p=>!ClearOfMain(p)))return;
        // XZ winding differs with street orientation.
        Add(mat,vertices,vertical?new[]{0,1,2,0,2,3}:new[]{0,2,1,0,3,2});
    }
    static void Curb(Vector3 center,bool vertical,float height)
    {
        float width=.24f,length=1f;var along=vertical?Vector3.forward:Vector3.right;var across=vertical?Vector3.right:Vector3.forward;
        var a=across*width*.5f;var b=along*length*.5f;center.y=.04f;
        var v=new[]{center-a-b,center-a+b,center+a+b,center+a-b,center-a-b+Vector3.up*height,center-a+b+Vector3.up*height,center+a+b+Vector3.up*height,center+a-b+Vector3.up*height};
        if(v.Any(p=>!ClearOfMain(p)))return;
        var t=new[]{4,5,6,4,6,7,0,4,7,0,7,3,1,2,6,1,6,5,0,1,5,0,5,4,3,7,6,3,6,2};
        if(!vertical)for(int i=0;i<t.Length;i+=3){int swap=t[i+1];t[i+1]=t[i+2];t[i+2]=swap;}
        Add(curb,v,t);
    }
    static float Along(Vector3 p,bool vertical)=>vertical?p.z:p.x;
    static Vector3 Point(float fixedAxis,float distance,bool vertical)=>vertical?new Vector3(fixedAxis,0,distance):new Vector3(distance,0,fixedAxis);
    static float[] Junctions(bool vertical)=>vertical?new[]{-5f}:new[]{-90f,85f};
    static void Street(float fixedAxis,float start,float end,bool vertical)
    {
        var across=vertical?Vector3.right:Vector3.forward;var junctions=Junctions(vertical);
        for(float d=start+.5f;d<end;d+=1f)
        {
            var p=Point(fixedAxis,d,vertical);
            float junctionDistance=junctions.Min(j=>Mathf.Abs(d-j));
            if(junctionDistance>16 && Mathf.Repeat(d-start,8)<3)Patch(yellow,p,.16f,1.02f,vertical,.064f);
            if(junctionDistance<6.7f)continue;
            foreach(int side in new[]{-1,1})
            {
                Patch(ivory,p+across*(side*5.55f),.12f,1.02f,vertical,.066f);
                Patch(gutter,p+across*(side*5.88f),.22f,1.02f,vertical,.056f);
                // Lower the curb near crossings to suggest a pedestrian ramp.
                float crossingDistance=junctions.SelectMany(j=>new[]{Mathf.Abs(d-j-10),Mathf.Abs(d-j+10)}).Min();
                float height=Mathf.Lerp(.025f,.10f,Mathf.Clamp01((crossingDistance-.8f)/2));
                Curb(p+across*(side*6.12f),vertical,height);
                Patch(paving,p+across*(side*7.55f),2.62f,1.01f,vertical,.04f+height);
                if(((int)(d-start))%3==0)Patch(seam,p+across*(side*7.55f),2.6f,.025f,vertical,.044f+height);
            }
        }
    }
    static void Crossing(Vector3 center,bool vertical)
    {
        var across=vertical?Vector3.right:Vector3.forward;
        for(float offset=-4.8f;offset<5;offset+=1.2f)Patch(ivory,center+across*offset,.58f,3.2f,vertical,.069f);
    }
    static Mesh Save(Mesh mesh)
    {
        string path=Dir+"/Infield Detail Meshes.asset";
        if(!meshAssetCreated){AssetDatabase.CreateAsset(mesh,path);meshAssetCreated=true;}else AssetDatabase.AddObjectToAsset(mesh,path);
        return mesh;
    }
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit Mode required");
        var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/LakeTownScene.unity")throw new Exception("Open Lake Town first");
        var town=Object.FindAnyObjectByType<LakeTownNightLights>().transform;
        if(town.Find("Infield street details")!=null||AssetDatabase.IsValidFolder(Dir))throw new Exception("Infield already dressed");
        var road=GameObject.Find("Lake Town Road");var roadVertices=road.GetComponent<MeshCollider>().sharedMesh.vertices;
        loop=Enumerable.Range(0,roadVertices.Length/2-1).Select(i=>road.transform.TransformPoint((roadVertices[i*2]+roadVertices[i*2+1])*.5f)).ToArray();for(int i=0;i<loop.Length;i++)loop[i].y=0;
        AssetDatabase.CreateFolder("Assets/Environments/LakeTownTrial","InfieldStreets");groups=new Dictionary<(Material,int,int),Geometry>();meshAssetCreated=false;
        root=new GameObject("Infield street details").transform;root.SetParent(town,false);
        ivory=AssetDatabase.LoadAssetAtPath<Material>("Assets/Environments/LakeTownTrial/Road Ivory.mat");yellow=AssetDatabase.LoadAssetAtPath<Material>("Assets/Environments/LakeTownTrial/Warm Yellow.mat");
        paving=Mat("Town sidewalk",new Color(.58f,.59f,.55f));curb=Mat("Town curb",new Color(.69f,.69f,.64f));gutter=Mat("Town gutter",new Color(.25f,.28f,.28f));seam=Mat("Pavement seams",new Color(.39f,.41f,.39f));
        var texture=new Texture2D(128,128,TextureFormat.RGB24,true){name="Fine asphalt grain",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=2};var pixels=new Color[128*128];var random=new System.Random(2341);
        for(int i=0;i<pixels.Length;i++){float grain=(float)random.NextDouble()*.035f;pixels[i]=new Color(.105f+grain,.12f+grain,.13f+grain);}
        texture.SetPixels(pixels);texture.Apply();AssetDatabase.CreateAsset(texture,Dir+"/Fine asphalt grain.asset");
        var asphalt=Mat("Town street asphalt",Color.white);asphalt.SetTexture("_BaseMap",texture);EditorUtility.SetDirty(asphalt);
        // Only replace the infield render meshes; original physics meshes and the main loop stay intact.
        var originalSections=town.Find("Local road lighting sections").GetComponentsInChildren<MeshFilter>().Where(f=>new[]{"Market Street (","Marina Avenue (","Old Town Cross Street ("}.Any(prefix=>f.name.StartsWith(prefix))).ToArray();
        foreach(var filter in originalSections)
        {
            var mesh=Object.Instantiate(filter.sharedMesh);mesh.name=filter.sharedMesh.name+" textured";
            var vertices=mesh.vertices;var sourceTriangles=mesh.triangles;
            mesh.uv=vertices.Select(v=>new Vector2(v.x,v.z)*.5f).ToArray();
            var inner=new List<int>();var joining=new List<int>();
            for(int i=0;i<sourceTriangles.Length;i+=3)
            {
                bool inside=Enumerable.Range(0,3).All(k=>ClearOfMain(filter.transform.TransformPoint(vertices[sourceTriangles[i+k]])));
                (inside?inner:joining).AddRange(new[]{sourceTriangles[i],sourceTriangles[i+1],sourceTriangles[i+2]});
            }
            mesh.subMeshCount=2;mesh.SetTriangles(inner,0);mesh.SetTriangles(joining,1);filter.sharedMesh=Save(mesh);
            filter.GetComponent<Renderer>().sharedMaterials=new[]{asphalt,AssetDatabase.LoadAssetAtPath<Material>("Assets/Environments/LakeTownTrial/Lake Town Asphalt.mat")};
        }
        Street(-90,-145,91,true);Street(85,-88,120,true);Street(-5,-290,294,false);
        foreach(float x in new[]{-90f,85f})
        {
            foreach(float sign in new[]{-1f,1f})
            {
                Crossing(new Vector3(x,0,-5+sign*10),true);Crossing(new Vector3(x+sign*10,0,-5),false);
                Patch(ivory,new Vector3(x+sign*2.9f,0,-5+sign*13),5.2f,.35f,true,.07f);
            }
        }
        int index=0;
        foreach(var pair in groups)
        {
            var geometry=pair.Value;var mesh=new Mesh{name=pair.Key.Item1.name+" "+pair.Key.Item2+","+pair.Key.Item3};mesh.SetVertices(geometry.vertices);mesh.SetTriangles(geometry.triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();Save(mesh);
            var go=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=pair.Key.Item1;renderer.shadowCastingMode=ShadowCastingMode.Off;index++;
        }
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return "Dressed three infield streets: "+originalSections.Length+" textured road sections, "+index+" local detail batches, 8 crosswalks and stop lines. No colliders added or changed; main circuit excluded by 12 m boundary.";
    }
}
