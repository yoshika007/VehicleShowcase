using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

// Editor-authored decoration: one palette material, spatial mesh batches, no runtime component.
public static class DressLakeTownResidential
{
    const string Dir="Assets/Environments/LakeTownTrial/ResidentialDetails";
    const float TileSize=64f;
    class Geometry { public List<Vector3> v=new List<Vector3>();public List<int> t=new List<int>();public List<Vector2> uv=new List<Vector2>(); }
    class Road { public Vector3 a,b;public bool main; }
    class PathPlan { public Vector3[] points;public float endHeight; }
    static Dictionary<Vector2Int,Geometry> groups;
    static List<Road> roads;
    static Bounds[] houses;
    static Vector3[] trunks;
    static MeshCollider ground;
    static List<Bounds> reservations;
    static bool created;
    static int triangles;
    static Vector3 Flat(Vector3 p){p.y=0;return p;}
    static Vector3 Closest(Vector3 p,Vector3 a,Vector3 b){var d=b-a;return a+d*Mathf.Clamp01(Vector3.Dot(p-a,d)/d.sqrMagnitude);}
    static float DistanceTo(Bounds b,Vector3 p)
    {float x=Mathf.Max(b.min.x-p.x,0,p.x-b.max.x),z=Mathf.Max(b.min.z-p.z,0,p.z-b.max.z);return Mathf.Sqrt(x*x+z*z);}
    static bool OnLand(Vector3 p){p.y=20;return ground.Raycast(new Ray(p,Vector3.down),out var hit,30);}
    static bool ClearRoad(Vector3 p,bool path)
    {
        foreach(var r in roads)
        {
            float radius=path?(r.main?7.3f:6.3f):(r.main?11.8f:9.2f);
            if(p.x<Mathf.Min(r.a.x,r.b.x)-radius||p.x>Mathf.Max(r.a.x,r.b.x)+radius||p.z<Mathf.Min(r.a.z,r.b.z)-radius||p.z>Mathf.Max(r.a.z,r.b.z)+radius)continue;
            if((p-Closest(p,r.a,r.b)).sqrMagnitude<=radius*radius)return false;
        }
        return true;
    }
    static bool ClearPoint(Vector3 p,bool path,int own=-1)
    {
        p=Flat(p);if(!OnLand(p)||!ClearRoad(p,path))return false;
        for(int i=0;i<houses.Length;i++)if(DistanceTo(houses[i],p)<(i==own?.03f:.6f))return false;
        return trunks.All(t=>Vector3.Distance(p,t)>1f);
    }
    static Bounds Footprint(Vector3 a,Vector3 b,float width)
    {
        var bounds=new Bounds((a+b)*.5f,Vector3.zero);bounds.Encapsulate(a);bounds.Encapsulate(b);bounds.Expand(new Vector3(width,1,width));return bounds;
    }
    static bool ClearFoot(Vector3 center,Vector3 size)
    {
        var bounds=new Bounds(Flat(center),new Vector3(size.x,0,size.z));
        if(reservations.Any(b=>b.min.x<bounds.max.x&&b.max.x>bounds.min.x&&b.min.z<bounds.max.z&&b.max.z>bounds.min.z))return false;
        for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)if(!ClearPoint(center+new Vector3(size.x*x*.5f,0,size.z*z*.5f),false))return false;
        return true;
    }
    static bool ClearSegment(Vector3 a,Vector3 b,int own)
    {
        var direction=Flat(b-a);float length=direction.magnitude;if(length<.01f)return true;
        var side=Vector3.Cross(Vector3.up,direction.normalized)*.82f;
        int steps=Mathf.CeilToInt(length/.7f);
        for(int i=0;i<=steps;i++)foreach(float offset in new[]{-1f,0f,1f})if(!ClearPoint(Vector3.Lerp(a,b,i/(float)steps)+side*offset,true,own))return false;
        return true;
    }
    static PathPlan Plan(int index)
    {
        var h=houses[index];var start=new Vector3(h.center.x,0,h.min.z-.32f);var front=new Vector3(h.center.x,0,h.min.z-3.2f);
        var candidates=roads.Select(r=>{var nearest=Closest(front,r.a,r.b);var away=(front-nearest).normalized;return new{point=nearest+away*(r.main?11.1f:8.6f),height=r.main?.035f:.16f};}).OrderBy(c=>(c.point-front).sqrMagnitude).ToArray();
        PathPlan best=null;float bestLength=float.MaxValue;
        foreach(var candidate in candidates.Take(160))
        {
            var target=candidate.point;
            if(Vector3.Distance(start,front)+Vector3.Distance(front,target)>=bestLength)break;
            var options=new[]{new[]{start,front,target},new[]{start,front,new Vector3(target.x,0,front.z),target},new[]{start,front,new Vector3(front.x,0,target.z),target},
                new[]{start,front,new Vector3(h.min.x-2.5f,0,front.z),new Vector3(h.min.x-2.5f,0,target.z),target},
                new[]{start,front,new Vector3(h.max.x+2.5f,0,front.z),new Vector3(h.max.x+2.5f,0,target.z),target}};
            foreach(var option in options)
            {
                float length=0;bool safe=true;
                for(int i=1;i<option.Length;i++){length+=Vector3.Distance(option[i-1],option[i]);if(length>=bestLength||!ClearSegment(option[i-1],option[i],index)){safe=false;break;}}
                if(safe&&length<90){bestLength=length;best=new PathPlan{points=option,endHeight=candidate.height};}
            }
        }
        return best;
    }
    static void Add(Vector3[] v,int[] t,int color)
    {
        var center=v.Aggregate(Vector3.zero,(a,b)=>a+b)/v.Length;var key=new Vector2Int(Mathf.FloorToInt(center.x/TileSize),Mathf.FloorToInt(center.z/TileSize));
        if(!groups.TryGetValue(key,out var g))groups[key]=g=new Geometry();int first=g.v.Count;g.v.AddRange(v);g.t.AddRange(t.Select(i=>i+first));g.uv.AddRange(v.Select(p=>new Vector2((color+.5f)/16f,.5f)));triangles+=t.Length/3;
    }
    static void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,int color)=>Add(new[]{a,b,c,d},new[]{0,1,2,0,2,3},color);
    static void FlatBox(Vector3 center,Vector3 size,int color)
    {
        var a=new Vector3(size.x*.5f,0,0);var b=new Vector3(0,0,size.z*.5f);Quad(center-a-b,center-a+b,center+a+b,center+a-b,color);
    }
    static void Box(Vector3 c,Vector3 size,int color)
    {
        var a=Vector3.right*size.x*.5f;var b=Vector3.forward*size.z*.5f;var y=Vector3.up*size.y*.5f;
        Quad(c-a-b+y,c-a+b+y,c+a+b+y,c+a-b+y,color);
        Quad(c-a-b-y,c-a-b+y,c+a-b+y,c+a-b-y,color);
        Quad(c+a-b-y,c+a-b+y,c+a+b+y,c+a+b-y,color);
        Quad(c+a+b-y,c+a+b+y,c-a+b+y,c-a+b-y,color);
        Quad(c-a+b-y,c-a+b+y,c-a-b+y,c-a-b-y,color);
    }
    static void Shrub(Vector3 center,Vector3 size,int color)
    {
        var top=center+Vector3.up*size.y;var ring=new Vector3[6];
        for(int i=0;i<6;i++){float a=i*Mathf.PI/3;ring[i]=center+new Vector3(Mathf.Cos(a)*size.x*.5f,size.y*.35f,Mathf.Sin(a)*size.z*.5f);}
        for(int i=0;i<6;i++)Add(new[]{ring[i],top,ring[(i+1)%6]},new[]{0,1,2},color);
    }
    static void Entrance(PathPlan plan)
    {
        var end=plan.points.Last();
        for(int s=1;s<plan.points.Length;s++)
        {
            var a=plan.points[s-1];var b=plan.points[s];var delta=b-a;float length=delta.magnitude;if(length<.05f)continue;var side=Vector3.Cross(Vector3.up,delta.normalized)*.8f;
            int steps=Mathf.CeilToInt(length/2.5f);
            for(int i=0;i<steps;i++)
            {
                var from=Vector3.Lerp(a,b,i/(float)steps);var to=Vector3.Lerp(a,b,(i+1f)/steps);
                from.y=Mathf.Lerp(plan.endHeight,.065f,Mathf.Clamp01(Vector3.Distance(Flat(from),end)/2f));to.y=Mathf.Lerp(plan.endHeight,.065f,Mathf.Clamp01(Vector3.Distance(Flat(to),end)/2f));
                Quad(from-side,to-side,to+side,from+side,0);
                if(i>0){var joint=from+Vector3.up*.003f;var half=delta.normalized*.012f;Quad(joint-side-half,joint-side+half,joint+side+half,joint+side-half,9);}
            }
            reservations.Add(Footprint(a,b,1.9f));
        }
    }
    public static string Run()
    {
        if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean Edit Mode required");var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/LakeTownScene.unity")throw new Exception("Open Lake Town first");
        var town=Object.FindAnyObjectByType<LakeTownNightLights>().transform;if(town.Find("Residential garden details")!=null||AssetDatabase.IsValidFolder(Dir))throw new Exception("Residential details already exist");
        ground=town.GetComponentsInChildren<MeshCollider>().Single(c=>c.name=="Town ground");houses=town.GetComponentsInChildren<BoxCollider>().Where(c=>c.name.StartsWith("Town house ")).OrderBy(c=>int.Parse(c.name.Substring(11))).Select(c=>c.bounds).ToArray();
        trunks=town.GetComponentsInChildren<Transform>().Where(t=>t.name=="Pine trunk").Select(t=>Flat(t.position)).ToArray();
        roads=new List<Road>();var source=town.GetComponentsInChildren<MeshCollider>().Single(c=>c.name=="Lake Town Road");var points=source.sharedMesh.vertices;
        var loop=Enumerable.Range(0,points.Length/2).Select(i=>Flat(source.transform.TransformPoint((points[i*2]+points[i*2+1])*.5f))).ToArray();for(int i=1;i<loop.Length;i++)roads.Add(new Road{a=loop[i-1],b=loop[i],main=true});
        roads.Add(new Road{a=new Vector3(-90,0,-145),b=new Vector3(-90,0,91)});roads.Add(new Road{a=new Vector3(85,0,-88),b=new Vector3(85,0,120)});roads.Add(new Road{a=new Vector3(-290,0,-5),b=new Vector3(294,0,-5)});
        groups=new Dictionary<Vector2Int,Geometry>();reservations=new List<Bounds>();triangles=0;created=false;
        // Plan all routes first, so gardens cannot cover any entrance path.
        var plans=new PathPlan[houses.Length];for(int i=0;i<houses.Length;i++){plans[i]=Plan(i);if(plans[i]==null)throw new Exception("No clear sidewalk route for house "+i);}
        AssetDatabase.CreateFolder("Assets/Environments/LakeTownTrial","ResidentialDetails");
        foreach(var plan in plans)Entrance(plan);
        int beds=0,hedges=0;var random=new System.Random(70423);
        for(int i=0;i<houses.Length;i++)
        {
            var h=houses[i];
            foreach(int side in new[]{-1,1})
            {
                var p=new Vector3(h.center.x+side*h.size.x*.28f,.085f,h.min.z-1.65f);var size=new Vector3(2.6f,0,1.6f);
                if(ClearFoot(p,size))
                {
                    FlatBox(p,size,3);Box(p+new Vector3(0,.06f,-.76f),new Vector3(2.7f,.12f,.12f),10);Box(p+new Vector3(0,.06f,.76f),new Vector3(2.7f,.12f,.12f),10);
                    for(int j=0;j<3;j++){var bush=p+new Vector3(-.85f+j*.85f,.03f,0);Shrub(bush,new Vector3(.8f,.45f,.85f),i%2==0?1:2);Shrub(bush+new Vector3(.1f,.42f,-.05f),new Vector3(.3f,.17f,.3f),i%3==0?5:4);}
                    reservations.Add(new Bounds(Flat(p),new Vector3(3,1,2)));beds++;
                }
                if((i+side)%3==0)continue;
                var hedge=new Vector3(h.center.x+side*(h.extents.x+2),.35f,h.min.z+2.8f);var footprint=new Vector3(.9f,0,3.4f);
                if(ClearFoot(hedge,footprint)){for(int j=0;j<3;j++)Shrub(hedge+new Vector3(0,-.30f,-1.05f+j*1.05f),new Vector3(.9f,.65f,1.25f),i%2==0?1:2);reservations.Add(new Bounds(Flat(hedge),new Vector3(1.2f,1,3.8f)));hedges++;}
            }
        }
        int patches=0;
        for(int attempt=0;attempt<3500&&patches<100;attempt++)
        {
            var c=new Vector3(-320+(float)random.NextDouble()*640,-.026f,-180+(float)random.NextDouble()*330);float rx=3+(float)random.NextDouble()*6,rz=3+(float)random.NextDouble()*6;
            if(!ClearFoot(c,new Vector3(rx*2,0,rz*2)))continue;
            var vertices=new List<Vector3>{c};for(int k=0;k<8;k++){float a=k*Mathf.PI/4;float r=.78f+(float)random.NextDouble()*.22f;vertices.Add(c+new Vector3(Mathf.Cos(a)*rx*r,0,Mathf.Sin(a)*rz*r));}
            var t=new List<int>();for(int k=0;k<8;k++)t.AddRange(new[]{0,1+(k+1)%8,1+k});Add(vertices.ToArray(),t.ToArray(),patches%3==0?6:patches%3==1?7:11);reservations.Add(new Bounds(Flat(c),new Vector3(rx*1.7f,1,rz*1.7f)));patches++;
        }
        var colors=new[]{new Color(.58f,.55f,.46f),new Color(.16f,.29f,.13f),new Color(.23f,.37f,.16f),new Color(.24f,.17f,.10f),new Color(.94f,.69f,.22f),new Color(.69f,.32f,.44f),new Color(.33f,.46f,.25f),new Color(.31f,.435f,.24f),new Color(.88f,.82f,.64f),new Color(.45f,.43f,.36f),new Color(.31f,.25f,.16f),new Color(.38f,.49f,.26f)};
        var texture=new Texture2D(16,1,TextureFormat.RGB24,false,true){name="Residential palette",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};for(int i=0;i<16;i++)texture.SetPixel(i,0,colors[Mathf.Min(i,colors.Length-1)].linear);texture.Apply();AssetDatabase.CreateAsset(texture,Dir+"/Residential palette.asset");
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Residential details shared"};material.color=Color.white;material.SetTexture("_BaseMap",texture);material.SetFloat("_Smoothness",.1f);AssetDatabase.CreateAsset(material,Dir+"/Residential details shared.mat");
        var root=new GameObject("Residential garden details").transform;root.SetParent(town,false);
        foreach(var item in groups)
        {
            var g=item.Value;var mesh=new Mesh{name="Residential tile "+item.Key};mesh.SetVertices(g.v);mesh.SetTriangles(g.t,0);mesh.SetUVs(0,g.uv);mesh.RecalculateNormals();mesh.RecalculateBounds();string asset=Dir+"/Residential geometry.asset";if(!created){AssetDatabase.CreateAsset(mesh,asset);created=true;}else AssetDatabase.AddObjectToAsset(mesh,asset);
            var go=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
        }
        if(triangles>15000||groups.Count>100)throw new Exception("Residential mobile budget exceeded");
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        System.IO.File.WriteAllText("/private/tmp/ResidentialRoutes.txt",string.Join("\n",plans.SelectMany((plan,i)=>plan.points.Select(p=>i+" "+p))));
        return houses.Length+" sidewalk-connected entrance paths, "+beds+" flower beds, "+hedges+" low hedge clusters and "+patches+" subtle grass patches; "+triangles+" triangles in "+groups.Count+" local meshes using ONE material. No lights, colliders or runtime scripts added.";
    }
}
