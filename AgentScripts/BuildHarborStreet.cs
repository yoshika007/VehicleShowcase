using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using Object = UnityEngine.Object;

// Original Mediterranean street course; not a replica of a licensed race venue.
// Generated scenery is batched by material; driveable surfaces retain simple colliders.
public static class BuildHarborStreet
{
    const string Dir="Assets/Environments/HarborStreet";
    const string ScenePath="Assets/Scenes/HarborStreetScene.unity";
    static Transform root;
    static Vector3[] route;
    static Mesh cube;
    static Dictionary<Material,List<CombineInstance>> batches;
    static Material road,walk,stone,white,red,metal,glass,roof,water,wood,navy,gold,leaf,trunk,lens,glow,nightGlass;
    static Material[] facades;
    static List<Light> streetLights;
    static List<Renderer> lanterns,windows;
    static int tunnelStart,tunnelEnd;
    public static Vector3[] Route()
    {
        var k=new[]{
            new Vector3(-170,0,-170),new Vector3(-50,0,-170),new Vector3(85,0,-170),
            new Vector3(140,1,-150),new Vector3(155,4,-115),new Vector3(145,10,-70),
            new Vector3(125,18,-10),new Vector3(80,25,35),new Vector3(20,28,50),
            new Vector3(-25,27,70),new Vector3(-45,24,100),new Vector3(-20,20,125),
            new Vector3(10,17,120),new Vector3(25,14,98),new Vector3(17,12,80),
            new Vector3(0,11,85),new Vector3(-7,10,105),new Vector3(-4,9,133),
            new Vector3(35,7,155),new Vector3(105,4,145),new Vector3(160,2,100),
            new Vector3(200,0,35),new Vector3(205,0,-40),new Vector3(235,0,-65),
            new Vector3(260,0,-45),new Vector3(260,0,20),new Vector3(245,0,95),
            new Vector3(205,0,170),new Vector3(125,0,200),new Vector3(10,0,205),
            new Vector3(-100,0,190),new Vector3(-180,0,155),new Vector3(-210,0,110),
            new Vector3(-205,0,40),new Vector3(-190,0,5),new Vector3(-205,0,-20),
            new Vector3(-190,0,-45),new Vector3(-205,0,-80),new Vector3(-195,0,-135)};
        var result=new List<Vector3>();
        for(int i=0;i<k.Length;i++)
        {
            var a=k[(i+k.Length-1)%k.Length];var b=k[i];var c=k[(i+1)%k.Length];var d=k[(i+2)%k.Length];
            int n=Mathf.CeilToInt(Vector3.Distance(b,c)/1.5f);
            for(int j=0;j<n;j++)
            {
                float t=j/(float)n;
                result.Add(.5f*(2*b+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t));
            }
        }
        return result.ToArray();
    }
    static int Near(Vector3 point)=>Enumerable.Range(0,route.Length).OrderBy(i=>(route[i]-point).sqrMagnitude).First();
    static Vector3 Forward(int i)=>(route[(i+1)%route.Length]-route[(i+route.Length-1)%route.Length]).normalized;
    static Vector3 Side(int i)=>Vector3.Cross(Vector3.up,Forward(i)).normalized;
    static Material Mat(string name,Color color,float smooth=.15f,float emission=0)
    {
        var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,color=color};
        m.SetFloat("_Smoothness",smooth);
        if(emission>0){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*emission);m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;}
        AssetDatabase.CreateAsset(m,Dir+"/"+name+".mat");return m;
    }
    static void Box(Vector3 p,Vector3 size,Material material,Quaternion rotation=default)
    {
        if(rotation.Equals(default(Quaternion)))rotation=Quaternion.identity;
        if(!batches.ContainsKey(material))batches[material]=new List<CombineInstance>();
        batches[material].Add(new CombineInstance{mesh=cube,transform=Matrix4x4.TRS(p,rotation,size)});
    }
    static Mesh SaveMesh(string name,List<Vector3> v,List<int> t)
    {
        var m=new Mesh{name=name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
        m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();
        AssetDatabase.CreateAsset(m,Dir+"/"+name+".asset");return m;
    }
    static GameObject MeshObject(string name,Mesh mesh,Material material,bool collider=false)
    {
        var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(root,false);
        g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<Renderer>().sharedMaterial=material;g.isStatic=true;
        if(collider)g.AddComponent<MeshCollider>().sharedMesh=mesh;return g;
    }
    static void Quad(List<Vector3> v,List<int> t,Vector3 a,Vector3 b,Vector3 c,Vector3 d,bool doubleSided=false)
    {
        int n=v.Count;v.AddRange(new[]{a,b,c,d});t.AddRange(new[]{n,n+1,n+2,n+2,n+1,n+3});
        if(doubleSided)
        {
            // Separate back-face vertices keep their normals from cancelling the front.
            n=v.Count;v.AddRange(new[]{a,b,c,d});t.AddRange(new[]{n+2,n+1,n,n+3,n+1,n+2});
        }
    }
    static GameObject Ribbon(string name,float width,float height,Material material,bool collider=false,float offset=0)
    {
        var v=new List<Vector3>();var t=new List<int>();
        for(int i=0;i<=route.Length;i++)
        {
            int j=i%route.Length;v.Add(route[j]+Side(j)*(offset-width*.5f)+Vector3.up*height);
            v.Add(route[j]+Side(j)*(offset+width*.5f)+Vector3.up*height);
            if(i>0){int n=i*2;t.AddRange(new[]{n-2,n,n-1,n-1,n,n+1});}
        }
        return MeshObject(name,SaveMesh(name,v,t),material,collider);
    }
    static void RoadAndWalls()
    {
        // Keep the elevated town roads solid down to the flat city base.
        var v=new List<Vector3>();var t=new List<int>();
        for(int i=0;i<route.Length;i++)
        {
            int j=(i+1)%route.Length;
            foreach(int sign in new[]{-1,1})
            {
                var a=route[i]+Side(i)*sign*7.6f;var b=route[j]+Side(j)*sign*7.6f;
                Quad(v,t,a-Vector3.up*.1f,b-Vector3.up*.1f,new Vector3(a.x,-.5f,a.z),new Vector3(b.x,-.5f,b.z),true);
            }
        }
        MeshObject("City retaining walls",SaveMesh("City retaining walls",v,t),stone);
        Ribbon("City sidewalk",15.2f,-.12f,walk,true);
        Ribbon("Harbor Street asphalt",10.5f,0,road,true);
        // Subtle lane edges and alternating red/ivory curb blocks.
        Ribbon("Left road edge",.11f,.016f,white,false,-5.05f);
        Ribbon("Right road edge",.11f,.016f,white,false,5.05f);
        foreach(int sign in new[]{-1,1})
        {
            v=new List<Vector3>();t=new List<int>();
            for(int i=0;i<route.Length;i++)
            {
                int j=(i+1)%route.Length;
                var a=route[i]+Side(i)*sign*5.9f;var b=route[j]+Side(j)*sign*5.9f;
                // A continuous wall with road-side face, outer face and top.
                var n=Side(i)*.22f;var m=Side(j)*.22f;
                Quad(v,t,a-n,b-m,a-n+Vector3.up*1.05f,b-m+Vector3.up*1.05f,true);
                Quad(v,t,a+n,b+m,a+n+Vector3.up*1.05f,b+m+Vector3.up*1.05f,true);
                Quad(v,t,a-n+Vector3.up*1.05f,b-m+Vector3.up*1.05f,a+n+Vector3.up*1.05f,b+m+Vector3.up*1.05f,true);
                if(i%2==0)
                {
                    var p=(route[i]+route[j])*.5f+Side(i)*sign*5.45f;
                    Box(p+Vector3.up*.045f,new Vector3(.35f,.09f,Vector3.Distance(route[i],route[j])*2.02f),i%4==0?red:white,Quaternion.LookRotation(Forward(i)));
                }
            }
            MeshObject(sign<0?"Left city safety wall":"Right city safety wall",SaveMesh(sign<0?"Left city safety wall":"Right city safety wall",v,t),white,true);
        }
        // Start/finish checkerboard and small grid boxes on the harbor straight.
        for(int x=0;x<10;x++)for(int z=0;z<2;z++)
            Box(new Vector3(-110+z*.65f,.025f,-174.5f+x),new Vector3(.65f,.022f,.9f),(x+z)%2==0?white:navy);
        for(int n=0;n<6;n++)foreach(float z in new[]{-172.5f,-167.5f})
            Box(new Vector3(-130-n*5,.022f,z),new Vector3(.12f,.02f,2.4f),white);
    }
    static Renderer SeparateBox(string name,Vector3 p,Vector3 size,Material material,Quaternion rotation=default)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(root,false);
        g.transform.position=p;g.transform.localScale=size;g.transform.rotation=rotation.Equals(default(Quaternion))?Quaternion.identity:rotation;
        g.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(g.GetComponent<Collider>());g.isStatic=true;return g.GetComponent<Renderer>();
    }
    static void Lamp(Vector3 foot,Vector3 direction,bool tunnel=false)
    {
        float h=tunnel?6.6f:7.5f;
        if(!tunnel)Box(foot+Vector3.up*h*.5f,new Vector3(.18f,h,.18f),metal);
        var position=foot+Vector3.up*h;
        var lensRenderer=SeparateBox(tunnel?"Tunnel lamp lens":"Boulevard lamp lens",position,new Vector3(1.3f,.12f,.5f),lens);
        lanterns.Add(lensRenderer);
        var g=new GameObject(tunnel?"Tunnel lighting":"Harbor streetlight",typeof(Light));g.transform.SetParent(root,false);
        var light=g.GetComponent<Light>();light.type=LightType.Spot;light.range=24;light.spotAngle=110;light.innerSpotAngle=70;
        light.intensity=tunnel?95:110;light.color=new Color(1f,.82f,.57f);light.shadows=LightShadows.None;
        light.transform.position=position-Vector3.up*.2f;light.transform.rotation=Quaternion.LookRotation(direction-Vector3.up*7);
        if(!tunnel){light.enabled=false;streetLights.Add(light);} // Tunnel lights remain on by day, too.
    }
    static void Tunnel()
    {
        tunnelStart=Near(new Vector3(160,2,100));tunnelEnd=Near(new Vector3(205,0,-40));
        var v=new List<Vector3>();var t=new List<int>();
        for(int i=tunnelStart;i<tunnelEnd;i++)
        {
            int j=i+1;var a=route[i];var b=route[j];var s=Side(i);var r=Side(j);
            Quad(v,t,a-s*8+Vector3.up*7.6f,b-r*8+Vector3.up*7.6f,a+s*8+Vector3.up*7.6f,b+r*8+Vector3.up*7.6f,true);
            Quad(v,t,a-s*8+Vector3.up*8.4f,b-r*8+Vector3.up*8.4f,a+s*8+Vector3.up*8.4f,b+r*8+Vector3.up*8.4f,true);
            Quad(v,t,a+s*7.3f,b+r*7.3f,a+s*7.3f+Vector3.up*8.4f,b+r*7.3f+Vector3.up*8.4f,true);
            Quad(v,t,a-s*8+Vector3.up*7.6f,b-r*8+Vector3.up*7.6f,a-s*8+Vector3.up*8.4f,b-r*8+Vector3.up*8.4f,true);
            if((i-tunnelStart)%9==0)
            {
                var rotation=Quaternion.LookRotation(Forward(i));
                Box(a-s*7.2f+Vector3.up*3.8f,new Vector3(.7f,7.6f,.8f),stone,rotation);
                Box(a+Vector3.up*7.35f,new Vector3(14.6f,.5f,.55f),walk,rotation);
                Lamp(a,Forward(i)*3,true);
            }
        }
        MeshObject("Seaside covered tunnel",SaveMesh("Seaside covered tunnel",v,t),stone,true);
        foreach(int i in new[]{tunnelStart,tunnelEnd})
        {
            var rot=Quaternion.LookRotation(Forward(i));
            Box(route[i]+Vector3.up*8.5f,new Vector3(17,1.6f,1.3f),walk,rot);
            foreach(int side in new[]{-1,1})Box(route[i]+Side(i)*side*7.3f+Vector3.up*4,new Vector3(1,8,1.3f),walk,rot);
        }
    }
    static void Text(string value,Vector3 p,Vector3 facing,float size,Vector2 area,Color color)
    {
        var g=new GameObject(value,typeof(TextMeshPro));g.transform.SetParent(root,false);g.transform.position=p;
        g.transform.rotation=Quaternion.LookRotation(facing);
        var tmp=g.GetComponent<TextMeshPro>();tmp.text=value;tmp.fontSize=size;tmp.color=color;tmp.alignment=TextAlignmentOptions.Center;
        tmp.rectTransform.sizeDelta=area;tmp.textWrappingMode=TextWrappingModes.NoWrap;
    }
    static void Sign(string label,Vector3 target,float side=9f,float width=11)
    {
        int i=Near(target);var p=route[i]+Side(i)*side;var f=Forward(i);var rot=Quaternion.LookRotation(f);
        Box(p+Vector3.up*2.3f,new Vector3(.18f,4.6f,.18f),metal);
        Box(p+Vector3.up*4.1f,new Vector3(width,1.7f,.18f),navy,rot);
        Text(label,p+Vector3.up*4.1f-f*.12f,f,5,new Vector2(width-.3f,1.5f),gold.color);
    }
    static void Building(Vector3 p,float width,float depth,float height,int style)
    {
        var facade=facades[style%facades.Length];
        if(p.y>0)Box(new Vector3(p.x,(p.y-.4f)*.5f,p.z),new Vector3(width,p.y+.4f,depth),stone);
        Box(p+Vector3.up*height*.5f,new Vector3(width,height,depth),facade);
        Box(p+Vector3.up*.5f,new Vector3(width+.4f,1,depth+.4f),stone);
        Box(p+Vector3.up*(height+.35f),new Vector3(width+1,.7f,depth+1),walk);
        Box(p+Vector3.up*(height+.95f),new Vector3(width*.88f,.6f,depth*.88f),roof);
        int floors=Mathf.FloorToInt(height/3.7f),columns=Mathf.Max(2,Mathf.FloorToInt(width/3.3f));
        for(int floor=0;floor<floors;floor++)
        {
            float y=2.1f+floor*3.7f;
            foreach(int face in new[]{-1,1})
            {
                for(int x=0;x<columns;x++)
                {
                    float px=(x-(columns-1)*.5f)*(width/(columns+.4f));
                    Box(p+new Vector3(px,y,face*(depth*.5f+.035f)),new Vector3(1.35f,1.85f,.09f),glass);
                    if(floor>0&&floor%2==0)
                    {
                        Box(p+new Vector3(px,y-.9f,face*(depth*.5f+.5f)),new Vector3(2.2f,.16f,1.1f),walk);
                        Box(p+new Vector3(px,y-.5f,face*(depth*.5f+1f)),new Vector3(2.2f,.08f,.08f),metal);
                    }
                }
                Box(p+new Vector3(0,y-1.3f,face*(depth*.5f+.1f)),new Vector3(width+.2f,.14f,.25f),walk);
            }
            int sideColumns=Mathf.Max(2,Mathf.FloorToInt(depth/3.5f));
            foreach(int face in new[]{-1,1})for(int z=0;z<sideColumns;z++)
                Box(p+new Vector3(face*(width*.5f+.04f),y,(z-(sideColumns-1)*.5f)*(depth/(sideColumns+.4f))),new Vector3(.09f,1.85f,1.35f),glass);
        }
        // Door and shop awning use separate positions from the ground-floor windows.
        Box(p+new Vector3(width*.35f,1.2f,-depth*.5f-.1f),new Vector3(1.1f,2.4f,.15f),navy);
        Box(p+new Vector3(0,3.2f,-depth*.5f-.7f),new Vector3(width*.65f,.18f,1.5f),style%2==0?red:navy);
    }
    static void City()
    {
        int count=0;var random=new System.Random(261101);
        // Leave a continuous safety envelope around every sampled road, including the hairpin.
        for(float z=-135;z<=175;z+=35)for(float x=-160;x<=140;x+=35)
        {
            float width=18+random.Next(7),depth=18+random.Next(6);
            var p=new Vector3(x+random.Next(-3,4),-.4f,z+random.Next(-3,4));
            float radius=new Vector2(width+2,depth+2).magnitude*.5f;
            if(route.Any(q=>new Vector2(q.x-p.x,q.z-p.z).magnitude<radius+10))continue;
            float h=22+random.Next(26);
            // Raise foundations near the upper boulevard so the buildings face the elevated street.
            int nearest=Near(p);p.y=Mathf.Max(-.4f,route[nearest].y-4);
            Building(p,width,depth,h,count++);
        }
        // The hotel sits outside the full hairpin envelope, overlooking the slow section.
        var hotel=new Vector3(-80,17,106);Building(hotel,26,32,32,1);
        Text("HOTEL RIVIERA",hotel+new Vector3(0,33,-16.6f),Vector3.forward,5.5f,new Vector2(25,2),navy.color);
        for(int i=0;i<route.Length;i+=28)
        {
            if(i>=tunnelStart-8&&i<=tunnelEnd+8)continue;
            int sign=i%56==0?1:-1;var foot=route[i]+Side(i)*sign*8.4f;
            if(route.Where((q,j)=>Mathf.Abs(j-i)>15).Any(q=>new Vector2(q.x-foot.x,q.z-foot.z).magnitude<7))continue;
            Lamp(foot,Side(i)*-sign*2);
        }
        // Palms line the harbor quay; all trunks sit beyond the driving barriers.
        for(int i=0;i<10;i++)
        {
            var p=new Vector3(-170+i*32,0,-190);
            Box(p+Vector3.up*4,new Vector3(.45f,8,.45f),trunk);
            for(int a=0;a<6;a++)
            {
                var rot=Quaternion.Euler(0,a*60,12);
                Box(p+Vector3.up*8+Quaternion.Euler(0,a*60,0)*Vector3.forward*1.7f,new Vector3(.8f,.18f,4.8f),leaf,rot);
            }
        }
    }
    static void Harbor()
    {
        Box(new Vector3(-2.5f,-.65f,30),new Vector3(565,.5f,440),stone);
        Box(new Vector3(0,-1.3f,-390),new Vector3(1500,.4f,400),water);
        Box(new Vector3(530,-1.3f,0),new Vector3(500,.4f,1200),water);
        Box(new Vector3(10,-.3f,-193),new Vector3(560,.7f,9),walk);
        Box(new Vector3(10,-1,-198),new Vector3(560,3,1.2f),stone);
        // Finger piers and low-poly yachts make the harbor visible from the straight and intro.
        for(int pier=0;pier<4;pier++)
        {
            float x=-135+pier*100;
            Box(new Vector3(x,-.35f,-231),new Vector3(4,.65f,66),wood);
            for(int n=0;n<3;n++)foreach(int side in new[]{-1,1})
            {
                var p=new Vector3(x+side*13,-.4f,-213-n*18);
                Box(p,new Vector3(7,1.5f,14),white);
                Box(p+Vector3.up*1.1f,new Vector3(5.7f,1,10),navy);
                Box(p+Vector3.up*2.1f+Vector3.forward,new Vector3(4.6f,1.1f,6),white);
                Box(p+Vector3.up*2.3f+Vector3.forward*4.05f,new Vector3(4,.65f,.1f),glass);
                Box(p+Vector3.up*3,new Vector3(.1f,2,.1f),metal);
            }
        }
        // Quiet water highlights and distant coastal silhouettes.
        for(int i=0;i<30;i++)Box(new Vector3(-280+i*28,-1.085f,-285-(i%4)*35),new Vector3(12,.01f,.12f),walk);
        for(int i=0;i<15;i++)Box(new Vector3(-290+i*42,8+i%4*7,285+i%3*25),new Vector3(30,18+i%4*14,30),facades[i%facades.Length]);
    }
    static void Batch()
    {
        foreach(var pair in batches)
        {
            var mesh=new Mesh{name=pair.Key.name+" scenery",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
            mesh.CombineMeshes(pair.Value.ToArray());
            AssetDatabase.CreateAsset(mesh,Dir+"/"+mesh.name+".asset");
            var g=MeshObject("Batched "+pair.Key.name,mesh,pair.Key);
            if(pair.Key==glass)windows.Add(g.GetComponent<Renderer>());
        }
    }
    static void NightLights()
    {
        var c=root.gameObject.AddComponent<LakeTownNightLights>();var data=new SerializedObject(c);
        foreach(var spec in new[]{new {name="streetlights",values=streetLights.Cast<Object>().ToArray()},
            new {name="lanterns",values=lanterns.Cast<Object>().ToArray()},new {name="windows",values=windows.Cast<Object>().ToArray()}})
        {
            var a=data.FindProperty(spec.name);a.arraySize=spec.values.Length;
            for(int i=0;i<a.arraySize;i++)a.GetArrayElementAtIndex(i).objectReferenceValue=spec.values[i];
        }
        data.FindProperty("nightLanterns").objectReferenceValue=glow;data.FindProperty("nightWindows").objectReferenceValue=nightGlass;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
    public static string Run()
    {
        if(EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
            throw new Exception("Stop Play Mode and preserve any dirty scene before building.");
        if(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)!=null||AssetDatabase.IsValidFolder(Dir))
            throw new Exception("Harbor Street already exists; refusing to overwrite.");
        AssetDatabase.CreateFolder("Assets/Environments","HarborStreet");
        var source=EditorSceneManager.OpenScene("Assets/Scenes/MountainPassScene.unity");
        EditorSceneManager.SaveScene(source,ScenePath,true);
        var scene=EditorSceneManager.OpenScene(ScenePath);
        Object.DestroyImmediate(GameObject.Find("Mountain Pass Course"));
        var oldLights=GameObject.Find("Sparse mountain streetlights");if(oldLights)Object.DestroyImmediate(oldLights);
        var infrastructure=scene.GetRootGameObjects().First(g=>g.name.Contains("Driving Infrastructure"));infrastructure.name="Harbor Street Driving Infrastructure";
        root=new GameObject("Harbor Street Course").transform;route=Route();batches=new Dictionary<Material,List<CombineInstance>>();
        streetLights=new List<Light>();lanterns=new List<Renderer>();windows=new List<Renderer>();
        var temp=GameObject.CreatePrimitive(PrimitiveType.Cube);cube=temp.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(temp);
        road=Mat("City asphalt",new Color(.17f,.19f,.20f));walk=Mat("Pale limestone",new Color(.76f,.73f,.63f));
        stone=Mat("Coastal concrete",new Color(.47f,.51f,.50f));white=Mat("Barrier ivory",new Color(.92f,.92f,.85f));
        red=Mat("Racing red",new Color(.72f,.12f,.09f));metal=Mat("Ironwork",new Color(.12f,.19f,.21f),.3f);
        glass=Mat("Blue windows",new Color(.12f,.29f,.34f),.5f);roof=Mat("Terracotta roofs",new Color(.51f,.24f,.17f));
        water=Mat("Mediterranean water",new Color(.055f,.39f,.50f),.7f);wood=Mat("Marina decking",new Color(.44f,.32f,.21f));
        navy=Mat("Harbor navy",new Color(.025f,.12f,.18f));gold=Mat("Warm gold",new Color(.96f,.73f,.26f));
        leaf=Mat("Palm fronds",new Color(.18f,.36f,.23f));trunk=Mat("Palm trunks",new Color(.44f,.34f,.21f));
        lens=Mat("Lamp glass",new Color(.95f,.87f,.66f));glow=Mat("Warm lamp glow",new Color(1,.72f,.35f),.2f,2);
        nightGlass=Mat("Evening windows",new Color(.78f,.62f,.35f),.3f,.55f);
        facades=new[]{Mat("Warm cream facades",new Color(.84f,.75f,.59f)),Mat("Blush facades",new Color(.77f,.56f,.46f)),
            Mat("Ochre facades",new Color(.77f,.66f,.43f)),Mat("Pearl facades",new Color(.81f,.81f,.75f))};
        RoadAndWalls();Tunnel();Harbor();City();
        Sign("HARBOR STREET",new Vector3(-150,0,-170),9,14);
        Sign("HOTEL HAIRPIN",new Vector3(-20,20,125),-10,12);
        Sign("SEASIDE TUNNEL",route[tunnelStart-15],11,14);
        Sign("CHICANE / SLOW",new Vector3(-205,0,40),-10,13);
        // Start gantry is above maximum vehicle clearance.
        Box(new Vector3(-110,6,-170),new Vector3(1,1.3f,16),navy);
        foreach(int sign in new[]{-1,1})Box(new Vector3(-110,3,-170+sign*7.7f),new Vector3(.5f,6,.5f),metal);
        Text("HARBOR STREET",new Vector3(-110.55f,6,-170),Vector3.right,4.8f,new Vector2(14,1.2f),white.color);
        Batch();NightLights();
        var manager=Object.FindAnyObjectByType<TestDriveSceneManager>();var managerData=new SerializedObject(manager);
        var spawn=(Transform)managerData.FindProperty("spawnPoint").objectReferenceValue;int start=Near(new Vector3(-148,0,-170));
        spawn.position=route[start]-Side(start)*2.3f+Vector3.up*.4f;spawn.rotation=Quaternion.LookRotation(Forward(start));
        var map=Object.FindAnyObjectByType<TestDriveMinimap>();var mapData=new SerializedObject(map);
        var roadObject=GameObject.Find("Harbor Street asphalt");
        mapData.FindProperty("roadMesh").objectReferenceValue=roadObject.GetComponent<MeshCollider>().sharedMesh;
        mapData.FindProperty("roadTransform").objectReferenceValue=roadObject.transform;
        mapData.FindProperty("startLineA").vector3Value=new Vector3(-110,0,-175);
        mapData.FindProperty("startLineB").vector3Value=new Vector3(-110,0,-165);mapData.ApplyModifiedPropertiesWithoutUndo();
        foreach(var label in map.transform.parent.GetComponentsInChildren<TMP_Text>(true))if(label.text=="MOUNTAIN")label.text="HARBOR";
        var camera=Object.FindAnyObjectByType<TestDriveCameraFollow>().GetComponent<Camera>();camera.farClipPlane=1400;
        camera.transform.position=spawn.position-spawn.forward*8+Vector3.up*4;camera.transform.LookAt(spawn.position+spawn.forward*5);
        RenderSettings.fogColor=new Color(.64f,.76f,.80f);RenderSettings.fogDensity=.0009f;
        EditorSceneManager.SaveScene(scene);
        var scenes=EditorBuildSettings.scenes.ToList();scenes.Add(new EditorBuildSettingsScene(ScenePath,true));EditorBuildSettings.scenes=scenes.ToArray();
        AssetDatabase.SaveAssets();
        float length=Enumerable.Range(0,route.Length).Sum(i=>Vector3.Distance(route[i],route[(i+1)%route.Length]));
        return "Harbor Street built: "+length.ToString("F0")+"m, "+route.Max(p=>p.y).ToString("F1")+"m climb, hotel hairpin, seaside tunnel, harbor chicane, "+streetLights.Count+" night streetlights.";
    }
    public static string AddMenu()
    {
        if(EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Open a saved scene outside Play Mode.");
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/ShowcaseScene.unity");
        var menu=Object.FindObjectsByType<FreeRideCourseMenu>(FindObjectsInactive.Include).Single();var dialog=menu.transform.Find("Dialog");
        if(dialog.Find("HarborStreetCourse"))throw new Exception("Harbor card already exists.");
        var go=Object.Instantiate(dialog.Find("MountainPassCourse").gameObject,dialog,false);go.name="HarborStreetCourse";
        go.transform.Find("Label").GetComponent<TMP_Text>().text="HARBOR STREET";
        go.transform.Find("Details").GetComponent<TMP_Text>().text="City hairpin, seaside\ntunnel + marina";
        var button=go.GetComponent<UnityEngine.UI.Button>();button.onClick=new UnityEngine.UI.Button.ButtonClickedEvent();
        var manager=Object.FindAnyObjectByType<VehicleShowcaseManager>();UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick,manager.HarborStreetRide);
        var data=new SerializedObject(menu);data.FindProperty("harborStreet").objectReferenceValue=button;data.ApplyModifiedPropertiesWithoutUndo();
        // Apply the same runtime layout in edit mode, preserving the existing action button sizes.
        typeof(FreeRideCourseMenu).GetMethod("ArrangeCourses",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(menu,null);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return "Fourth course card wired to Harbor Street.";
    }
}
