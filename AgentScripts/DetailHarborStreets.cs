using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

// Modest static street dressing, with a road-distance envelope for every prop.
public static class DetailHarborStreets
{
    const string Dir="Assets/Environments/HarborStreet/StreetDetails";
    static Dictionary<Material,List<CombineInstance>> batches;
    static Mesh cube;static Transform root;
    static Material stone,metal,wood,leaf,glass,ivory,red,navy;
    static Vector3[] road;
    static List<Bounds> buildings;
    static List<Vector3> furniture=new List<Vector3>();
    static Material Mat(string name,Color color){var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,color=color};m.SetFloat("_Smoothness",.18f);AssetDatabase.CreateAsset(m,Dir+"/"+name+".mat");return m;}
    static void Box(Vector3 p,Vector3 size,Material mat,Quaternion rotation=default){if(rotation==default)rotation=Quaternion.identity;if(!batches.ContainsKey(mat))batches[mat]=new List<CombineInstance>();batches[mat].Add(new CombineInstance{mesh=cube,transform=Matrix4x4.TRS(p,rotation,size)});}
    static float Distance(Vector3 p)=>road.Min(q=>new Vector2(p.x-q.x,p.z-q.z).magnitude);
    static bool Clear(Vector3 p,float radius){return Distance(p)>9+radius&&!buildings.Any(b=>p.x>b.min.x-radius&&p.x<b.max.x+radius&&p.z>b.min.z-radius&&p.z<b.max.z+radius);}
    static List<Bounds> Boxes(MeshFilter filter){var v=filter.sharedMesh.vertices;var result=new List<Bounds>();if(v.Length%24!=0)throw new Exception("Unexpected facade topology");for(int i=0;i<v.Length;i+=24){var b=new Bounds(filter.transform.TransformPoint(v[i]),Vector3.zero);for(int j=i+1;j<i+24;j++)b.Encapsulate(filter.transform.TransformPoint(v[j]));result.Add(b);}return result;}
    static void Planter(Vector3 p,Quaternion r){Box(p+Vector3.up*.38f,new Vector3(1.9f,.76f,1),stone,r);Box(p+Vector3.up*.78f,new Vector3(1.65f,.08f,.78f),wood,r);for(int i=-1;i<=1;i++)Box(p+r*new Vector3(i*.5f,1.03f,0),new Vector3(.65f,.6f,.72f),leaf,r*Quaternion.Euler(0,i*14,0));}
    static void Bench(Vector3 p,Quaternion r){for(int i=-1;i<=1;i++)Box(p+r*new Vector3(0,.55f,i*.17f),new Vector3(2.3f,.12f,.13f),wood,r);for(int i=0;i<3;i++)Box(p+r*new Vector3(0,.85f+i*.15f,.34f),new Vector3(2.3f,.1f,.1f),wood,r);foreach(float x in new[]{-.83f,.83f}){Box(p+r*new Vector3(x,.27f,0),new Vector3(.12f,.54f,.62f),metal,r);Box(p+r*new Vector3(x,.8f,.34f),new Vector3(.1f,1.05f,.1f),metal,r);}}
    public static string Run(){var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.name!="HarborStreetScene"||scene.isDirty||EditorApplication.isPlaying)throw new Exception("Expected saved HarborStreetScene in Edit Mode");if(GameObject.Find("Harbor street details"))throw new Exception("Already dressed");
        var rv=GameObject.Find("Harbor Street asphalt").GetComponent<MeshFilter>().sharedMesh.vertices;road=Enumerable.Range(0,rv.Length/2-1).Select(i=>(rv[2*i]+rv[2*i+1])*.5f).ToArray();
        var facadeFilters=Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include).Where(f=>f.name.EndsWith("facades")).ToArray();buildings=facadeFilters.SelectMany(Boxes).ToList();
        int originalColliders=Object.FindObjectsByType<Collider>(FindObjectsInactive.Include).Length;
        if(!AssetDatabase.IsValidFolder(Dir))AssetDatabase.CreateFolder("Assets/Environments/HarborStreet","StreetDetails");root=new GameObject("Harbor street details").transform;Undo.RegisterCreatedObjectUndo(root.gameObject,"Dress harbor streets");batches=new Dictionary<Material,List<CombineInstance>>();var primitive=GameObject.CreatePrimitive(PrimitiveType.Cube);cube=primitive.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(primitive);
        stone=Mat("Street pale stone",new Color(.56f,.55f,.48f));metal=Mat("Street dark iron",new Color(.13f,.19f,.2f));wood=Mat("Street warm wood",new Color(.36f,.24f,.15f));leaf=Mat("Street clipped greenery",new Color(.25f,.37f,.24f));glass=Mat("Distant blue window",new Color(.23f,.34f,.38f));ivory=Mat("Shop ivory trim",new Color(.83f,.8f,.69f));red=Mat("Shop muted brick",new Color(.47f,.21f,.17f));navy=Mat("Shop blue trim",new Color(.17f,.27f,.32f));
        int frontages=0,skyline=0,planters=0,benches=0;var frontagePoints=new List<Vector3>();
        foreach(var filter in facadeFilters.Where(f=>f.name.Contains("Blush")||f.name.Contains("Pearl")||f.name.Contains("Ochre")||f.name.Contains("Warm cream"))){int selected=0;foreach(var b in Boxes(filter).OrderBy(b=>b.center.z).ThenBy(b=>b.center.x)){
            if(b.center.z>250){ // These distant silhouettes originally have no windows.
                var face=b.min.z-.055f;int rows=Mathf.Max(2,Mathf.FloorToInt(b.size.y/5));
                for(int row=0;row<rows;row++)for(int col=0;col<5;col++)Box(new Vector3(b.center.x+(col-2)*4.6f,b.min.y+3+row*(b.size.y-5)/rows,face),new Vector3(1.55f,1.9f,.12f),glass);
                Box(new Vector3(b.center.x,b.max.y+.25f,b.center.z),new Vector3(b.size.x+.7f,.5f,b.size.z+.7f),ivory);
                Box(new Vector3(b.center.x,b.min.y+1.1f,face-.02f),new Vector3(b.size.x,.45f,.15f),stone);skyline++;continue;
            }
            if(selected>=4||b.min.y>.2f)continue;
            var p=new Vector3(b.center.x,b.min.y,b.min.z-1.9f);if(Distance(p)<11)continue;selected++;frontages++;frontagePoints.Add(p);
            // Small framed shop fascia stays above the existing ground-floor windows.
            var accent=filter.name.Contains("Blush")?navy:filter.name.Contains("Pearl")?red:wood;
            Box(p+new Vector3(0,3.75f,1.23f),new Vector3(b.size.x*.5f,.52f,.13f),ivory);
            Box(p+new Vector3(0,3.75f,1.14f),new Vector3(b.size.x*.5f-.2f,.34f,.08f),accent);
            // Door canopy and brass-like handle beside the existing entrance.
            Box(new Vector3(b.center.x+b.size.x*.35f,b.min.y+2.65f,b.min.z-.45f),new Vector3(1.65f,.13f,.9f),accent);
            Box(new Vector3(b.center.x+b.size.x*.35f+.32f,b.min.y+1.2f,b.min.z-.21f),new Vector3(.06f,.35f,.08f),ivory);
            foreach(int sign in new[]{-1,1}){var pot=p+Vector3.right*sign*(b.size.x*.38f);if(Clear(pot,1.15f)){Planter(pot,Quaternion.identity);planters++;furniture.Add(pot);}}
        }}
        // Small seating pockets follow the low circuit perimeter, away from the marina quay.
        for(int i=0;i<road.Length&&benches<10;i+=32){var p=road[i];if(p.y>.8f||p.z< -170||p.z>220)continue;var forward=road[(i+1)%road.Length]-road[(i+road.Length-1)%road.Length];forward.y=0;forward.Normalize();var side=Vector3.Cross(Vector3.up,forward);
            foreach(int sign in new[]{-1,1}){var foot=p+side*sign*13.5f;foot.y=-.4f;if(!Clear(foot,2.8f)||furniture.Any(q=>Vector3.Distance(q,foot)<15))continue;
                // Only use the existing flat urban platform, whose top is y=-.4.
                if(foot.x< -280||foot.x>275||foot.z< -185||foot.z>242)continue;
                var r=Quaternion.LookRotation(side*sign);Bench(foot,r);var pot=foot+forward*3.3f;if(Clear(pot,1.2f)){Planter(pot,r);planters++;}furniture.Add(foot);benches++;break;}}
        foreach(var pair in batches){var mesh=new Mesh{name=pair.Key.name+" street details",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.CombineMeshes(pair.Value.ToArray());AssetDatabase.CreateAsset(mesh,Dir+"/"+mesh.name+".asset");var go=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=pair.Key;go.isStatic=true;}
        if(Object.FindObjectsByType<Collider>(FindObjectsInactive.Include).Length!=originalColliders)throw new Exception("Colliders changed");AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return frontages+" selected shopfronts across Blush/Pearl/Ochre/Warm cream; "+skyline+" background blocks detailed; "+planters+" planters; "+benches+" benches. All freestanding props checked against road and building envelopes. No new lights or colliders.";
    }
    public static string AddWarmCream()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.name!="HarborStreetScene"||scene.isDirty||EditorApplication.isPlaying)throw new Exception("Expected saved HarborStreetScene outside Play Mode");
        root=GameObject.Find("Harbor street details").transform;
        if(root.Find("Warm cream details applied"))throw new Exception("Warm cream already detailed");
        var rv=GameObject.Find("Harbor Street asphalt").GetComponent<MeshFilter>().sharedMesh.vertices;
        road=Enumerable.Range(0,rv.Length/2-1).Select(i=>(rv[2*i]+rv[2*i+1])*.5f).ToArray();
        var facadeFilters=Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include).Where(f=>f.name.EndsWith("facades")).ToArray();buildings=facadeFilters.SelectMany(Boxes).ToList();
        batches=new Dictionary<Material,List<CombineInstance>>();furniture=new List<Vector3>();
        var temp=GameObject.CreatePrimitive(PrimitiveType.Cube);cube=temp.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(temp);
        stone=AssetDatabase.LoadAssetAtPath<Material>(Dir+"/Street pale stone.mat");wood=AssetDatabase.LoadAssetAtPath<Material>(Dir+"/Street warm wood.mat");leaf=AssetDatabase.LoadAssetAtPath<Material>(Dir+"/Street clipped greenery.mat");glass=AssetDatabase.LoadAssetAtPath<Material>(Dir+"/Distant blue window.mat");ivory=AssetDatabase.LoadAssetAtPath<Material>(Dir+"/Shop ivory trim.mat");red=AssetDatabase.LoadAssetAtPath<Material>(Dir+"/Shop muted brick.mat");navy=AssetDatabase.LoadAssetAtPath<Material>(Dir+"/Shop blue trim.mat");
        int frontages=0,skyline=0,planters=0;var frontagePoints=new List<Vector3>();
        foreach(var filter in facadeFilters.Where(f=>f.name.Contains("Warm cream"))){int selected=0;foreach(var b in Boxes(filter).OrderBy(b=>b.center.z).ThenBy(b=>b.center.x)){
            if(b.center.z>250){ // These distant silhouettes originally have no windows.
                var face=b.min.z-.055f;int rows=Mathf.Max(2,Mathf.FloorToInt(b.size.y/5));
                for(int row=0;row<rows;row++)for(int col=0;col<5;col++)Box(new Vector3(b.center.x+(col-2)*4.6f,b.min.y+3+row*(b.size.y-5)/rows,face),new Vector3(1.55f,1.9f,.12f),glass);
                Box(new Vector3(b.center.x,b.max.y+.25f,b.center.z),new Vector3(b.size.x+.7f,.5f,b.size.z+.7f),ivory);
                Box(new Vector3(b.center.x,b.min.y+1.1f,face-.02f),new Vector3(b.size.x,.45f,.15f),stone);skyline++;continue;
            }
            if(selected>=4||b.min.y>.2f)continue;
            var p=new Vector3(b.center.x,b.min.y,b.min.z-1.9f);if(Distance(p)<11)continue;selected++;frontages++;frontagePoints.Add(p);
            // Small framed shop fascia stays above the existing ground-floor windows.
            var accent=filter.name.Contains("Blush")?navy:filter.name.Contains("Pearl")?red:wood;
            Box(p+new Vector3(0,3.75f,1.23f),new Vector3(b.size.x*.5f,.52f,.13f),ivory);
            Box(p+new Vector3(0,3.75f,1.14f),new Vector3(b.size.x*.5f-.2f,.34f,.08f),accent);
            // Door canopy and brass-like handle beside the existing entrance.
            Box(new Vector3(b.center.x+b.size.x*.35f,b.min.y+2.65f,b.min.z-.45f),new Vector3(1.65f,.13f,.9f),accent);
            Box(new Vector3(b.center.x+b.size.x*.35f+.32f,b.min.y+1.2f,b.min.z-.21f),new Vector3(.06f,.35f,.08f),ivory);
            foreach(int sign in new[]{-1,1}){var pot=p+Vector3.right*sign*(b.size.x*.38f);if(Clear(pot,1.15f)){Planter(pot,Quaternion.identity);planters++;furniture.Add(pot);}}
        }}

        foreach(var pair in batches){
            var filter=root.GetComponentsInChildren<MeshFilter>().Single(f=>f.GetComponent<Renderer>().sharedMaterial==pair.Key);var mesh=filter.sharedMesh;
            var merged=new Mesh{indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
            merged.CombineMeshes(new[]{new CombineInstance{mesh=mesh,transform=Matrix4x4.identity}}.Concat(pair.Value).ToArray());
            Undo.RegisterCompleteObjectUndo(mesh,"Detail Warm cream facades");mesh.Clear();mesh.vertices=merged.vertices;mesh.normals=merged.normals;mesh.uv=merged.uv;mesh.tangents=merged.tangents;mesh.triangles=merged.triangles;mesh.RecalculateBounds();mesh.name=pair.Key.name+" street details";EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssetIfDirty(mesh);Object.DestroyImmediate(merged);
        }
        var marker=new GameObject("Warm cream details applied");marker.transform.SetParent(root,false);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return frontages+" Warm cream shopfronts, "+skyline+" background blocks and "+planters+" matching planters. Existing batches reused; no added lights or colliders. Frontages: "+string.Join("; ",frontagePoints.Select(p=>p.ToString()));
    }
    public static string Capture(){var dir="/Users/yoshi/Documents/ChatGPT/Unity Project/Artifacts/HarborStreet/Streets";Directory.CreateDirectory(dir);var go=new GameObject("Temporary street details camera",typeof(Camera));var cam=go.GetComponent<Camera>();cam.enabled=false;cam.fieldOfView=60;cam.aspect=1.6f;cam.nearClipPlane=.15f;cam.farClipPlane=1400;var rt=new RenderTexture(1440,900,24){antiAliasing=4};var previous=RenderTexture.active;
        var positions=new[]{new Vector3(-230,95,-245),new Vector3(-120,7,-162),new Vector3(-21,7,-168),new Vector3(-60,65,210)};var targets=new[]{new Vector3(-50,10,-80),new Vector3(-125,3,-135),new Vector3(-21,3,-143),new Vector3(-40,20,300)};
        try{cam.targetTexture=rt;for(int i=0;i<positions.Length;i++){cam.transform.position=positions[i];cam.transform.LookAt(targets[i]);cam.Render();RenderTexture.active=rt;var tex=new Texture2D(1440,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1440,900),0,0);tex.Apply();File.WriteAllBytes(dir+"/view-"+i+".png",tex.EncodeToPNG());Object.DestroyImmediate(tex);}}finally{RenderTexture.active=previous;Object.DestroyImmediate(go);rt.Release();Object.DestroyImmediate(rt);}return "Four street detail previews captured.";}
}
