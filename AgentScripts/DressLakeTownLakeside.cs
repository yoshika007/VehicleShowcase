using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

// Editor-baked waterfront furniture and opaque shoreline planting.
public static class DressLakeTownLakeside
{
    const string Dir="Assets/Environments/LakeTownTrial/LakesideDetails";
    class Geometry { public List<Vector3> v=new List<Vector3>();public List<int> t=new List<int>();public List<Vector2> uv=new List<Vector2>(); }
    static Dictionary<Vector2Int,Geometry> groups;
    static MeshCollider ground;
    static MeshCollider[] roads;
    static List<Bounds> reserved;
    static Vector3[] cube;
    static int[] cubeTriangles;
    static Vector3[] centerline;
    static bool Inside(Bounds b,Vector3 p,float margin=0)=>p.x>b.min.x-margin&&p.x<b.max.x+margin&&p.z>b.min.z-margin&&p.z<b.max.z+margin;
    static float SegmentDistance(Vector3 p,Vector3 a,Vector3 b){p.y=a.y=b.y=0;var d=b-a;return Vector3.Distance(p,a+d*Mathf.Clamp01(Vector3.Dot(p-a,d)/d.sqrMagnitude));}
    static bool Safe(Vector3 p)
    {
        if(!ground.Raycast(new Ray(new Vector3(p.x,20,p.z),Vector3.down),out var hit,30))return false;
        if(reserved.Any(b=>Inside(b,p,.25f)))return false;
        for(int i=1;i<centerline.Length;i++)if(SegmentDistance(p,centerline[i-1],centerline[i])<12.3f)return false;
        if(Mathf.Abs(p.x+90)<9.3f&&p.z<100||Mathf.Abs(p.x-85)<9.3f&&p.z<122)return false;
        return true;
    }
    static bool Clear(Vector3 p,float x,float z)
    {for(float a=-x*.5f;a<=x*.5f+.01f;a+=.4f)for(float b=-z*.5f;b<=z*.5f+.01f;b+=.4f)if(!Safe(p+new Vector3(a,0,b)))return false;return true;}
    static void Add(Vector3[] v,int[] t,int color)
    {
        var c=v.Aggregate(Vector3.zero,(a,b)=>a+b)/v.Length;var key=new Vector2Int(Mathf.FloorToInt(c.x/64),Mathf.FloorToInt(c.z/64));
        if(!groups.TryGetValue(key,out var g))groups[key]=g=new Geometry();int start=g.v.Count;g.v.AddRange(v);g.t.AddRange(t.Select(i=>i+start));g.uv.AddRange(v.Select(p=>new Vector2((color+.5f)/16,.5f)));
    }
    static void Box(Vector3 p,Vector3 size,int color)=>Add(cube.Select(v=>p+Vector3.Scale(v,size)).ToArray(),cubeTriangles,color);
    static void Pad(Vector3 p,float x,float z){p.y=-.009f;Add(new[]{p+new Vector3(-x/2,0,-z/2),p+new Vector3(-x/2,0,z/2),p+new Vector3(x/2,0,z/2),p+new Vector3(x/2,0,-z/2)},new[]{0,1,2,0,2,3},0);}
    static void Planter(Vector3 p)
    {
        Box(p+Vector3.up*.25f,new Vector3(.85f,.5f,.85f),10);Box(p+Vector3.up*.51f,new Vector3(.7f,.04f,.7f),3);
        for(int i=0;i<3;i++){var q=p+new Vector3((i-1)*.22f,.64f,i%2==0?.16f:-.16f);Box(q,new Vector3(.3f,.24f,.32f),2);Box(q+Vector3.up*.15f,new Vector3(.18f,.08f,.18f),i%2==0?4:5);}
    }
    static void Bench(Vector3 p)
    {
        // Existing seat faces +Z, toward the lake. Add its missing back and legs.
        foreach(float x in new[]{-1.2f,1.2f})foreach(float z in new[]{-.26f,.26f})Box(p+new Vector3(x,-.30f,z),new Vector3(.16f,.6f,.16f),10);
        foreach(float x in new[]{-1.3f,1.3f})Box(p+new Vector3(x,.35f,-.33f),new Vector3(.14f,.9f,.14f),10);
        foreach(float y in new[]{.45f,.72f})Box(p+new Vector3(0,y,-.35f),new Vector3(3.5f,.19f,.14f),10);
    }
    static void Picnic(Vector3 p)
    {
        Pad(p,7.8f,6);Box(p+Vector3.up*.91f,new Vector3(2.8f,.17f,1.5f),10);
        foreach(float x in new[]{-.95f,.95f}){Box(p+new Vector3(x,.43f,0),new Vector3(.2f,.86f,1.1f),10);Box(p+new Vector3(x,.25f,0),new Vector3(.18f,.15f,3.3f),10);}
        foreach(float z in new[]{-1.35f,1.35f}){Box(p+new Vector3(0,.48f,z),new Vector3(3.1f,.16f,.55f),10);foreach(float x in new[]{-1.05f,1.05f})Box(p+new Vector3(x,.22f,z),new Vector3(.15f,.44f,.35f),10);}
        Planter(p+new Vector3(2.7f,0,1.7f));Planter(p+new Vector3(-2.7f,0,1.7f));
    }
    static float Shore(float x)
    {
        var edge=new[]{new Vector2(-430,200),new Vector2(-300,120),new Vector2(-160,100),new Vector2(-60,130),new Vector2(30,155),new Vector2(120,155),new Vector2(205,150),new Vector2(330,215)};
        for(int i=1;i<edge.Length;i++)if(x<=edge[i].x)return Mathf.Lerp(edge[i-1].y,edge[i].y,Mathf.InverseLerp(edge[i-1].x,edge[i].x,x));throw new Exception("Outside waterfront");
    }
    public static string Run()
    {
        var scene=SceneManager.GetActiveScene();if(EditorApplication.isPlaying||scene.isDirty||scene.path!="Assets/Scenes/LakeTownScene.unity")throw new Exception("Clean Lake Town Edit Mode required");
        var town=Object.FindAnyObjectByType<LakeTownNightLights>().transform;if(town.Find("Lakeside leisure details")!=null||AssetDatabase.IsValidFolder(Dir))throw new Exception("Lakeside details already exist");
        groups=new Dictionary<Vector2Int,Geometry>();reserved=new List<Bounds>();ground=town.GetComponentsInChildren<MeshCollider>().Single(c=>c.name=="Town ground");roads=town.GetComponentsInChildren<MeshCollider>().Where(c=>c.GetComponent<TestDriveAudioSurface>()!=null).ToArray();
        var road=roads.Single(c=>c.name=="Lake Town Road");var rv=road.sharedMesh.vertices;centerline=Enumerable.Range(0,rv.Length/2).Select(i=>road.transform.TransformPoint((rv[i*2]+rv[i*2+1])*.5f)).ToArray();
        foreach(var c in town.GetComponentsInChildren<BoxCollider>())if(c.name.StartsWith("Town house ")||c.name.EndsWith(" post"))reserved.Add(c.bounds);
        foreach(var t in town.GetComponentsInChildren<Transform>().Where(t=>t.name=="Pine trunk"))reserved.Add(new Bounds(t.position,new Vector3(3,1,3)));
        foreach(var rootName in new[]{"Residential garden details","Meadow field details"})foreach(var f in town.Find(rootName).GetComponentsInChildren<MeshFilter>())
        {
            var mesh=f.sharedMesh;var v=mesh.vertices;var uv=mesh.uv;var t=mesh.triangles;
            for(int i=0;i<t.Length;i+=3){int color=Mathf.FloorToInt(uv[t[i]].x*16);if(new[]{6,7,11,14}.Contains(color))continue;var b=new Bounds(f.transform.TransformPoint(v[t[i]]),Vector3.zero);b.Encapsulate(f.transform.TransformPoint(v[t[i+1]]));b.Encapsulate(f.transform.TransformPoint(v[t[i+2]]));reserved.Add(b);}
        }
        var primitive=GameObject.CreatePrimitive(PrimitiveType.Cube);cube=primitive.GetComponent<MeshFilter>().sharedMesh.vertices;cubeTriangles=primitive.GetComponent<MeshFilter>().sharedMesh.triangles;Object.DestroyImmediate(primitive);
        int benches=0,pads=0,planters=0,picnics=0,reeds=0;
        foreach(var t in town.GetComponentsInChildren<Transform>().Where(t=>t.name=="Promenade bench"))
        {
            Bench(t.position);benches++;var p=t.position;p.y=0;
            if(Clear(p,5.8f,3.4f)){Pad(p,5.8f,3.4f);pads++;foreach(float side in new[]{-1f,1f}){Planter(p+new Vector3(side*2.35f,0,0));planters++;}}
            reserved.Add(new Bounds(p,new Vector3(6.4f,1,4)));
        }
        foreach(var preferred in new[]{new Vector3(-15,0,139),new Vector3(173,0,131)})
        {
            bool found=false;for(int ring=0;ring<=5&&!found;ring++)foreach(var offset in new[]{Vector3.zero,Vector3.left,Vector3.right,Vector3.forward,Vector3.back}){var p=preferred+offset*ring*2;if(!Clear(p,8.8f,7))continue;Picnic(p);reserved.Add(new Bounds(p,new Vector3(9.5f,1,7.5f)));picnics++;planters+=2;found=true;break;}
            if(!found)throw new Exception("No clear picnic location around "+preferred);
        }
        var rng=new System.Random(7721);
        for(float x=-280;x<=245;x+=22)
        {
            var p=new Vector3(x,-.03f,Shore(x)-1);if(!Clear(p,2,1.1f))continue;
            for(int i=0;i<9;i++)
            {
                var q=p+new Vector3(((float)rng.NextDouble()-.5f)*1.5f,0,((float)rng.NextDouble()-.5f)*.55f);float h=.75f+(float)rng.NextDouble()*.65f;
                // Opaque crossed blades with both faces, no transparent sorting or animation.
                foreach(var axis in new[]{Vector3.right,Vector3.forward}){var side=axis*.065f;var tip=q+Vector3.up*h+axis*.16f;Add(new[]{q-side,q+side,tip,q-side,q+side,tip},new[]{0,2,1,3,4,5},i%3==0?1:2);}
            }
            reeds++;
        }
        int tris=groups.Values.Sum(g=>g.t.Count/3);if(benches!=4||picnics!=2||reeds<12||tris>5000)throw new Exception("Unexpected lakeside density: "+benches+" benches, "+reeds+" reeds, "+tris+" triangles");
        // Check every generated vertex before creating persistent assets.
        foreach(var p in groups.Values.SelectMany(g=>g.v))if(!ground.Raycast(new Ray(new Vector3(p.x,20,p.z),Vector3.down),out var hit,30)||roads.Any(r=>r.Raycast(new Ray(new Vector3(p.x,20,p.z),Vector3.down),out var rh,30)))throw new Exception("Detail outside clear land at "+p);
        AssetDatabase.CreateFolder("Assets/Environments/LakeTownTrial","LakesideDetails");var root=new GameObject("Lakeside leisure details").transform;root.SetParent(town,false);var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Environments/LakeTownTrial/ResidentialDetails/Residential details shared.mat");bool first=true;
        foreach(var pair in groups){var g=pair.Value;var mesh=new Mesh{name="Lakeside tile "+pair.Key};mesh.SetVertices(g.v);mesh.SetTriangles(g.t,0);mesh.SetUVs(0,g.uv);mesh.RecalculateNormals();mesh.RecalculateBounds();string path=Dir+"/Lakeside geometry.asset";if(first){AssetDatabase.CreateAsset(mesh,path);first=false;}else AssetDatabase.AddObjectToAsset(mesh,path);var go=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=mat;go.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;}
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return benches+" completed water-facing benches, "+pads+" seating pads, "+picnics+" picnic tables, "+planters+" planters, "+reeds+" shoreline reed clusters; "+tris+" triangles, "+groups.Count+" shared-material batches. No new lights, colliders or runtime scripts.";
    }
}
