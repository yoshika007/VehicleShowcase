using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Object = UnityEngine.Object;

public static class HighlandClimbBuilder
{
    const string Dir = "Assets/Environments/HighlandClimb";
    const string ScenePath = "Assets/Scenes/HighlandClimbScene.unity";
    static Vector3[] route;
    static Transform root;
    static Material dirt, soil, grass, rock, foliage, bark, creek;
    static Mesh cone;
    static int assetNumber;
    static Mesh SaveMesh(string name, Vector3[] vertices, int[] triangles)
    {
        var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.vertices = vertices; mesh.triangles = triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, Dir + "/" + name + " " + assetNumber++ + ".asset"); return mesh;
    }
    static Material Material(string name, Color color)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, color = color };
        m.SetFloat("_Smoothness", .1f); AssetDatabase.CreateAsset(m, Dir + "/" + name + ".mat"); return m;
    }
    static GameObject MeshObject(string name, Mesh mesh, Material material, bool collision = false)
    {
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(root, false);
        go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<MeshRenderer>().sharedMaterial = material;
        if (collision) go.AddComponent<MeshCollider>().sharedMesh = mesh;
        go.isStatic = true; return go;
    }
    static Vector3 Side(int i)
    {
        var v = route[(i + 1) % route.Length] - route[(i + route.Length - 1) % route.Length]; v.y = 0;
        return Vector3.Cross(Vector3.up, v.normalized);
    }
    static float Ground(float x, float z)
    {
        float best = float.PositiveInfinity, y = 0;
        for (int i = 0; i < route.Length; i += 3)
        {
            float dx = x - route[i].x, dz = z - route[i].z, distance = dx * dx + dz * dz;
            if (distance < best) { best = distance; y = route[i].y; }
        }
        float d = Mathf.Sqrt(best);
        return y - 2.2f - Mathf.Max(0, d - 14) * .10f + Mathf.Sin(x * .012f) * Mathf.Sin(z * .016f) * Mathf.Min(2f, d * .015f);
    }
    static Mesh Ribbon(string name, int from, int to, float width, float offset)
    {
        var vertices = new List<Vector3>(); var triangles = new List<int>();
        for (int i = from; i <= to; i++)
        {
            int k = i % route.Length; var side = Side(k);
            vertices.Add(route[k] + side * (-width * .5f) + Vector3.up * offset);
            vertices.Add(route[k] + side * (width * .5f) + Vector3.up * offset);
            if (i > from) { int n = vertices.Count; triangles.AddRange(new[]{n-4,n-2,n-3,n-3,n-2,n-1}); }
        }
        return SaveMesh(name, vertices.ToArray(), triangles.ToArray());
    }
    static void AudioSurface(GameObject go, TestDriveAudioSurface.Kind kind)
    {
        var audio = go.AddComponent<TestDriveAudioSurface>();
        var so = new SerializedObject(audio);
        so.FindProperty("kind").enumValueIndex = (int)kind;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    static void Surface(GameObject go, float grip)
    {
        var surface = go.AddComponent<TestDriveSurface>(); var so = new SerializedObject(surface);
        so.FindProperty("gripMultiplier").floatValue = grip; so.ApplyModifiedPropertiesWithoutUndo();
    }
    public static string Build()
    {
        if (EditorApplication.isPlaying) throw new Exception("Exit Play Mode first");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) throw new Exception("Highland Climb scene already exists");
        if (AssetDatabase.IsValidFolder(Dir)) throw new Exception("Highland Climb asset folder already exists");
        AssetDatabase.CreateFolder("Assets/Environments", "HighlandClimb");
        var template = EditorSceneManager.OpenScene("Assets/Scenes/TestTrackScene.unity", OpenSceneMode.Additive);
        if (!EditorSceneManager.SaveScene(template, ScenePath, true)) throw new Exception("Cannot copy driving scene");
        EditorSceneManager.CloseScene(template, true);
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var infrastructure = scene.GetRootGameObjects().First(g => g.name.StartsWith("Test Track"));
        infrastructure.name = "Highland Climb Driving Infrastructure";
        var geometry = infrastructure.transform.Find("CircuitGeometry"); if (geometry != null) Object.DestroyImmediate(geometry.gameObject);
        foreach (var name in new[]{"CircuitFacilities", "Offroad Course", "Clean Lap Challenge", "Night Roadside Lighting"})
        {
            var go = scene.GetRootGameObjects().FirstOrDefault(x => x.name == name);
            if (go != null) Object.DestroyImmediate(go);
        }
        var manager = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TestDriveSceneManager>(true)).First();
        var managerData = new SerializedObject(manager); managerData.FindProperty("cleanLapChallenge").objectReferenceValue = null; managerData.ApplyModifiedPropertiesWithoutUndo();
        var controls = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TestDriveTouchControls>(true)).First(); var controlsData = new SerializedObject(controls);
        var challenge = controlsData.FindProperty("cleanLapPanel").objectReferenceValue as RectTransform;
        if (challenge != null) challenge.gameObject.SetActive(false);
        controlsData.FindProperty("cleanLapPanel").objectReferenceValue = null; controlsData.ApplyModifiedPropertiesWithoutUndo();
        root = new GameObject("Highland Climb Trail").transform; SceneManager.MoveGameObjectToScene(root.gameObject, scene);
        dirt = Material("Packed trail dirt", new Color(.42f,.29f,.17f)); soil = Material("Earth bank",new Color(.45f,.34f,.22f));
        grass = Material("Forest meadow",new Color(.48f,.39f,.28f)); rock = Material("Ridge stone",new Color(.48f,.41f,.32f));
        foliage = Material("Pine foliage",new Color(.34f,.30f,.22f)); bark = Material("Pine bark",new Color(.30f,.24f,.18f));
        creek = Material("Shallow creek bed",new Color(.36f,.40f,.32f));
        const int samples = 600; route = new Vector3[samples];
        for (int i=0; i<samples; i++)
        {
            float a = i * Mathf.PI * 2 / samples;
            float radius = 1f + .18f * Mathf.Cos(4*a) + .10f * Mathf.Sin(3*a);
            float x = 170 * radius * Mathf.Cos(a) + 18 * Mathf.Sin(3*a), z = 135 * radius * Mathf.Sin(a) + 12 * Mathf.Sin(2*a);
            float height = 18f + 16f * Mathf.Sin(a) + 9f * Mathf.Sin(3*a) + 4f * Mathf.Sin(6*a);
            route[i] = new Vector3(x, height, z);
        }
        var vertices = new List<Vector3>(); var triangles = new List<int>(); const int grid = 90;
        for (int z=0; z<=grid; z++) for (int x=0; x<=grid; x++)
        {
            float px=-540+x*12f, pz=-540+z*12f; vertices.Add(new Vector3(px,Ground(px,pz),pz));
            if (x>0 && z>0) { int i=z*(grid+1)+x; triangles.AddRange(new[]{i-grid-2,i-1,i-grid-1,i-grid-1,i-1,i}); }
        }
        AudioSurface(MeshObject("Forest terrain",SaveMesh("Forest terrain",vertices.ToArray(),triangles.ToArray()),grass,true), TestDriveAudioSurface.Kind.Grass);
        vertices.Clear(); triangles.Clear();
        for (int i=0;i<=samples;i++)
        {
            int k=i%samples; var side=Side(k);
            foreach(float offset in new[]{-24f,-8f,8f,24f})
            {
                var p=route[k]+side*offset; p.y=Mathf.Abs(offset)<9?route[k].y-.15f:Ground(p.x,p.z); vertices.Add(p);
            }
            if(i>0) for(int j=0;j<3;j++){int a=(i-1)*4+j,b=i*4+j;triangles.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
        }
        var banks = MeshObject("Trail earth banks",SaveMesh("Trail earth banks",vertices.ToArray(),triangles.ToArray()),soil,true);
        AudioSurface(banks, TestDriveAudioSurface.Kind.Dirt);
        Surface(banks, 1f); // Dust without changing the bank's original grip.
        var full=Ribbon("Trail map",0,samples,14,0); // map mesh represents the complete loop
        for(int part=0;part<3;part++)
        {
            int from=part==0?0:part==1?350:380, to=part==0?350:part==1?380:samples;
            var road=MeshObject(part==1?"Rocky trail section":"Packed dirt trail "+part,Ribbon("Trail section",from,to,14,.03f),part==1?rock:dirt,true);
            Surface(road,part==1?.85f:.88f);
            AudioSurface(road, part==1 ? TestDriveAudioSurface.Kind.Gravel : TestDriveAudioSurface.Kind.Dirt);
        }
        var cv=new List<Vector3>(); var ct=new List<int>();
        for(int i=0;i<8;i++){float a=i*Mathf.PI/4,b=(i+1)*Mathf.PI/4;int n=cv.Count;cv.AddRange(new[]{new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)),Vector3.up,new Vector3(Mathf.Cos(b),0,Mathf.Sin(b))});ct.AddRange(new[]{n,n+1,n+2});}
        cone=SaveMesh("Pine crown",cv.ToArray(),ct.ToArray());
        var random=new System.Random(20731); var crowns=new List<CombineInstance>(); var trunks=new List<CombineInstance>();
        var cube=GameObject.CreatePrimitive(PrimitiveType.Cube); var cubeMesh=cube.GetComponent<MeshFilter>().sharedMesh; Object.DestroyImmediate(cube);
        for(int i=0;i<450;i++)
        {
            float x=-430+(float)random.NextDouble()*860,z=-430+(float)random.NextDouble()*860;
            if(route.Min(p=>(p.x-x)*(p.x-x)+(p.z-z)*(p.z-z))<30*30)continue;
            float h=8+(float)random.NextDouble()*8; var pos=new Vector3(x,Ground(x,z),z);
            trunks.Add(new CombineInstance{mesh=cubeMesh,transform=Matrix4x4.TRS(pos+Vector3.up*h*.25f,Quaternion.identity,new Vector3(.6f,h*.5f,.6f))});
            crowns.Add(new CombineInstance{mesh=cone,transform=Matrix4x4.TRS(pos+Vector3.up*h*.2f,Quaternion.identity,new Vector3(h*.43f,h*.8f,h*.43f))});
        }
        foreach(var group in new[]{(name:"Pine trees",items:crowns,mat:foliage),(name:"Pine trunks",items:trunks,mat:bark)})
        {
            var mesh=new Mesh{name=group.name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.CombineMeshes(group.items.ToArray());
            AssetDatabase.CreateAsset(mesh,Dir+"/"+group.name+".asset");MeshObject(group.name,mesh,group.mat);
        }
        var spawn=infrastructure.transform.Find("VehicleSpawn"); var direction=route[3]-route[2]; direction.y=0;
        spawn.SetPositionAndRotation(route[2]+Vector3.up*.5f,Quaternion.LookRotation(direction));
        var camera=infrastructure.GetComponentInChildren<Camera>(true); if(camera!=null) camera.farClipPlane=1600;
        var map=scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TestDriveMinimap>(true)).First(); var mapData=new SerializedObject(map);
        mapData.FindProperty("roadMesh").objectReferenceValue=full; mapData.FindProperty("roadTransform").objectReferenceValue=root;
        mapData.FindProperty("startLineA").vector3Value=route[2]-Side(2)*7; mapData.FindProperty("startLineB").vector3Value=route[2]+Side(2)*7; mapData.ApplyModifiedPropertiesWithoutUndo();
        RenderSettings.fog=true; RenderSettings.fogColor=new Color(.68f,.62f,.53f); RenderSettings.fogMode=FogMode.Exponential; RenderSettings.fogDensity=.0008f;
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        var scenes=EditorBuildSettings.scenes.ToList(); scenes.Add(new EditorBuildSettingsScene(ScenePath,true)); EditorBuildSettings.scenes=scenes.ToArray();
        AssetDatabase.SaveAssets(); EditorSceneManager.CloseScene(scene,true);
        return "Created Highland Climb loop and driving scene";
    }
}
