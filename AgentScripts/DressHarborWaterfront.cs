using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using Object=UnityEngine.Object;

// Waterfront-only scenery; existing driving surfaces and gameplay remain intact.
public static class DressHarborWaterfront
{
    const string Dir="Assets/Environments/HarborStreet/Marina";
    static Transform root;
    static Mesh cube,cylinder,ring;
    static Dictionary<Material,List<CombineInstance>> batches;
    static readonly List<Light> lights=new List<Light>();
    static readonly List<Renderer> lenses=new List<Renderer>();
    static Material navy,cream,wood,iron,rubber,rope,orange,glass,warm,red,green;
    static Material Mat(string name,Color color,float emission=0)
    {
        var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,color=color};m.SetFloat("_Smoothness",.22f);
        if(emission>0){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*emission);m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;}
        AssetDatabase.CreateAsset(m,Dir+"/"+name+".mat");return m;
    }
    static Mesh Primitive(PrimitiveType type)
    {
        var g=GameObject.CreatePrimitive(type);var mesh=g.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(g);return mesh;
    }
    static void Shape(Mesh mesh,Vector3 p,Vector3 size,Material mat,Quaternion? rotation=null)
    {
        if(!batches.ContainsKey(mat))batches[mat]=new List<CombineInstance>();
        batches[mat].Add(new CombineInstance{mesh=mesh,transform=Matrix4x4.TRS(p,rotation??Quaternion.identity,size)});
    }
    static void Box(Vector3 p,Vector3 size,Material mat)=>Shape(cube,p,size,mat);
    static void Pole(Vector3 p,float height,float width,Material mat)=>Shape(cylinder,p+Vector3.up*height*.5f,new Vector3(width,height*.5f,width),mat);
    static void Line(Vector3 a,Vector3 b,float width,Material mat)=>Shape(cube,(a+b)*.5f,new Vector3(width,width,Vector3.Distance(a,b)),mat,Quaternion.LookRotation(b-a));
    static Renderer Lens(string name,Vector3 p,Vector3 size,Material mat)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(root,false);g.transform.position=p;g.transform.localScale=size;
        var r=g.GetComponent<Renderer>();r.sharedMaterial=mat;Object.DestroyImmediate(g.GetComponent<Collider>());g.isStatic=true;return r;
    }
    static void Light(Vector3 p,Vector3 target,float strength,float range)
    {
        var g=new GameObject("Marina warm light",typeof(Light));g.transform.SetParent(root,false);g.transform.position=p;g.transform.LookAt(target);
        var l=g.GetComponent<Light>();l.type=LightType.Spot;l.color=new Color(1,.78f,.48f);l.intensity=strength;l.range=range;l.spotAngle=110;l.innerSpotAngle=70;l.shadows=LightShadows.None;l.enabled=false;lights.Add(l);
    }
    static Mesh Ring()
    {
        var v=new List<Vector3>();var t=new List<int>();const int around=24,tube=8;
        for(int i=0;i<=around;i++)for(int j=0;j<=tube;j++)
        {
            float a=i*Mathf.PI*2/around,b=j*Mathf.PI*2/tube,r=.27f+Mathf.Cos(b)*.065f;
            v.Add(new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,Mathf.Sin(b)*.065f));
            if(i>0&&j>0){int n=i*(tube+1)+j;t.AddRange(new[]{n-tube-2,n-1,n-tube-1,n-tube-1,n-1,n});}
        }
        var m=new Mesh{name="Marina lifebuoy"};m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();AssetDatabase.CreateAsset(m,Dir+"/Lifebuoy.asset");return m;
    }
    static void NightController(GameObject host,Light[] lamps,Renderer[] emitters,Material night)
    {
        var c=host.AddComponent<LakeTownNightLights>();var so=new SerializedObject(c);
        var array=so.FindProperty("streetlights");array.arraySize=lamps.Length;for(int i=0;i<lamps.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=lamps[i];
        array=so.FindProperty("lanterns");array.arraySize=emitters.Length;for(int i=0;i<emitters.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=emitters[i];
        so.FindProperty("windows").arraySize=0;so.FindProperty("nightLanterns").objectReferenceValue=night;
        so.FindProperty("fullLightDistance").floatValue=100;so.FindProperty("fadeOutDistance").floatValue=160;so.ApplyModifiedPropertiesWithoutUndo();
    }
    static int MoorYachts()
    {
        int moved=0;const string parent="Assets/Environments/HarborStreet/";
        var meshes=new[]{"Barrier ivory scenery.asset","Harbor navy scenery.asset","Blue windows scenery.asset","Ironwork scenery.asset"}
            .Select(n=>AssetDatabase.LoadAssetAtPath<Mesh>(parent+n)).ToList();
        meshes.AddRange(AssetDatabase.LoadAllAssetsAtPath(parent+"City window pattern.asset").OfType<Mesh>());
        var updates=new Dictionary<Mesh,Vector3[]>();
        foreach(var mesh in meshes)
        {
            var v=mesh.vertices;int touched=0;
            for(int i=0;i<v.Length;i+=24)
            {
                var b=new Bounds(v[i],Vector3.zero);for(int j=1;j<24;j++)b.Encapsulate(v[i+j]);
                if(b.center.z> -200||b.center.z< -260||b.center.y>4)continue;
                bool matched=false;
                for(int pier=0;pier<4&&!matched;pier++)foreach(int side in new[]{-1,1})
                {
                    float oldX=-135+100*pier+side*13,newX=-135+100*pier+side*6;
                    if(Mathf.Abs(b.center.x-oldX)<.02f)
                    {for(int j=0;j<24;j++)v[i+j].x-=side*7;touched++;}
                    else if(Mathf.Abs(b.center.x-newX)>.02f)continue;
                    moved++;matched=true;break;
                }
            }
            if(touched>0)updates.Add(mesh,v);
        }
        if(moved!=144)throw new Exception("Unexpected yacht piece count: "+moved+"; no source meshes saved.");
        foreach(var update in updates)
        {
            update.Key.vertices=update.Value;update.Key.RecalculateBounds();EditorUtility.SetDirty(update.Key);AssetDatabase.SaveAssetIfDirty(update.Key);
        }
        return moved;
    }
    static void Buoy(Vector3 p,Material paint,string name)
    {
        Pole(p-Vector3.up*.4f,1.25f,.7f,paint);Pole(p+Vector3.up*.8f,.7f,.12f,iron);
        var emitter=Lens(name+" navigation lens",p+Vector3.up*1.55f,new Vector3(.18f,.18f,.18f),paint);
        var glow=Mat(name+" navigation glow",paint.color,2.8f);NightController(emitter.gameObject,Array.Empty<Light>(),new[]{emitter},glow);
    }
    static void Build()
    {
        const float quay=.05f,deck=-.025f;
        // Quay edge railing with boarding gaps at each finger pier.
        var railPosts=new HashSet<float>();
        for(float x=-235;x<255;x+=5)
        {
            if(Enumerable.Range(0,4).Any(i=>Mathf.Abs((x+2.5f)-(-135+i*100))<5))continue;
            foreach(float postX in new[]{x,x+5})if(railPosts.Add(postX))Pole(new Vector3(postX,quay,-197),1.05f,.10f,iron);
            foreach(float y in new[]{.5f,1f})Line(new Vector3(x,quay+y,-197),new Vector3(x+5,quay+y,-197),.055f,iron);
        }
        // Compact shielded quay lanterns cast warm light toward the berths.
        for(int i=0;i<8;i++)
        {
            var foot=new Vector3(-185+i*52,quay,-194);
            Box(foot+Vector3.up*.12f,new Vector3(.65f,.24f,.65f),cream);Pole(foot,5.2f,.15f,navy);
            Box(foot+Vector3.up*5.25f,new Vector3(.65f,.12f,.65f),navy);
            var head=foot+Vector3.up*4.95f;lenses.Add(Lens("Quay lantern glass",head,new Vector3(.35f,.43f,.35f),glass));
            Light(head-Vector3.up*.25f,new Vector3(foot.x,-.025f,-200),180,42);
        }
        for(int pier=0;pier<4;pier++)
        {
            float x=-135+pier*100;
            // Bollard lights stand directly on the existing timber pier.
            for(int n=0;n<3;n++)
            {
                var foot=new Vector3(x-.95f,deck,-204-n*25);
                Box(foot+Vector3.up*.08f,new Vector3(.4f,.16f,.4f),cream);Pole(foot,.95f,.18f,navy);
                var head=foot+Vector3.up*.88f;lenses.Add(Lens("Pier bollard glass",head,new Vector3(.23f,.16f,.23f),glass));
                Box(head+Vector3.up*.13f,new Vector3(.32f,.1f,.32f),navy);Light(head,new Vector3(x,deck,foot.z),12,13);
            }
            for(int boat=0;boat<3;boat++)foreach(int side in new[]{-1,1})
            {
                float z=-213-boat*18;
                foreach(int end in new[]{-1,1})
                {
                    var cleat=new Vector3(x+side*1.55f,deck,z+end*5);
                    Pole(cleat,.26f,.13f,iron);Box(cleat+Vector3.up*.27f,new Vector3(.48f,.08f,.12f),iron);
                    var start=cleat+Vector3.up*.29f;var finish=new Vector3(x+side*2.55f,.4f,z+end*5.4f);
                    var mid=(start+finish)*.5f-Vector3.up*.12f;Line(start,mid,.035f,rope);Line(mid,finish,.035f,rope);
                }
                Shape(cylinder,new Vector3(x+side*2.1f,-.35f,z),new Vector3(.38f,.36f,.38f),rubber);
            }
            var post=new Vector3(x+1.2f,deck,-259);Pole(post,1.55f,.1f,cream);
            Shape(ring,post+Vector3.up*1.05f-Vector3.forward*.08f,Vector3.one,orange);
            Box(new Vector3(x,deck+.015f,-260.5f),new Vector3(3.6f,.03f,.22f),cream);
        }
        // Benches face the water and remain clear of the pier entrances and palms.
        foreach(float x in new[]{-210f,-85f,15f,115f,220f})
        {
            var p=new Vector3(x,quay,-192);
            Box(p+Vector3.up*.5f,new Vector3(2.2f,.15f,.65f),wood);
            Box(p+new Vector3(0,.92f,.28f),new Vector3(2.2f,.7f,.12f),wood);
            foreach(float side in new[]{-.8f,.8f})Box(p+new Vector3(side,.24f,0),new Vector3(.12f,.48f,.5f),iron);
        }
        // A small harbor office anchors the end of the promenade.
        var office=new Vector3(245,quay,-192);
        Box(office+Vector3.up*1.6f,new Vector3(7,3.2f,4),cream);Box(office+Vector3.up*3.3f,new Vector3(7.5f,.3f,4.5f),navy);
        Box(office+new Vector3(-1.5f,1.55f,2.03f),new Vector3(2.6f,1.2f,.08f),glass);
        Box(office+new Vector3(1.9f,1.15f,2.03f),new Vector3(1,2.3f,.08f),navy);
        var label=new GameObject("Harbor office sign",typeof(TextMeshPro));label.transform.SetParent(root,false);
        label.transform.position=office+new Vector3(0,2.8f,2.08f);label.transform.rotation=Quaternion.LookRotation(Vector3.back);
        var text=label.GetComponent<TextMeshPro>();text.text="HARBOR MARINA";text.fontSize=3.5f;text.alignment=TextAlignmentOptions.Center;text.color=navy.color;text.rectTransform.sizeDelta=new Vector2(6.4f,.7f);
        Buoy(new Vector3(-225,-.9f,-282),red,"Port");Buoy(new Vector3(255,-.9f,-282),green,"Starboard");
    }
    static Mesh Boxes(Mesh source,List<int> selected,string name)
    {
        var old=source.vertices;var normal=source.normals;var uv=source.uv;var tangents=source.tangents;var indices=source.triangles;
        var v=new List<Vector3>();var n=new List<Vector3>();var u=new List<Vector2>();var tan=new List<Vector4>();var t=new List<int>();
        foreach(int box in selected)
        {
            int start=v.Count,offset=box*24;
            for(int j=0;j<24;j++){v.Add(old[offset+j]);if(normal.Length==old.Length)n.Add(normal[offset+j]);if(uv.Length==old.Length)u.Add(uv[offset+j]);if(tangents.Length==old.Length)tan.Add(tangents[offset+j]);}
            for(int j=0;j<36;j++)t.Add(start+indices[box*36+j]-offset);
        }
        var m=new Mesh{name=name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};m.SetVertices(v);m.SetTriangles(t,0);if(n.Count>0)m.SetNormals(n);if(u.Count>0)m.SetUVs(0,u);if(tan.Count>0)m.SetTangents(tan);m.RecalculateBounds();
        AssetDatabase.CreateAsset(m,Dir+"/Local lighting meshes/"+name+".asset");return m;
    }
    static void LocalizeMarinaMeshes()
    {
        if(!AssetDatabase.IsValidFolder(Dir+"/Local lighting meshes"))AssetDatabase.CreateFolder(Dir,"Local lighting meshes");
        var group=new GameObject("Local marina lighting groups").transform;group.SetParent(root,false);
        foreach(string name in new[]{"Marina decking","Barrier ivory","Harbor navy"})
        {
            var original=GameObject.Find("Batched "+name);var filter=original.GetComponent<MeshFilter>();var source=filter.sharedMesh;var vertices=source.vertices;
            if(vertices.Length%24!=0||source.triangles.Length!=vertices.Length/24*36)throw new Exception("Unexpected box batching: "+name);
            var kept=new List<int>();var groups=new Dictionary<int,List<int>>();
            for(int box=0;box<vertices.Length/24;box++)
            {
                var b=new Bounds(vertices[box*24],Vector3.zero);for(int j=1;j<24;j++)b.Encapsulate(vertices[box*24+j]);int id=-1;
                if(name=="Marina decking")id=Mathf.RoundToInt((b.center.x+135)/100);
                else if(b.center.z< -200&&b.center.z> -260&&b.center.y<4)
                {
                    for(int pier=0;pier<4;pier++)foreach(int side in new[]{-1,1})
                        if(Mathf.Abs(b.center.x-(-135+100*pier+side*6))<.02f)id=pier*6+(side>0?3:0)+Mathf.RoundToInt((-213-b.center.z)/18);
                }
                if(id<0)kept.Add(box);else{if(!groups.ContainsKey(id))groups[id]=new List<int>();groups[id].Add(box);}
            }
            foreach(var entry in groups)
            {
                var mesh=Boxes(source,entry.Value,name+" local "+entry.Key);
                var g=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(group,false);
                g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<Renderer>().sharedMaterial=original.GetComponent<Renderer>().sharedMaterial;
                // Keep each berth's light list independent of the large city batches.
                g.isStatic=false;
            }
            if(kept.Count>0)filter.sharedMesh=Boxes(source,kept,name+" city remainder");else original.SetActive(false);
        }
    }
    static void PierMasts()
    {
        var poles=new List<CombineInstance>();var caps=new List<CombineInstance>();
        var poleMesh=Primitive(PrimitiveType.Cylinder);var box=Primitive(PrimitiveType.Cube);
        var controller=root.GetComponent<LakeTownNightLights>();var data=new SerializedObject(controller);
        var lamps=data.FindProperty("streetlights");var emitters=data.FindProperty("lanterns");int start=lamps.arraySize;
        lamps.arraySize=start+4;emitters.arraySize=emitters.arraySize+4;int lensStart=emitters.arraySize-4;
        for(int i=0;i<4;i++)
        {
            var foot=new Vector3(-135+i*100+.95f,-.025f,-233);
            poles.Add(new CombineInstance{mesh=poleMesh,transform=Matrix4x4.TRS(foot+Vector3.up*2.15f,Quaternion.identity,new Vector3(.13f,2.15f,.13f))});
            caps.Add(new CombineInstance{mesh=box,transform=Matrix4x4.TRS(foot+Vector3.up*4.36f,Quaternion.identity,new Vector3(.55f,.12f,.55f))});
            var lens=Lens("Pier mast lantern",foot+Vector3.up*4.15f,new Vector3(.3f,.3f,.3f),AssetDatabase.LoadAssetAtPath<Material>(Dir+"/Lantern glass.mat"));
            var g=new GameObject("Pier mast light",typeof(Light));g.transform.SetParent(root,false);g.transform.position=foot+Vector3.up*3.95f;g.transform.rotation=Quaternion.Euler(90,0,0);
            var light=g.GetComponent<Light>();light.type=LightType.Spot;light.intensity=130;light.range=32;light.spotAngle=160;light.innerSpotAngle=135;light.color=new Color(1,.78f,.48f);light.shadows=LightShadows.None;light.enabled=false;
            lamps.GetArrayElementAtIndex(start+i).objectReferenceValue=light;emitters.GetArrayElementAtIndex(lensStart+i).objectReferenceValue=lens;
        }
        data.ApplyModifiedPropertiesWithoutUndo();
        poles.AddRange(caps);var meshAll=new Mesh{name="Pier lantern masts",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};meshAll.CombineMeshes(poles.ToArray());AssetDatabase.CreateAsset(meshAll,Dir+"/Pier lantern masts.asset");
        var item=new GameObject("Pier lantern masts",typeof(MeshFilter),typeof(MeshRenderer));item.transform.SetParent(root,false);item.GetComponent<MeshFilter>().sharedMesh=meshAll;item.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Dir+"/Marina navy.mat");
    }
    public static string LocalLighting()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(EditorApplication.isPlaying||scene.isDirty||scene.name!="HarborStreetScene")throw new Exception("Use saved Harbor Street outside Play Mode.");
        root=GameObject.Find("Harbor waterfront details").transform;
        if(root.Find("Local marina lighting groups"))throw new Exception("Local lighting groups already exist.");
        LocalizeMarinaMeshes();PierMasts();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return "Docks and yacht hulls use local light groups; 4 tall pier lanterns added, 24 marina lights total.";
    }
    static void ConfigureOfficeWindow()
    {
        var so=new SerializedObject(root.GetComponent<LakeTownNightLights>());
        var windows=so.FindProperty("windows");windows.arraySize=1;
        windows.GetArrayElementAtIndex(0).objectReferenceValue=root.Find("Lantern glass waterfront").GetComponent<Renderer>();
        so.FindProperty("nightWindows").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Material>(Dir+"/Warm marina glow.mat");so.ApplyModifiedPropertiesWithoutUndo();
    }
    public static string TuneLighting()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(EditorApplication.isPlaying||scene.isDirty||scene.name!="HarborStreetScene")throw new Exception("Use saved Harbor Street outside Play Mode.");
        var group=GameObject.Find("Harbor waterfront details");
        foreach(var lamp in group.GetComponentsInChildren<Light>())
        {
            var p=lamp.transform.position;
            if(lamp.name=="Pier mast light")
            {lamp.transform.rotation=Quaternion.Euler(90,0,0);lamp.intensity=130;lamp.range=32;lamp.spotAngle=160;lamp.innerSpotAngle=135;}
            else if(p.y>3){lamp.transform.LookAt(new Vector3(p.x,-.025f,-200));lamp.intensity=180;lamp.range=42;}
            else{lamp.transform.LookAt(new Vector3(p.x+.95f,-.025f,p.z));lamp.intensity=12;lamp.range=13;}
        }
        foreach(var controller in group.GetComponentsInChildren<LakeTownNightLights>())
        {
            var so=new SerializedObject(controller);so.FindProperty("fullLightDistance").floatValue=100;so.FindProperty("fadeOutDistance").floatValue=160;so.ApplyModifiedPropertiesWithoutUndo();
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return "Marina lights illuminate the quay and piers, with 100m full brightness / 160m fade distance.";
    }
    public static string Run()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(EditorApplication.isPlaying||scene.isDirty||scene.name!="HarborStreetScene")throw new Exception("Use saved Harbor Street outside Play Mode.");
        if(GameObject.Find("Harbor waterfront details"))throw new Exception("Waterfront dressing already exists.");
        if(AssetDatabase.IsValidFolder(Dir))throw new Exception("Marina assets already exist; refusing overwrite.");
        AssetDatabase.CreateFolder("Assets/Environments/HarborStreet","Marina");
        root=new GameObject("Harbor waterfront details").transform;cube=Primitive(PrimitiveType.Cube);cylinder=Primitive(PrimitiveType.Cylinder);ring=Ring();
        batches=new Dictionary<Material,List<CombineInstance>>();lights.Clear();lenses.Clear();
        navy=Mat("Marina navy",new Color(.05f,.13f,.17f));cream=Mat("Marina stone",new Color(.76f,.75f,.65f));wood=Mat("Bench timber",new Color(.42f,.28f,.16f));
        iron=Mat("Marine iron",new Color(.24f,.29f,.30f));rubber=Mat("Dock fenders",new Color(.045f,.05f,.055f));rope=Mat("Mooring rope",new Color(.70f,.65f,.47f));
        orange=Mat("Safety orange",new Color(.95f,.28f,.055f));glass=Mat("Lantern glass",new Color(.92f,.86f,.65f));warm=Mat("Warm marina glow",new Color(1,.70f,.32f),2.6f);
        red=Mat("Port red",new Color(.8f,.075f,.04f));green=Mat("Starboard green",new Color(.07f,.65f,.28f));
        Build();int moved=MoorYachts();
        foreach(var pair in batches)
        {
            var mesh=new Mesh{name=pair.Key.name+" waterfront",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.CombineMeshes(pair.Value.ToArray());AssetDatabase.CreateAsset(mesh,Dir+"/"+mesh.name+".asset");
            var go=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.isStatic=true;
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<Renderer>().sharedMaterial=pair.Key;
        }
        NightController(root.gameObject,lights.ToArray(),lenses.ToArray(),warm);
        ConfigureOfficeWindow();LocalizeMarinaMeshes();PierMasts();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        foreach(string guid in AssetDatabase.FindAssets("",new[]{Dir}))AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid)));
        return "Marina dressed: 8 quay lanterns, 12 pier bollards + 4 pier lanterns, mooring gear, 4 lifebuoys, 5 benches, harbor office and navigation buoys; "+moved+" yacht mesh pieces moored beside piers.";
    }
}
