using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;
using Object = UnityEngine.Object;

// One-time scene builder. Never overwrites an existing Mountain Pass.
public static class BuildMountainPass
{
    const string Dir = "Assets/Environments/MountainPass";
    static Transform root;
    static Vector3[] route;
    static Material asphalt, shoulder, grass, rock, pine, bark, white, yellow, metal, dark;
    static Mesh cone;
    static int meshIndex;
    public static Vector3[] Route()
    {
        var k = new[] {
            new Vector3(-180,0,-220),new Vector3(-40,0,-220),new Vector3(120,0,-220),
            new Vector3(220,5,-190),new Vector3(260,12,-100),new Vector3(220,20,-20),
            new Vector3(80,28,15),new Vector3(-100,36,15),new Vector3(-175,40,35),
            new Vector3(-190,43,65),new Vector3(-165,46,90),new Vector3(-90,49,95),
            new Vector3(90,58,95),new Vector3(175,63,120),new Vector3(190,66,150),
            new Vector3(165,69,175),new Vector3(70,73,180),new Vector3(-90,81,180),
            new Vector3(-170,85,205),new Vector3(-190,88,240),new Vector3(-165,90,275),
            new Vector3(-80,90,290),new Vector3(100,90,290),new Vector3(240,84,270),
            new Vector3(330,70,200),new Vector3(370,50,90),new Vector3(380,30,-40),
            new Vector3(340,15,-180),new Vector3(260,3,-300),new Vector3(100,0,-360),
            new Vector3(-80,0,-350),new Vector3(-200,0,-300) };
        var result = new List<Vector3>();
        for(int i=0;i<k.Length;i++)
        {
            var a=k[(i+k.Length-1)%k.Length];var b=k[i];var c=k[(i+1)%k.Length];var d=k[(i+2)%k.Length];
            int count=Mathf.CeilToInt(Vector3.Distance(b,c)/2f);
            for(int j=0;j<count;j++)
            {
                float t=j/(float)count;
                result.Add(.5f*(2*b+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t));
            }
        }
        return result.ToArray();
    }
    static Material Mat(string name,Color tint)
    {
        var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,color=tint};
        m.SetFloat("_Smoothness",.12f);AssetDatabase.CreateAsset(m,Dir+"/"+name+".mat");return m;
    }
    static Mesh Mesh(string name,List<Vector3> v,List<int> t)
    {
        var mesh=new Mesh{name=name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
        mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,Dir+"/"+name+" "+(meshIndex++)+".asset");return mesh;
    }
    static GameObject MeshGo(string name,Mesh mesh,Material mat,bool collision=false)
    {
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);
        go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<Renderer>().sharedMaterial=mat;go.isStatic=true;
        if(collision)go.AddComponent<MeshCollider>().sharedMesh=mesh;return go;
    }
    static GameObject Box(string name,Vector3 p,Vector3 size,Material mat,bool collision=false)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root,false);
        go.transform.position=p;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=mat;
        if(!collision)Object.DestroyImmediate(go.GetComponent<Collider>());go.isStatic=true;return go;
    }
    static Vector3 Side(int i)
    {
        var d=route[(i+1)%route.Length]-route[(i+route.Length-1)%route.Length];d.y=0;
        return Vector3.Cross(Vector3.up,d.normalized);
    }
    static GameObject Ribbon(string name,float offset,float width,float height,Material mat,bool collision=false)
    {
        var v=new List<Vector3>();var t=new List<int>();
        for(int i=0;i<=route.Length;i++)
        {
            int k=i%route.Length;var side=Side(k);
            v.Add(route[k]+side*(offset-width*.5f)+Vector3.up*height);
            v.Add(route[k]+side*(offset+width*.5f)+Vector3.up*height);
            if(i>0){int n=i*2;t.AddRange(new[]{n-2,n,n-1,n-1,n,n+1});}
        }
        return MeshGo(name,Mesh(name,v,t),mat,collision);
    }
    static float Ground(float x,float z)
    {
        float best=float.MaxValue,y=0;
        for(int i=0;i<route.Length;i+=3)
        {
            float d=(new Vector2(x-route[i].x,z-route[i].z)).sqrMagnitude;
            if(d<best){best=d;y=route[i].y;}
        }
        float distance=Mathf.Sqrt(best);
        // Cut below the continuous road embankment; never let coarse terrain pierce asphalt.
        return y-3f-Mathf.Max(0,distance-10)*.28f+Mathf.Sin(x*.018f)*Mathf.Sin(z*.021f)*Mathf.Min(distance*.02f,2);
    }
    static void Terrain()
    {
        var v=new List<Vector3>();var t=new List<int>();
        const int n=100;
        for(int z=0;z<=n;z++)for(int x=0;x<=n;x++)
        {
            float px=-600+x*12,pz=-600+z*12;v.Add(new Vector3(px,Ground(px,pz),pz));
            if(x>0&&z>0){int i=z*(n+1)+x;t.AddRange(new[]{i-n-2,i-1,i-n-1,i-n-1,i-1,i});}
        }
        MeshGo("Alpine terrain",Mesh("Alpine terrain",v,t),grass,true);
        // A continuous sloping bank supports the road and shoulders without exposed gaps.
        v.Clear();t.Clear();
        for(int i=0;i<=route.Length;i++)
        {
            int k=i%route.Length;
            foreach(float offset in new[]{-24f,-9f,9f,24f})
            {
                var p=route[k]+Side(k)*offset;
                p.y=Mathf.Abs(offset)<10?route[k].y-.24f:Ground(p.x,p.z);
                v.Add(p);
            }
            if(i>0)for(int j=0;j<3;j++){int a=(i-1)*4+j,b=i*4+j;t.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
        }
        MeshGo("Road embankment",Mesh("Road embankment",v,t),grass,true);
    }
    static void Rails(float offset)
    {
        var v=new List<Vector3>();var t=new List<int>();
        for(int i=0;i<=route.Length;i++)
        {
            int k=i%route.Length;var p=route[k]+Side(k)*offset;
            v.Add(p+Vector3.up*.55f);v.Add(p+Vector3.up*1.15f);
            if(i>0){int n=i*2;t.AddRange(new[]{n-2,n-1,n,n-1,n+1,n,n,n-1,n-2,n,n+1,n-1});}
            if(i%4==0)Box("Guardrail post",p+Vector3.up*.55f,new Vector3(.18f,1.1f,.18f),metal);
            if(i%12==0)Box("Road reflector",p+Vector3.up*1.22f,new Vector3(.15f,.16f,.15f),white);
        }
        MeshGo(offset<0?"Left safety rail":"Right safety rail",Mesh("Safety rail",v,t),metal,true);
    }
    static void Tree(Vector3 p,float h)
    {
        Box("Pine trunk",p+Vector3.up*h*.24f,new Vector3(.5f,h*.48f,.5f),bark);
        for(int i=0;i<2;i++)
        {
            var go=MeshGo("Alpine pine",cone,pine);go.transform.position=p+Vector3.up*(h*.22f+i*h*.22f);
            go.transform.localScale=new Vector3(h*(.45f-i*.08f),h*.65f,h*(.45f-i*.08f));
        }
    }
    static void Sign(string text,int index,float side=11,float width=9)
    {
        var forward=(route[(index+1)%route.Length]-route[index]).normalized;
        var p=route[index]+Side(index)*side;
        Box("Sign post",p+Vector3.up*1.8f,new Vector3(.18f,3.6f,.18f),metal);
        var board=Box(text+" sign",p+Vector3.up*3.4f,new Vector3(width,1.8f,.18f),dark);
        board.transform.rotation=Quaternion.LookRotation(forward);
        var label=new GameObject("Lettering",typeof(TextMeshPro));label.transform.SetParent(root,false);
        label.transform.position=board.transform.position-forward*.12f;label.transform.rotation=board.transform.rotation;
        var tmp=label.GetComponent<TextMeshPro>();tmp.text=text;tmp.fontSize=6;tmp.color=white.color;tmp.alignment=TextAlignmentOptions.Center;
        tmp.rectTransform.sizeDelta=new Vector2(width-.4f,1.5f);
    }
    static int Nearest(Vector3 p) => Enumerable.Range(0,route.Length).OrderBy(i=>(route[i]-p).sqrMagnitude).First();
    static void Tunnel()
    {
        int start=Nearest(new Vector3(-60,90,290)),end=Nearest(new Vector3(55,90,290));
        var v=new List<Vector3>();var t=new List<int>();const int rings=12;
        for(int i=start;i<=end;i++)
        {
            for(int j=0;j<=rings;j++)
            {
                float angle=j*Mathf.PI/rings;
                v.Add(route[i]+Side(i)*(Mathf.Cos(angle)*8.2f)+Vector3.up*(1+Mathf.Sin(angle)*7.5f));
            }
            if(i>start)for(int j=0;j<rings;j++)
            {
                int b=(i-start)*(rings+1)+j,a=b-rings-1;
                t.AddRange(new[]{a,a+1,b,a+1,b+1,b,b,a+1,a,b,b+1,a+1});
            }
        }
        MeshGo("Summit tunnel vault",Mesh("Tunnel vault",v,t),rock,true);
        for(int i=start;i<=end;i+=10)
        {
            foreach(int side in new[]{-1,1})
            {
                var g=Box("Tunnel footing",route[i]+Side(i)*side*8.2f+Vector3.up*.5f,new Vector3(.7f,1.1f,4.4f),rock,true);
                g.transform.rotation=Quaternion.LookRotation(route[(i+1)%route.Length]-route[i]);
            }
            var lamp=new GameObject("Tunnel light",typeof(Light));lamp.transform.SetParent(root,false);
            lamp.transform.position=route[i]+Vector3.up*6.4f;var l=lamp.GetComponent<Light>();
            l.type=LightType.Point;l.range=20;l.intensity=3;l.color=new Color(1,.82f,.55f);l.shadows=LightShadows.None;
            Box("Tunnel light strip",route[i]+Vector3.up*7,new Vector3(1.8f,.12f,.5f),white);
        }
        foreach(int i in new[]{start,end})
        {
            var p=route[i];var side=Side(i);var forward=(route[(i+1)%route.Length]-p).normalized;
            foreach(int sign in new[]{-1,1})
            {
                var pillar=Box("Tunnel portal pillar",p+side*sign*9.2f+Vector3.up*4,new Vector3(2,8,2),rock,true);
                pillar.transform.rotation=Quaternion.LookRotation(forward);
            }
            var top=Box("Tunnel portal lintel",p+Vector3.up*9,new Vector3(20,2,2),rock,true);top.transform.rotation=Quaternion.LookRotation(forward);
        }
    }
    static void TunnelRidge()
    {
        int start=Nearest(new Vector3(-60,90,290)),end=Nearest(new Vector3(55,90,290));
        var v=new List<Vector3>();var t=new List<int>();const int rings=12;
        for(int i=start;i<=end;i++)
        {
            for(int j=0;j<=rings;j++)
            {
                float angle=j*Mathf.PI/rings;
                v.Add(route[i]+Side(i)*(Mathf.Cos(angle)*34f)+Vector3.up*(-8+Mathf.Sin(angle)*34));
            }
            if(i>start)for(int j=0;j<rings;j++)
            {
                int b=(i-start)*(rings+1)+j,a=b-rings-1;
                t.AddRange(new[]{a,b,a+1,a+1,b,b+1});
            }
        }
        MeshGo("Summit rock ridge",Mesh("Summit rock ridge",v,t),rock,true);
        foreach(int i in new[]{start,end})
        {
            v=new List<Vector3>();t=new List<int>();
            for(int j=0;j<=rings;j++)
            {
                float angle=j*Mathf.PI/rings;
                v.Add(route[i]+Side(i)*(Mathf.Cos(angle)*8.2f)+Vector3.up*(1+Mathf.Sin(angle)*7.5f));
                v.Add(route[i]+Side(i)*(Mathf.Cos(angle)*34f)+Vector3.up*(-8+Mathf.Sin(angle)*34));
                if(j>0){int n=j*2;t.AddRange(i==start?new[]{n-2,n-1,n,n,n-1,n+1}:new[]{n-2,n,n-1,n,n+1,n-1});}
            }
            MeshGo("Rock tunnel mouth",Mesh("Rock tunnel mouth",v,t),rock,true);
        }
    }
    static void FixNormals()
    {
        foreach(string name in new[]{"Left safety rail","Right safety rail"})
        {
            var mesh=GameObject.Find(name).GetComponent<MeshCollider>().sharedMesh;
            var normals=new Vector3[mesh.vertexCount];
            for(int i=0;i<normals.Length/2;i++)normals[i*2]=normals[i*2+1]=Side(i%route.Length)*(name.StartsWith("Left")?-1:1);
            mesh.normals=normals;EditorUtility.SetDirty(mesh);
        }
        var vault=GameObject.Find("Summit tunnel vault").GetComponent<MeshCollider>().sharedMesh;
        var vn=new Vector3[vault.vertexCount];int start=Nearest(new Vector3(-60,90,290));
        for(int i=0;i<vn.Length;i++){int j=i%13;float a=j*Mathf.PI/12;vn[i]=-Side(start+i/13)*Mathf.Cos(a)-Vector3.up*Mathf.Sin(a);}
        vault.normals=vn;EditorUtility.SetDirty(vault);
    }
    public static string Polish()
    {
        if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Stop play and save first.");
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/MountainPassScene.unity");
        root=GameObject.Find("Mountain Pass Course").transform;route=Route();meshIndex=100;
        rock=AssetDatabase.LoadAssetAtPath<Material>(Dir+"/Mountain granite.mat");
        if(GameObject.Find("Summit rock ridge")==null)TunnelRidge();
        FixNormals();EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return "Tunnel rock ridge and rail/vault normals finished.";
    }
    static void BatchScenery()
    {
        var all=root.GetComponentsInChildren<MeshFilter>().Where(f=>f.GetComponent<Collider>()==null&&f.GetComponent<TextMeshPro>()==null&&f.GetComponent<Renderer>()!=null).ToArray();
        foreach(var group in all.GroupBy(f=>f.GetComponent<Renderer>().sharedMaterial))
        {
            var m=new Mesh{name="Mountain scenery",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
            m.CombineMeshes(group.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=f.transform.localToWorldMatrix}).ToArray());
            AssetDatabase.CreateAsset(m,Dir+"/Scenery "+(meshIndex++)+".asset");
            MeshGo("Batched "+group.Key.name,m,group.Key);
            foreach(var f in group)Object.DestroyImmediate(f.gameObject);
        }
    }
    public static string Run()
    {
        if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Scene must be saved and Play Mode stopped.");
        if(AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/MountainPassScene.unity")!=null)throw new Exception("Mountain Pass already exists; refusing overwrite.");
        meshIndex=0;route=Route();
        if(!AssetDatabase.IsValidFolder(Dir))AssetDatabase.CreateFolder("Assets/Environments","MountainPass");
        var template=EditorSceneManager.OpenScene("Assets/Scenes/TestTrackScene.unity");
        EditorSceneManager.SaveScene(template,"Assets/Scenes/MountainPassScene.unity",true);
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/MountainPassScene.unity");
        var infrastructure=scene.GetRootGameObjects().First(g=>g.name.StartsWith("Test Track"));
        infrastructure.name="Mountain Pass Driving Infrastructure";
        var geometry=infrastructure.transform.Find("CircuitGeometry");if(geometry!=null)Object.DestroyImmediate(geometry.gameObject);
        foreach(string name in new[]{"CircuitFacilities","Offroad Course","Clean Lap Challenge","Night Roadside Lighting"})
        {var go=GameObject.Find(name);if(go!=null)Object.DestroyImmediate(go);}
        var manager=Object.FindAnyObjectByType<TestDriveSceneManager>();
        var managerData=new SerializedObject(manager);managerData.FindProperty("cleanLapChallenge").objectReferenceValue=null;managerData.ApplyModifiedPropertiesWithoutUndo();
        var controls=Object.FindAnyObjectByType<TestDriveTouchControls>();var controlsData=new SerializedObject(controls);
        var challenge=controlsData.FindProperty("cleanLapPanel").objectReferenceValue as RectTransform;
        if(challenge!=null)challenge.gameObject.SetActive(false);
        controlsData.FindProperty("cleanLapPanel").objectReferenceValue=null;controlsData.ApplyModifiedPropertiesWithoutUndo();
        root=new GameObject("Mountain Pass Course").transform;
        asphalt=Mat("Alpine asphalt",new Color(.12f,.145f,.16f));
        shoulder=Mat("Gravel shoulders",new Color(.43f,.43f,.36f));
        grass=Mat("Alpine meadow",new Color(.33f,.43f,.27f));
        rock=Mat("Mountain granite",new Color(.39f,.43f,.43f));
        pine=Mat("Alpine pine",new Color(.13f,.29f,.23f));
        bark=Mat("Pine bark",new Color(.30f,.23f,.17f));
        white=Mat("Road ivory",new Color(.91f,.92f,.84f));
        yellow=Mat("Safety gold",new Color(.98f,.75f,.22f));
        metal=Mat("Galvanized rail",new Color(.60f,.67f,.67f));
        dark=Mat("Sign forest",new Color(.035f,.11f,.10f));
        var cv=new List<Vector3>();var ct=new List<int>();
        for(int i=0;i<8;i++){float a=i*Mathf.PI/4,b=(i+1)*Mathf.PI/4;int n=cv.Count;cv.AddRange(new[]{new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)),Vector3.up,new Vector3(Mathf.Cos(b),0,Mathf.Sin(b))});ct.AddRange(new[]{n,n+1,n+2});}
        cone=Mesh("Pine cone",cv,ct);
        Terrain();
        Ribbon("Gravel shoulders",0,18,-.04f,shoulder,true);
        var road=Ribbon("Mountain Pass asphalt",0,12,0,asphalt,true);road.AddComponent<TestDriveAudioSurface>();
        Ribbon("Left edge marking",-5.6f,.16f,.018f,white);
        Ribbon("Right edge marking",5.6f,.16f,.018f,white);
        float distance=0,dash=0;
        for(int i=0;i<route.Length;i++)
        {
            var next=route[(i+1)%route.Length];float length=Vector3.Distance(route[i],next);distance+=length;dash+=length;
            if(dash>=9){dash=0;var g=Box("Center dash",(route[i]+next)*.5f+Vector3.up*.025f,new Vector3(.15f,.018f,Mathf.Min(3,length*1.5f)),yellow);g.transform.rotation=Quaternion.LookRotation(next-route[i]);}
        }
        Rails(-7.4f);Rails(7.4f);Tunnel();TunnelRidge();FixNormals();
        var random=new System.Random(261026);
        for(int i=0;i<650;i++)
        {
            float x=-430+(float)random.NextDouble()*1000,z=-460+(float)random.NextDouble()*1000;
            float d=route.Min(p=>new Vector2(p.x-x,p.z-z).sqrMagnitude);
            if(d<24*24)continue;
            var treePosition=new Vector3(x,Ground(x,z),z);
            float treeHeight=8+(float)random.NextDouble()*8;
            // Include the full crown when clearing the ridge and both portals.
            var treeBounds=new Bounds(treePosition+Vector3.up*(treeHeight*.545f),
                new Vector3(treeHeight*.9f,treeHeight*1.09f,treeHeight*.9f));
            var tunnelClearance=GameObject.Find("Summit rock ridge").GetComponent<Renderer>().bounds;
            tunnelClearance.Expand(new Vector3(8,8,8));
            if(tunnelClearance.Intersects(treeBounds))continue;
            Tree(treePosition,treeHeight);
        }
        for(int i=0;i<90;i++)
        {
            float x=-420+(float)random.NextDouble()*940,z=-440+(float)random.NextDouble()*930;
            if(route.Min(p=>new Vector2(p.x-x,p.z-z).sqrMagnitude)<22*22)continue;
            float size=2+(float)random.NextDouble()*5;
            var go=Box("Granite outcrop",new Vector3(x,Ground(x,z)+size*.3f,z),new Vector3(size,size*.8f,size*.7f),rock);
            go.transform.rotation=Quaternion.Euler(10,random.Next(360),15);
        }
        for(int i=0;i<18;i++)
        {
            float a=i*Mathf.PI*2/18;var p=new Vector3(100+Mathf.Cos(a)*850,-65,Mathf.Sin(a)*850);
            var m=MeshGo("Distant alpine peak",cone,rock);m.transform.position=p;
            m.transform.localScale=new Vector3(220+random.Next(100),220+random.Next(170),220+random.Next(100));
        }
        Sign("MOUNTAIN PASS",20,12,13);
        Sign("HAIRPIN  1",Nearest(new Vector3(-100,36,15)),11,10);
        Sign("HAIRPIN  2",Nearest(new Vector3(90,58,95)),-11,10);
        Sign("HAIRPIN  3",Nearest(new Vector3(-90,81,180)),11,10);
        Sign("SUMMIT TUNNEL",Nearest(new Vector3(-165,90,275)),12,12);
        Sign("DESCENT  /  SLOW",Nearest(new Vector3(240,84,270)),12,13);
        var spawn=infrastructure.transform.Find("VehicleSpawn");
        int spawnIndex=20;spawn.position=route[spawnIndex]-Side(spawnIndex)*2.6f+Vector3.up*.35f;
        var direction=route[spawnIndex+1]-route[spawnIndex];direction.y=0;spawn.rotation=Quaternion.LookRotation(direction);
        for(int i=0;i<12;i++){var p=route[spawnIndex+6]+Side(spawnIndex+6)*(i-5.5f);Box("Start marker",p+Vector3.up*.03f,new Vector3(.8f,.025f,.8f),i%2==0?white:dark);}
        var camera=infrastructure.GetComponentInChildren<Camera>();camera.farClipPlane=1800;
        camera.transform.position=spawn.position-spawn.forward*8+Vector3.up*4;camera.transform.LookAt(spawn.position+spawn.forward*5);
        var map=Object.FindAnyObjectByType<TestDriveMinimap>();var mapData=new SerializedObject(map);
        mapData.FindProperty("roadMesh").objectReferenceValue=road.GetComponent<MeshFilter>().sharedMesh;
        mapData.FindProperty("roadTransform").objectReferenceValue=road.transform;
        mapData.FindProperty("startLineA").vector3Value=route[spawnIndex+6]-Side(spawnIndex+6)*6;
        mapData.FindProperty("startLineB").vector3Value=route[spawnIndex+6]+Side(spawnIndex+6)*6;
        mapData.ApplyModifiedPropertiesWithoutUndo();
        foreach(var label in map.transform.parent.GetComponentsInChildren<TMP_Text>(true))if(label.text=="CIRCUIT")label.text="MOUNTAIN";
        RenderSettings.skybox=AssetDatabase.LoadAssetAtPath<Material>("Assets/Environments/CircuitFacilities/CircuitDaySky.mat");
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.62f,.75f,.84f);RenderSettings.ambientEquatorColor=new Color(.43f,.52f,.53f);RenderSettings.ambientGroundColor=new Color(.27f,.32f,.24f);
        RenderSettings.fog=true;RenderSettings.fogColor=new Color(.61f,.73f,.79f);RenderSettings.fogMode=FogMode.Exponential;RenderSettings.fogDensity=.00085f;
        BatchScenery();EditorSceneManager.SaveScene(scene);
        var scenes=EditorBuildSettings.scenes.ToList();scenes.Add(new EditorBuildSettingsScene(scene.path,true));EditorBuildSettings.scenes=scenes.ToArray();
        AssetDatabase.SaveAssets();
        return "Mountain Pass created: "+distance.ToString("F0")+"m loop, 90m climb, 3 hairpins, summit tunnel, guardrails, terrain, pines and minimap.";
    }
    public static string AddMenu()
    {
        if(SceneManager.GetActiveScene().isDirty)throw new Exception("Save scene first.");
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/ShowcaseScene.unity");
        var menu=Object.FindObjectsByType<FreeRideCourseMenu>(FindObjectsInactive.Include).Single();
        var dialog=menu.transform.Find("Dialog");var template=dialog.Find("LakeTownCourse");
        if(dialog.Find("MountainPassCourse")!=null)throw new Exception("Mountain Pass button already exists.");
        var go=Object.Instantiate(template.gameObject,dialog,false);go.name="MountainPassCourse";
        go.transform.Find("Label").GetComponent<TMP_Text>().text="MOUNTAIN PASS";
        go.transform.Find("Details").GetComponent<TMP_Text>().text="Hairpins, summit tunnel\nand sweeping descents";
        var trial=go.transform.Find("Trial");if(trial!=null)trial.gameObject.SetActive(false);
        var button=go.GetComponent<UnityEngine.UI.Button>();button.onClick=new UnityEngine.UI.Button.ButtonClickedEvent();
        var manager=Object.FindAnyObjectByType<VehicleShowcaseManager>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick,manager.MountainPassRide);
        var data=new SerializedObject(menu);data.FindProperty("mountainPass").objectReferenceValue=button;data.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return "Mountain Pass course selection wired.";
    }
}
