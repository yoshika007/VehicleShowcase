using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using Object=UnityEngine.Object;

// Repair existing furniture in place: retain the road, lamps, signs and night-light references.
public static class GroundHarborStreetFurniture
{
    const string Dir="Assets/Environments/HarborStreet/";
    static Transform root;
    static Vector3[] route,sides;
    static Vector3[] metalVertices;
    static readonly HashSet<int> changed=new HashSet<int>();
    static readonly List<CombineInstance> concrete=new List<CombineInstance>(),braces=new List<CombineInstance>();
    static Mesh cube;
    static void Init()
    {
        root=GameObject.Find("Harbor Street Course").transform;
        var v=GameObject.Find("Harbor Street asphalt").GetComponent<MeshCollider>().sharedMesh.vertices;
        int count=v.Length/2-1;route=new Vector3[count];sides=new Vector3[count];
        for(int i=0;i<count;i++){route[i]=(v[i*2]+v[i*2+1])*.5f;sides[i]=(v[i*2+1]-v[i*2]).normalized;}
    }
    static Vector3 Foot(Vector3 original)
    {
        int i=Enumerable.Range(0,route.Length).OrderBy(j=>(route[j]-original).sqrMagnitude).First();
        float sign=Vector3.Dot(original-route[i],sides[i])<0?-1:1;
        return route[i]+sides[i]*sign*6.9f-Vector3.up*.12f;
    }
    static bool OnSidewalk(Vector3 foot)
    {
        var collider=GameObject.Find("City sidewalk").GetComponent<MeshCollider>();
        return collider.Raycast(new Ray(foot+Vector3.up,Vector3.down),out var hit,1.15f)&&Mathf.Abs(hit.point.y-foot.y)<.07f;
    }
    static TMP_Text[] Signs()=>root.GetComponentsInChildren<TMP_Text>().Where(t=>Mathf.Abs(t.rectTransform.sizeDelta.y-1.5f)<.001f).ToArray();
    public static string Inspect()
    {
        Init();var lamps=root.GetComponentsInChildren<Light>().Where(l=>l.name=="Harbor streetlight").ToArray();
        int supported=lamps.Count(l=>OnSidewalk(l.transform.position-Vector3.up*7.3f));
        int signs=0,supportedSigns=0;
        var vertices=AssetDatabase.LoadAssetAtPath<Mesh>(Dir+"Ironwork scenery.asset").vertices;
        for(int i=0;i<vertices.Length;i+=24)
        {
            var bounds=new Bounds(vertices[i],Vector3.zero);for(int j=1;j<24;j++)bounds.Encapsulate(vertices[i+j]);
            if(Mathf.Abs(bounds.size.y-4.6f)>.01f||bounds.size.x>.3f||bounds.size.z>.3f)continue;
            signs++;if(OnSidewalk(bounds.center-Vector3.up*2.3f))supportedSigns++;
        }
        return lamps.Length+" streetlights: "+supported+" bases on sidewalk; "+signs+" signs: "+supportedSigns+" post bases on sidewalk.";
    }
    public static string View(bool sign)
    {
        Init();UnityEngine.Object.FindAnyObjectByType<TestDriveCameraFollow>().enabled=false;
        Vector3 foot,forward;
        if(sign)
        {
            var label=Signs().Single(t=>t.name=="HOTEL HAIRPIN");forward=label.transform.forward;
            foot=Foot(label.transform.position+forward*.12f-Vector3.up*4.1f);
        }
        else
        {
            var lamp=root.GetComponentsInChildren<Light>().Where(l=>l.name=="Harbor streetlight").OrderByDescending(l=>l.transform.position.y).First();
            foot=lamp.transform.position-Vector3.up*7.3f;
            int i=Enumerable.Range(0,route.Length).OrderBy(j=>(route[j]-foot).sqrMagnitude).First();
            forward=(route[(i+1)%route.Length]-route[i]).normalized;
        }
        var side=Vector3.Cross(Vector3.up,forward).normalized;
        var camera=Camera.main;camera.transform.position=foot-side*8-forward*9+Vector3.up*5;
        camera.transform.LookAt(foot+Vector3.up*(sign?2.3f:3.8f));camera.fieldOfView=52;
        return (sign?"Hotel sign":"Highest streetlight")+" support view; base="+foot;
    }
    static void MovePost(Vector3 from,Vector3 to,float height)
    {
        int match=-1;
        for(int i=0;i<metalVertices.Length;i+=24)
        {
            if(changed.Contains(i))continue;
            var b=new Bounds(metalVertices[i],Vector3.zero);for(int j=1;j<24;j++)b.Encapsulate(metalVertices[i+j]);
            if((b.center-(from+Vector3.up*height*.5f)).sqrMagnitude<.0001f&&Mathf.Abs(b.size.y-height)<.01f&&b.size.x<.3f&&b.size.z<.3f)
            {match=i;break;}
        }
        if(match<0)throw new Exception("Could not identify pole at "+from);
        var delta=to-from;for(int j=0;j<24;j++)metalVertices[match+j]+=delta;changed.Add(match);
    }
    static void Box(List<CombineInstance> list,Vector3 p,Vector3 size,Quaternion rotation)
        =>list.Add(new CombineInstance{mesh=cube,transform=Matrix4x4.TRS(p,rotation,size)});
    static void Base(Vector3 p)=>Box(concrete,p+Vector3.up*.07f,new Vector3(.6f,.24f,.6f),Quaternion.identity);
    static void Beam(Vector3 a,Vector3 b,float width)
        =>Box(braces,(a+b)*.5f,new Vector3(width,width,Vector3.Distance(a,b)),Quaternion.LookRotation(b-a));
    static void Batch(string name,List<CombineInstance> parts,Material material,Transform parent)
    {
        var mesh=new Mesh{name=name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.CombineMeshes(parts.ToArray());
        AssetDatabase.CreateAsset(mesh,Dir+name+".asset");
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.isStatic=true;
        go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<Renderer>().sharedMaterial=material;
    }
    public static string Run()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(EditorApplication.isPlaying||scene.isDirty||scene.name!="HarborStreetScene")throw new Exception("Use the saved Harbor Street scene outside Play Mode.");
        Init();if(root.Find("Grounded street furniture"))throw new Exception("Support repair already applied.");
        string before=Inspect();changed.Clear();concrete.Clear();braces.Clear();
        var source=AssetDatabase.LoadAssetAtPath<Mesh>(Dir+"Ironwork scenery.asset");metalVertices=source.vertices;
        var temp=GameObject.CreatePrimitive(PrimitiveType.Cube);cube=temp.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(temp);
        var lamps=root.GetComponentsInChildren<Light>().Where(l=>l.name=="Harbor streetlight").ToArray();
        var lenses=root.GetComponentsInChildren<Renderer>().Where(r=>r.name=="Boulevard lamp lens").ToArray();
        foreach(var lamp in lamps)
        {
            var original=lamp.transform.position-Vector3.up*7.3f;var foot=Foot(original);
            if(!OnSidewalk(foot))throw new Exception("No sidewalk beneath new lamp base "+foot);
            MovePost(original,foot,7.5f);Base(foot);
            var lens=lenses.OrderBy(l=>(l.transform.position-(lamp.transform.position+Vector3.up*.2f)).sqrMagnitude).First();
            if((lens.transform.position-(lamp.transform.position+Vector3.up*.2f)).sqrMagnitude>.001f)throw new Exception("Missing matching lamp lens.");
            var delta=foot-original;lamp.transform.position+=delta;lens.transform.position+=delta;
        }
        int signs=0;
        foreach(var sign in Signs())
        {
            var f=sign.transform.forward;var board=sign.transform.position+f*.12f;
            var original=board-Vector3.up*4.1f;var foot=Foot(original);
            if(!OnSidewalk(foot))throw new Exception("No sidewalk beneath sign base "+sign.name);
            MovePost(original,foot,4.6f);Base(foot);
            var end=original+Vector3.up*4.5f+f*.16f;
            Beam(foot+Vector3.up*4.5f,end,.18f);Beam(foot+Vector3.up*3.3f,end,.14f);signs++;
        }
        source.vertices=metalVertices;source.RecalculateBounds();EditorUtility.SetDirty(source);AssetDatabase.SaveAssetIfDirty(source);
        var group=new GameObject("Grounded street furniture").transform;group.SetParent(root,false);
        Batch("Street furniture footings",concrete,AssetDatabase.LoadAssetAtPath<Material>(Dir+"Coastal concrete.mat"),group);
        Batch("Road sign support braces",braces,AssetDatabase.LoadAssetAtPath<Material>(Dir+"Ironwork.mat"),group);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return before+" Repaired "+lamps.Length+" lights and "+signs+" signs; all "+(lamps.Length+signs)+" base locations verified against sidewalk collision mesh. Road/collision geometry unchanged.";
    }
}
