using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

// Baked meadow dressing, merged into existing spatial batches when possible.
public static class DressLakeTownMeadows
{
    const string Dir="Assets/Environments/LakeTownTrial/MeadowDetails";
    class Geometry { public List<Vector3> v=new List<Vector3>(); public List<int> t=new List<int>(); public List<Vector2> uv=new List<Vector2>(); }
    class Road { public Vector3 a,b;public bool main; }
    static List<Road> roads;
    static Bounds[] houses;
    static Bounds[] posts;
    static Vector3[] trunks;
    static MeshCollider ground;
    static List<Bounds> reserved;
    static Dictionary<Vector2Int,Geometry> groups;
    static System.Random random;
    static int triangles;
    static Vector3 Flat(Vector3 p){p.y=0;return p;}
    static Vector3 Closest(Vector3 p,Vector3 a,Vector3 b){var d=b-a;return a+d*Mathf.Clamp01(Vector3.Dot(p-a,d)/d.sqrMagnitude);}
    static float Range(float min,float max)=>Mathf.Lerp(min,max,(float)random.NextDouble());
    static bool In(Bounds b,Vector3 p,float margin=0)=>p.x>b.min.x-margin&&p.x<b.max.x+margin&&p.z>b.min.z-margin&&p.z<b.max.z+margin;
    static bool Safe(Vector3 p,bool trail=false)
    {
        p=Flat(p);if(!ground.Raycast(new Ray(p+Vector3.up*20,Vector3.down),out var hit,30))return false;
        if(houses.Any(h=>In(h,p,2))||trunks.Any(t=>(t-p).sqrMagnitude<2.25f)||posts.Any(b=>In(b,p,1.25f))||reserved.Any(b=>In(b,p)))return false;
        foreach(var r in roads)
        {
            float radius=r.main?(trail?11.25f:12f):(trail?8.4f:9.5f);
            if(p.x<Mathf.Min(r.a.x,r.b.x)-radius||p.x>Mathf.Max(r.a.x,r.b.x)+radius||p.z<Mathf.Min(r.a.z,r.b.z)-radius||p.z>Mathf.Max(r.a.z,r.b.z)+radius)continue;
            if((p-Closest(p,r.a,r.b)).sqrMagnitude<radius*radius)return false;
        }
        return true;
    }
    static bool ClearArea(Vector3 center,float radius)
    {
        if(!Safe(center))return false;
        for(int ring=1;ring<=2;ring++)for(int i=0;i<16;i++){float a=i*Mathf.PI/8;if(!Safe(center+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*radius*ring*.5f))return false;}
        return true;
    }
    static void Reserve(Vector3 center,float radius)=>reserved.Add(new Bounds(Flat(center),new Vector3(radius*2,1,radius*2)));
    static void Add(Vector3[] v,int[] t,int color)
    {
        var center=v.Aggregate(Vector3.zero,(a,b)=>a+b)/v.Length;var key=new Vector2Int(Mathf.FloorToInt(center.x/64),Mathf.FloorToInt(center.z/64));
        if(!groups.TryGetValue(key,out var g))groups[key]=g=new Geometry();int offset=g.v.Count;g.v.AddRange(v);g.t.AddRange(t.Select(i=>i+offset));g.uv.AddRange(v.Select(p=>new Vector2((color+.5f)/16,.5f)));triangles+=t.Length/3;
    }
    static void Patch(Vector3 c,float rx,float rz,int color)
    {
        var v=new List<Vector3>{c};for(int i=0;i<8;i++){float a=i*Mathf.PI/4;v.Add(c+new Vector3(Mathf.Cos(a)*rx*Range(.8f,1),0,Mathf.Sin(a)*rz*Range(.8f,1)));}
        var t=new List<int>();for(int i=0;i<8;i++)t.AddRange(new[]{0,1+(i+1)%8,1+i});Add(v.ToArray(),t.ToArray(),color);
    }
    static void Bush(Vector3 c,float width,float height,int color)
    {
        // Two six-sided rings and a low crown, rather than transparent leaf cards.
        var v=new List<Vector3>();float yaw=Range(0,Mathf.PI);
        for(int ring=0;ring<2;ring++)for(int i=0;i<6;i++){float a=yaw+i*Mathf.PI/3;float r=width*(ring==0?.5f:.34f);v.Add(c+new Vector3(Mathf.Cos(a)*r,height*(ring==0?.2f:.72f),Mathf.Sin(a)*r*.85f));}
        v.Add(c+Vector3.up*height);var t=new List<int>();
        for(int i=0;i<6;i++){int j=(i+1)%6;t.AddRange(new[]{i,6+i,6+j,i,6+j,j,6+i,12,6+j});}
        Add(v.ToArray(),t.ToArray(),color);
    }
    static void Rock(Vector3 c,float size)
    {
        float yaw=Range(0,Mathf.PI);var lower=new Vector3[6];var upper=new Vector3[6];
        for(int i=0;i<6;i++){float a=yaw+i*Mathf.PI/3;lower[i]=c+new Vector3(Mathf.Cos(a)*size*.5f,0,Mathf.Sin(a)*size*.42f);upper[i]=c+new Vector3(Mathf.Cos(a)*size*.35f,size*Range(.23f,.40f),Mathf.Sin(a)*size*.32f);}
        var top=c+Vector3.up*size*.43f;
        for(int i=0;i<6;i++){int j=(i+1)%6;Add(new[]{lower[i],upper[i],upper[j],lower[j]},new[]{0,1,2,0,2,3},12);Add(new[]{upper[i],top,upper[j]},new[]{0,1,2},12);}
    }
    static void Flower(Vector3 p,int color)
    {
        Bush(p,.38f,.22f,2);var c=p+Vector3.up*.25f;float r=.13f;
        Add(new[]{c+Vector3.left*r,c+Vector3.forward*r,c+Vector3.right*r,c+Vector3.back*r},new[]{0,1,2,0,2,3},color);
    }
    static void Trail(Vector3[] points)
    {
        for(int i=1;i<points.Length;i++)
        {
            var a=points[i-1];var b=points[i];var side=Vector3.Cross(Vector3.up,(b-a).normalized)*.55f;a.y=b.y=-.012f;
            Add(new[]{a-side,b-side,b+side,a+side},new[]{0,1,2,0,2,3},13);
            var bound=new Bounds((a+b)*.5f,Vector3.zero);bound.Encapsulate(a);bound.Encapsulate(b);bound.Expand(new Vector3(2,1,2));reserved.Add(bound);
        }
    }
    static bool MakeTrail(Road road,int side)
    {
        var along=(road.b-road.a).normalized;float length=Vector3.Distance(road.a,road.b);if(length<32)return false;
        var outward=Vector3.Cross(Vector3.up,along)*side;float offset=road.main?11.7f:9.2f;
        var center=Vector3.Lerp(road.a,road.b,Range(.24f,.76f));float half=Range(10,17);float bulge=Range(6,9);var points=new Vector3[21];
        for(int i=0;i<points.Length;i++)
        {
            float t=i/(float)(points.Length-1);points[i]=center+along*Mathf.Lerp(-half,half,t)+outward*(offset+Mathf.Sin(t*Mathf.PI)*bulge);
        }
        // Sample each segment and its full width; never cross a road, garden or entrance path.
        for(int i=1;i<points.Length;i++)
        {
            var direction=points[i]-points[i-1];var edge=Vector3.Cross(Vector3.up,direction.normalized)*.65f;
            for(int sample=0;sample<=3;sample++)foreach(int s in new[]{-1,0,1})if(!Safe(Vector3.Lerp(points[i-1],points[i],sample/3f)+edge*s,true))return false;
        }
        Trail(points);return true;
    }
    public static string Run()
    {
        if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean Edit Mode required");var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/LakeTownScene.unity")throw new Exception("Open Lake Town first");
        var town=Object.FindAnyObjectByType<LakeTownNightLights>().transform;if(town.Find("Meadow field details")!=null||AssetDatabase.IsValidFolder(Dir))throw new Exception("Meadow details already exist");
        var residential=town.Find("Residential garden details");if(residential==null)throw new Exception("Residential details required");
        var originalMeshes=residential.GetComponentsInChildren<MeshFilter>();reserved=new List<Bounds>();groups=new Dictionary<Vector2Int,Geometry>();random=new System.Random(23804);triangles=0;
        foreach(var filter in originalMeshes)
        {
            var mesh=filter.sharedMesh;var v=mesh.vertices;var uv=mesh.uv;var t=mesh.triangles;
            if(uv.Any(p=>p.x>=.75f))throw new Exception("Reserved palette slots are already in use");
            for(int i=0;i<t.Length;i+=3)
            {
                int color=Mathf.FloorToInt(uv[t[i]].x*16);if(color==6||color==7||color==11)continue;
                var bounds=new Bounds(filter.transform.TransformPoint(v[t[i]]),Vector3.zero);bounds.Encapsulate(filter.transform.TransformPoint(v[t[i+1]]));bounds.Encapsulate(filter.transform.TransformPoint(v[t[i+2]]));bounds.Expand(new Vector3(.9f,1,.9f));reserved.Add(bounds);
            }
        }
        houses=town.GetComponentsInChildren<BoxCollider>().Where(c=>c.name.StartsWith("Town house ")).Select(c=>c.bounds).ToArray();trunks=town.GetComponentsInChildren<Transform>().Where(t=>t.name=="Pine trunk").Select(t=>Flat(t.position)).ToArray();ground=town.GetComponentsInChildren<MeshCollider>().Single(c=>c.name=="Town ground");
        posts=town.GetComponentsInChildren<BoxCollider>().Where(c=>c.name.EndsWith(" post")&&c.name!="Lakeside railing post").Select(c=>c.bounds).ToArray();
        roads=new List<Road>{new Road{a=new Vector3(-90,0,-145),b=new Vector3(-90,0,91)},new Road{a=new Vector3(85,0,-88),b=new Vector3(85,0,120)},new Road{a=new Vector3(-290,0,-5),b=new Vector3(294,0,-5)}};
        var main=town.GetComponentsInChildren<MeshCollider>().Single(c=>c.name=="Lake Town Road");var rv=main.sharedMesh.vertices;var loop=Enumerable.Range(0,rv.Length/2).Select(i=>Flat(main.transform.TransformPoint((rv[i*2]+rv[i*2+1])*.5f))).ToArray();for(int i=1;i<loop.Length;i++)roads.Add(new Road{a=loop[i-1],b=loop[i],main=true});
        int trails=0;
        // Longer street-edge segments allow connected walking loops, with no ends stranded in the lawn.
        foreach(var road in roads.Take(3))foreach(int side in new[]{-1,1})for(int attempt=0;attempt<65&&trails<6;attempt++)if(MakeTrail(road,side)){trails++;break;}
        int clusters=0,rocks=0,flowers=0;
        for(int attempt=0;attempt<1800&&clusters<50;attempt++)
        {
            var p=new Vector3(Range(-345,345),-.04f,Range(-205,165));if(!ClearArea(p,4))continue;
            Patch(new Vector3(p.x,-.018f,p.z),3.2f,2.8f,14);
            int count=3+random.Next(2);for(int i=0;i<count;i++){float a=i*Mathf.PI*.65f;var c=p+new Vector3(Mathf.Cos(a)*Range(.4f,1.8f),0,Mathf.Sin(a)*Range(.4f,1.6f));Bush(c,Range(1.4f,2.3f),Range(.65f,1.1f),i%2==0?1:2);}
            Reserve(p,4.5f);clusters++;
        }
        for(int attempt=0;attempt<1000&&rocks<20;attempt++)
        {
            var p=new Vector3(Range(-340,340),-.035f,Range(-200,160));if(!ClearArea(p,2.7f))continue;
            Patch(new Vector3(p.x,-.017f,p.z),1.8f,1.4f,14);for(int i=0;i<3;i++)Rock(p+new Vector3(Range(-.8f,.8f),0,Range(-.6f,.6f)),Range(.7f,1.25f));Reserve(p,3);rocks++;
        }
        for(int attempt=0;attempt<900&&flowers<18;attempt++)
        {
            var p=new Vector3(Range(-280,280),-.025f,Range(-165,145));if(!ClearArea(p,2))continue;
            for(int i=0;i<5;i++){float a=Range(0,Mathf.PI*2);float radius=Range(.25f,1.5f);Flower(p+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius),i%3==0?5:i%3==1?4:8);}
            Reserve(p,2.2f);flowers++;
        }
        if(clusters<40||trails<2||triangles>8000)throw new Exception("Meadow density/budget needs review: "+clusters+" clusters, "+trails+" trails, "+triangles+" triangles");
        AssetDatabase.CreateFolder("Assets/Environments/LakeTownTrial","MeadowDetails");var root=new GameObject("Meadow field details").transform;root.SetParent(town,false);
        var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Environments/LakeTownTrial/ResidentialDetails/Residential details shared.mat");var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Environments/LakeTownTrial/ResidentialDetails/Residential palette.asset");
        foreach(var pair in new[]{(12,new Color(.41f,.425f,.38f)),(13,new Color(.46f,.405f,.30f)),(14,new Color(.325f,.45f,.245f)),(15,new Color(.7f,.61f,.34f))})texture.SetPixel(pair.Item1,0,pair.Item2.linear);texture.Apply();EditorUtility.SetDirty(texture);
        bool first=true;int reused=0,added=0;
        foreach(var item in groups)
        {
            var g=item.Value;var mesh=new Mesh{name="Meadow tile "+item.Key};mesh.SetVertices(g.v);mesh.SetTriangles(g.t,0);mesh.SetUVs(0,g.uv);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var existing=residential.Find("Residential tile "+item.Key);GameObject go;
            if(existing!=null)
            {
                var original=existing.GetComponent<MeshFilter>().sharedMesh;var combined=new Mesh{name=original.name+" with meadow"};combined.CombineMeshes(new[]{new CombineInstance{mesh=original,transform=Matrix4x4.identity},new CombineInstance{mesh=mesh,transform=Matrix4x4.identity}});
                if(!combined.vertices.Take(original.vertexCount).SequenceEqual(original.vertices))throw new Exception("Original residential vertices altered");Object.DestroyImmediate(mesh);mesh=combined;go=existing.gameObject;reused++;
            }
            else{go=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);added++;}
            const string path=Dir+"/Meadow geometry.asset";if(first){AssetDatabase.CreateAsset(mesh,path);first=false;}else AssetDatabase.AddObjectToAsset(mesh,path);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
        }
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return clusters+" shrub groups, "+rocks+" small rock groups, "+flowers+" wildflower patches and "+trails+" connected worn footpaths; "+triangles+" new triangles, "+reused+" existing batches reused, only "+added+" additional renderers. Shared original material; no lights/colliders/runtime scripts.";
    }
}
