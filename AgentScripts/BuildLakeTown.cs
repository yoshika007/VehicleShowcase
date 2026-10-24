using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using Object = UnityEngine.Object;

public static class BuildLakeTown
{
    const string Dir = "Assets/Environments/LakeTownTrial";
    static Transform root;
    static Material asphalt, grass, water, white, gold, wood, stone, roof, glass, trunk, leaves, dark;
    static Mesh cone, roofMesh;
    public static Vector3[] Route()
    {
        var knots = new[] { new Vector3(-250,0,70),new Vector3(-160,0,65),new Vector3(-70,0,90),new Vector3(20,0,115),new Vector3(105,0,115),new Vector3(180,0,95),new Vector3(250,0,140),new Vector3(290,0,40),new Vector3(250,0,-40),new Vector3(180,0,-80),new Vector3(130,0,-40),new Vector3(80,0,-80),new Vector3(25,0,-135),new Vector3(-80,0,-140),new Vector3(-170,0,-90),new Vector3(-255,0,-100),new Vector3(-290,0,-35) };
        var points = new List<Vector3>();
        for (int i=0;i<knots.Length;i++) for(int j=0;j<18;j++)
        {
            float t=j/18f;var a=knots[(i+knots.Length-1)%knots.Length];var b=knots[i];var c=knots[(i+1)%knots.Length];var d=knots[(i+2)%knots.Length];
            points.Add(.5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t));
        }
        return points.ToArray();
    }
    static Material Mat(string name,Color color)
    {
        var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};m.color=color;m.SetFloat("_Smoothness",.15f);AssetDatabase.CreateAsset(m,Dir+"/"+name+".mat");return m;
    }
    static GameObject Shape(string name,PrimitiveType shape,Vector3 position,Vector3 scale,Material mat,bool collision=false)
    {
        var go=GameObject.CreatePrimitive(shape);go.name=name;go.transform.SetParent(root,false);go.transform.position=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=mat;
        if(!collision)Object.DestroyImmediate(go.GetComponent<Collider>());go.isStatic=true;return go;
    }
    static GameObject Box(string name,Vector3 p,Vector3 size,Material mat,bool collision=false)=>Shape(name,PrimitiveType.Cube,p,size,mat,collision);
    static GameObject MeshObject(string name,Mesh mesh,Material mat,bool collision=false)
    {
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<Renderer>().sharedMaterial=mat;go.isStatic=true;
        if(collision)go.AddComponent<MeshCollider>().sharedMesh=mesh;return go;
    }
    static Mesh SaveMesh(string name,List<Vector3> vertices,List<int> triangles)
    {
        if(name=="Lakeshore Ground") RefineGroundTriangles(ref vertices,ref triangles);
        var mesh=new Mesh{name=name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Dir+"/"+name+".asset");return mesh;
    }
    static GameObject Ribbon(string name,Vector3[] points,float offset,float width,float height,Material mat,bool collision=false,bool loop=true)
    {
        var v=new List<Vector3>();var t=new List<int>();int count=points.Length+(loop?1:0);
        for(int i=0;i<count;i++)
        {
            int k=i%points.Length;
            // Open streets use endpoint tangents; wrapping flips road and walk ends.
            int previous=loop?(k+points.Length-1)%points.Length:Mathf.Max(0,k-1);
            int next=loop?(k+1)%points.Length:Mathf.Min(points.Length-1,k+1);
            var direction=(points[next]-points[previous]).normalized;var side=Vector3.Cross(Vector3.up,direction);
            v.Add(points[k]+side*(offset-width*.5f)+Vector3.up*height);v.Add(points[k]+side*(offset+width*.5f)+Vector3.up*height);
            if(i>0){int n=i*2;t.AddRange(new[]{n-2,n,n-1,n-1,n,n+1});}
        }
        return MeshObject(name,SaveMesh(name,v,t),mat,collision);
    }
    static void Tree(Vector3 position,float size)
    {
        Shape("Pine trunk",PrimitiveType.Cylinder,position+Vector3.up*size*.2f,new Vector3(size*.10f,size*.20f,size*.10f),trunk);
        var crown=MeshObject("Pine canopy",cone,leaves);crown.transform.position=position+Vector3.up*size*.25f;crown.transform.localScale=new Vector3(size*.55f,size*.85f,size*.55f);
    }
    static void Sign(string text,Vector3 position,float yaw,float width=15)
    {
        var post=Box(text+" post",position+Vector3.up*2,new Vector3(.25f,4,.25f),wood,true);
        var board=Box(text+" board",position+Vector3.up*4,new Vector3(width,2.5f,.25f),dark);board.transform.rotation=Quaternion.Euler(0,yaw,0);
        var go=new GameObject("Sign lettering",typeof(TextMeshPro));go.transform.SetParent(board.transform,false);go.transform.localPosition=new Vector3(0,0,-.55f);go.transform.localRotation=Quaternion.identity;go.transform.localScale=new Vector3(1/width,1/2.5f,4);
        var label=go.GetComponent<TextMeshPro>();label.text=text;label.fontSize=12;label.color=new Color(1,.85f,.45f);label.alignment=TextAlignmentOptions.Center;label.rectTransform.sizeDelta=new Vector2(width-1,2);label.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
    }
    static void Building(Vector3 p,int index,Material wall)
    {
        float w=12+(index%3)*3,d=13+(index%2)*3,h=6+(index%3)*2;
        Box("Town house "+index,p+Vector3.up*h*.5f,new Vector3(w,h,d),wall,true);
        var r=MeshObject("Gabled roof "+index,roofMesh,roof);r.transform.position=p+Vector3.up*h;r.transform.localScale=new Vector3(w+1,3,d+1);
        for(int side=-1;side<=1;side+=2) for(int x=0;x<3;x++) for(int floor=0;floor<(h>7?2:1);floor++)
        {
            // Leave the centered front entrance clear; keep its upstairs window.
            if(side==-1 && x==1 && floor==0)continue;
            Box("Window",p+new Vector3((x-1)*w*.27f,2.6f+floor*3.1f,side*(d*.5f+.03f)),new Vector3(1.9f,1.7f,.08f),glass);
        }
        Box("Front door",p+new Vector3(0,1.3f,-d*.5f-.08f),new Vector3(1.7f,2.6f,.14f),wood);
        Box("House foundation",p+Vector3.up*.15f,new Vector3(w+.5f,.3f,d+.5f),stone);
    }
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play mode first");
        if(AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/LakeTownScene.unity")!=null)throw new Exception("Lake Town already exists; refusing overwrite");
        var original=SceneManager.GetActiveScene();if(original.isDirty)throw new Exception("Save current scene first");
        if(!AssetDatabase.IsValidFolder(Dir))AssetDatabase.CreateFolder("Assets/Environments","LakeTownTrial");
        var template=EditorSceneManager.OpenScene("Assets/Scenes/TestTrackScene.unity");
        EditorSceneManager.SaveScene(template,"Assets/Scenes/LakeTownScene.unity",true);
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/LakeTownScene.unity");
        var infrastructure=scene.GetRootGameObjects().First(g=>g.name.StartsWith("Test Track"));infrastructure.name="Lake Town Driving Infrastructure";
        Object.DestroyImmediate(infrastructure.transform.Find("CircuitGeometry").gameObject);
        foreach(var name in new[]{"CircuitFacilities","Offroad Course","Clean Lap Challenge","Night Roadside Lighting"}){var go=GameObject.Find(name);if(go!=null)Object.DestroyImmediate(go);}
        var manager=Object.FindAnyObjectByType<TestDriveSceneManager>();var managerData=new SerializedObject(manager);managerData.FindProperty("cleanLapChallenge").objectReferenceValue=null;managerData.ApplyModifiedPropertiesWithoutUndo();
        var controls=Object.FindAnyObjectByType<TestDriveTouchControls>();var controlData=new SerializedObject(controls);var panel=controlData.FindProperty("cleanLapPanel").objectReferenceValue as RectTransform;if(panel!=null)panel.gameObject.SetActive(false);controlData.FindProperty("cleanLapPanel").objectReferenceValue=null;controlData.ApplyModifiedPropertiesWithoutUndo();
        root=new GameObject("Lake Town — Trial Course").transform;
        asphalt=Mat("Lake Town Asphalt",new Color(.10f,.13f,.15f));grass=Mat("Meadow",new Color(.36f,.49f,.28f));water=Mat("Lake Teal",new Color(.12f,.48f,.59f));water.SetFloat("_Smoothness",.75f);
        white=Mat("Road Ivory",new Color(.91f,.91f,.83f));gold=Mat("Warm Yellow",new Color(.98f,.76f,.29f));wood=Mat("Timber",new Color(.33f,.22f,.14f));stone=Mat("Promenade Stone",new Color(.64f,.63f,.55f));roof=Mat("Terracotta Roof",new Color(.57f,.22f,.14f));glass=Mat("Window Blue",new Color(.22f,.42f,.50f));trunk=wood;leaves=Mat("Pine Green",new Color(.17f,.36f,.23f));dark=Mat("Sign Navy",new Color(.05f,.13f,.17f));
        var cv=new List<Vector3>();var ct=new List<int>();for(int i=0;i<8;i++){float a=i*Mathf.PI/4,b=(i+1)*Mathf.PI/4;int k=cv.Count;cv.AddRange(new[]{new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)),Vector3.up,new Vector3(Mathf.Cos(b),0,Mathf.Sin(b))});ct.AddRange(new[]{k,k+1,k+2});}cone=SaveMesh("Pine Cone",cv,ct);
        roofMesh=SaveMesh("Gabled Roof",new List<Vector3>{new Vector3(-.5f,0,-.5f),new Vector3(.5f,0,-.5f),new Vector3(0,1,-.5f),new Vector3(-.5f,0,.5f),new Vector3(.5f,0,.5f),new Vector3(0,1,.5f)},new List<int>{0,2,1,3,4,5,0,3,5,0,5,2,2,5,4,2,4,1});
        var landEdge = new[] {new Vector2(-750,200),new Vector2(-430,200),new Vector2(-300,120),new Vector2(-160,100),new Vector2(-60,130),new Vector2(30,155),new Vector2(120,155),new Vector2(205,150),new Vector2(330,215),new Vector2(500,450),new Vector2(750,750)};
        var landVertices=new List<Vector3>();var landTriangles=new List<int>();
        for(int i=0;i<landEdge.Length;i++){landVertices.Add(new Vector3(landEdge[i].x,-.05f,-750));landVertices.Add(new Vector3(landEdge[i].x,-.05f,landEdge[i].y));if(i>0){int k=i*2;landTriangles.AddRange(new[]{k-2,k-1,k,k,k-1,k+1});}}
        MeshObject("Town ground",SaveMesh("Lakeshore Ground",landVertices,landTriangles),grass,true);
        Box("Lake bed",new Vector3(0,-4,350),new Vector3(1100,1,800),grass,true);
        var route=Route();var road=Ribbon("Lake Town Road",route,0,14,.04f,asphalt,true);road.AddComponent<TestDriveAudioSurface>();
        Ribbon("Road left edge",route,-6.5f,.17f,.055f,white);Ribbon("Road right edge",route,6.5f,.17f,.055f,white);
        Ribbon("Promenade pavement",route,-9.5f,4,.01f,stone);Ribbon("Town pavement",route,9.5f,4,.01f,stone);
        var streetMeshes = new List<MeshFilter> { road.GetComponent<MeshFilter>() };
        var streets = new[] { new[]{new Vector3(-90,0,-145),new Vector3(-90,0,91)}, new[]{new Vector3(85,0,-88),new Vector3(85,0,120)}, new[]{new Vector3(-290,0,-5),new Vector3(294,0,-5)} };
        for(int street=0;street<streets.Length;street++)
        {
            var samples=Enumerable.Range(0,60).Select(i=>Vector3.Lerp(streets[street][0],streets[street][1],i/59f)).ToArray();
            string name = street==0 ? "Market Street" : street==1 ? "Marina Avenue" : "Old Town Cross Street";
            var streetRoad=Ribbon(name,samples,0,12,.045f,asphalt,true,false);streetRoad.AddComponent<TestDriveAudioSurface>();streetMeshes.Add(streetRoad.GetComponent<MeshFilter>());
            Ribbon(name+" west walk",samples,-7.5f,3,.01f,stone,false,false);Ribbon(name+" east walk",samples,7.5f,3,.01f,stone,false,false);
        }
        var mapRoad=new Mesh{name="Lake Town Road Network",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mapRoad.CombineMeshes(streetMeshes.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=f.transform.localToWorldMatrix}).ToArray());AssetDatabase.CreateAsset(mapRoad,Dir+"/RoadNetwork.asset");
        float length=0;for(int i=0;i<route.Length;i++){var next=route[(i+1)%route.Length];float segment=Vector3.Distance(route[i],next);length+=segment;if(i%2==0){var mark=Box("Center dash",(route[i]+next)*.5f+Vector3.up*.06f,new Vector3(.16f,.015f,segment*.55f),gold);mark.transform.rotation=Quaternion.LookRotation(next-route[i]);}}
        var shore=new[]{new Vector3(-430,-.22f,200),new Vector3(-300,-.22f,120),new Vector3(-160,-.22f,100),new Vector3(-60,-.22f,130),new Vector3(30,-.22f,155),new Vector3(120,-.22f,155),new Vector3(205,-.22f,150),new Vector3(330,-.22f,215),new Vector3(500,-.22f,450),new Vector3(350,-.22f,650),new Vector3(-450,-.22f,650)};
        var lv=new List<Vector3>{new Vector3(0,-.22f,350)};lv.AddRange(shore);var lt=new List<int>();for(int i=0;i<shore.Length;i++)lt.AddRange(new[]{0,1+(i+1)%shore.Length,1+i});MeshObject("Lake",SaveMesh("Lake Surface",lv,lt),water);
        // Lake bed is below the water; shoreline stones and promenade rails keep the road readable.
        for(int i=0;i<125;i++){float x=-390+(i*47%810),z=220+(i*79%380);Box("Lake glint",new Vector3(x,-.205f,z),new Vector3(4+(i%5)*2,.012f,.16f),white);}
        for(int i=0;i<108;i+=2){var dir=(route[i+1]-route[(i+route.Length-1)%route.Length]).normalized;var side=Vector3.Cross(Vector3.up,dir);var p=route[i]-side*12f;Box("Lakeside railing post",p+Vector3.up*.7f,new Vector3(.18f,1.4f,.18f),wood,true);var bar=Box("Lakeside railing",p+Vector3.up*1.15f,new Vector3(.13f,.13f,7.5f),white);bar.transform.rotation=Quaternion.LookRotation(dir);}
        var colors=new[]{Mat("House Cream",new Color(.87f,.79f,.62f)),Mat("House Sage",new Color(.55f,.68f,.52f)),Mat("House Ochre",new Color(.78f,.52f,.30f)),Mat("House Blue",new Color(.43f,.62f,.66f))};
        int houses=0;for(int z=-90;z<=90;z+=30)for(int x=-220;x<=220;x+=30){var p=new Vector3(x,0,z);if(route.Min(q=>Vector3.Distance(p,q))<24||Mathf.Abs(x+90)<20||Mathf.Abs(x-85)<20||Mathf.Abs(z+5)<20)continue;Building(p,houses,colors[houses%colors.Length]);houses++;}
        var random=new System.Random(734);for(int i=0;i<160;i++){var p=new Vector3(-390+(float)random.NextDouble()*780,0,-220+(float)random.NextDouble()*360);if(route.Min(q=>Vector3.Distance(p,q))<18||Mathf.Abs(p.z+5)<14||Mathf.Abs(p.x+90)<14&&p.z<100||Mathf.Abs(p.x-85)<14&&p.z<130||Mathf.Abs(p.x)<225&&p.z>-85&&p.z<70)continue;Tree(p,7+(float)random.NextDouble()*5);}
        for(int i=0;i<15;i++){var hill=MeshObject("Distant mountain",cone,i%2==0?leaves:grass);hill.transform.position=new Vector3(-650+i*90,-2,640+(i%3)*75);hill.transform.localScale=new Vector3(110,90+(i%4)*45,100);}
        // Pier, waterfront furniture and a small marina give the lake edge landmarks.
        Box("Marina pier",new Vector3(-60,.1f,164),new Vector3(6,.3f,54),wood,true);Box("Pier head",new Vector3(-60,.1f,190),new Vector3(28,.3f,6),wood,true);
        for(int i=0;i<4;i++){var boat=Shape("Moored boat",PrimitiveType.Capsule,new Vector3(-79+i*12,.1f,200+(i%2)*9),new Vector3(3,.8f,7),i%2==0?white:gold);}
        for(int i=0;i<6;i++){float x=-175+i*62;Box("Promenade bench",new Vector3(x,.55f,96+(i%3)*16),new Vector3(3.5f,.22f,.8f),wood);}
        Sign("LAKE TOWN",new Vector3(-240,0,47),90,18);Sign("MARINA  /  LAKE WALK",new Vector3(-85,0,104),180,22);Sign("OLD TOWN",new Vector3(90,0,-22),0,16);Sign("PINE BEND",new Vector3(254,0,40),-90,16);
        for(int i=0;i<route.Length;i+=18){var direction=(route[(i+1)%route.Length]-route[i]).normalized;var p=route[i]+Vector3.Cross(Vector3.up,direction)*10.8f;Box("Street lamp",p+Vector3.up*3.5f,new Vector3(.18f,7,.18f),dark);Box("Lamp lantern",p+Vector3.up*7,new Vector3(.8f,.7f,.8f),gold);var lamp=new GameObject("Warm street light",typeof(Light));lamp.transform.SetParent(root,false);lamp.transform.position=p+Vector3.up*6.5f;var light=lamp.GetComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.81f,.52f);light.range=23;light.intensity=1.3f;light.shadows=LightShadows.None;}
        var spawn=infrastructure.transform.Find("VehicleSpawn");var spawnSide=Vector3.Cross(Vector3.up,(route[6]-route[5]).normalized);spawn.position=route[5]-spawnSide*3.2f+Vector3.up*.35f;spawn.rotation=Quaternion.LookRotation(route[6]-route[5]);
        var camera=infrastructure.GetComponentInChildren<Camera>();camera.farClipPlane=1200;camera.transform.position=spawn.position-spawn.forward*8+Vector3.up*4;camera.transform.LookAt(spawn.position+spawn.forward*5);
        var map=Object.FindAnyObjectByType<TestDriveMinimap>();var mapData=new SerializedObject(map);mapData.FindProperty("roadMesh").objectReferenceValue=mapRoad;mapData.FindProperty("roadTransform").objectReferenceValue=road.transform;mapData.FindProperty("startLineA").vector3Value=route[5]+spawnSide*6;mapData.FindProperty("startLineB").vector3Value=route[5]-spawnSide*6;mapData.ApplyModifiedPropertiesWithoutUndo();
        var mapLabel=map.transform.parent.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t=>t.text=="CIRCUIT");if(mapLabel!=null)mapLabel.text="LAKE TOWN";
        RenderSettings.skybox=AssetDatabase.LoadAssetAtPath<Material>("Assets/Environments/LakesideOverlookTrial/OverlookSky.mat");RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.64f,.75f,.85f);RenderSettings.ambientEquatorColor=new Color(.45f,.53f,.56f);RenderSettings.ambientGroundColor=new Color(.32f,.35f,.28f);RenderSettings.fog=true;RenderSettings.fogColor=new Color(.60f,.75f,.82f);RenderSettings.fogMode=FogMode.Exponential;RenderSettings.fogDensity=.0009f;
        CombineScenery();
        EditorSceneManager.SaveScene(scene);
        var build=EditorBuildSettings.scenes.ToList();build.RemoveAll(s=>s.path==scene.path);build.Add(new EditorBuildSettingsScene(scene.path,true));EditorBuildSettings.scenes=build.ToArray();
        AssetDatabase.SaveAssets();return "Created LakeTownScene: "+length.ToString("F0")+" m loop, "+houses+" houses, marina, lakefront and pine bends; driving HUD and minimap wired.";
    }
    static void RefineGroundTriangles(ref List<Vector3> vertices, ref List<int> triangles)
    {
        var pending=new Queue<Vector3[]>();for(int i=0;i<triangles.Count;i+=3)pending.Enqueue(new[]{vertices[triangles[i]],vertices[triangles[i+1]],vertices[triangles[i+2]]});
        var result=new List<Vector3>();var indices=new List<int>();
        while(pending.Count>0)
        {
            var t=pending.Dequeue();var a=t[0];var b=t[1];var c=t[2];
            if(Mathf.Max(Vector3.Distance(a,b),Vector3.Distance(b,c),Vector3.Distance(c,a))>80f)
            {
                var ab=(a+b)*.5f;var bc=(b+c)*.5f;var ca=(c+a)*.5f;
                pending.Enqueue(new[]{a,ab,ca});pending.Enqueue(new[]{ab,b,bc});pending.Enqueue(new[]{ca,bc,c});pending.Enqueue(new[]{ab,bc,ca});
            }
            else {int k=result.Count;result.AddRange(t);indices.AddRange(new[]{k,k+1,k+2});}
        }
        vertices=result;triangles=indices;
    }
    public static string RefineExistingGround()
    {
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Dir+"/Lakeshore Ground.asset");var v=mesh.vertices.ToList();var t=mesh.triangles.ToList();RefineGroundTriangles(ref v,ref t);mesh.Clear();mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssets();return "Ground tessellated into "+t.Count/3+" triangles with edges no longer than 80m.";
    }
    static void CombineScenery()
    {
        var filters=root.GetComponentsInChildren<MeshFilter>().Where(f=>f.GetComponent<Collider>()==null&&f.GetComponent<TextMeshPro>()==null&&f.GetComponent<Renderer>()!=null&&!f.name.Contains("Road")&&!f.name.Contains("Lake Surface")&&f.name!="Lake"&&f.name!="Marina pier"&&f.name!="Pier head").ToArray();
        int index=0;
        foreach(var group in filters.GroupBy(f=>f.GetComponent<Renderer>().sharedMaterial))
        {
            var mesh=new Mesh{name="Batched scenery "+index,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.CombineMeshes(group.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=f.transform.localToWorldMatrix}).ToArray());AssetDatabase.CreateAsset(mesh,Dir+"/SceneryBatch"+index+".asset");MeshObject("Scenery batch "+index,mesh,group.Key);
            foreach(var filter in group){Object.DestroyImmediate(filter.GetComponent<Renderer>());Object.DestroyImmediate(filter);}index++;
        }
    }
}
