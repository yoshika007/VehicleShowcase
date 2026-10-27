using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Object=UnityEngine.Object;

// Adds optional wet lanes over the existing road; leaves a dry line alongside.
public static class AddPineWetLanes
{
    const string Dir="Assets/Environments/PineRidge/WetLanes";
    static Vector3[] route;
    static Transform root;
    static Vector3 Side(int i)
    {
        var v=route[(i+1)%route.Length]-route[(i+route.Length-1)%route.Length];v.y=0;
        return Vector3.Cross(Vector3.up,v.normalized);
    }
    static GameObject Lane(string name,int first,int last,Material material,TestDriveSurface.Kind kind,Transform recovery)
    {
        var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();float distance=0;
        for(int i=first;i<=last;i++)
        {
            if(i>first)distance+=Vector3.Distance(route[i],route[i-1]);
            // The inner edge stops short of the centre line, retaining 7m of dry trail.
            float fade=Mathf.SmoothStep(0,1,Mathf.Min((i-first)/8f,(last-i)/8f));
            foreach(float offset in new[]{-6.5f,-.35f})
            {
                var p=route[i]+Side(i)*offset;p.y+=.025f+.075f*fade;
                vertices.Add(p);uv.Add(new Vector2((offset+6.5f)/6.15f*2,distance/5));
            }
            if(i>first){int n=vertices.Count;triangles.AddRange(new[]{n-4,n-2,n-3,n-3,n-2,n-1});}
        }
        var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Dir+"/"+name+".asset");
        var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider),typeof(TestDriveSurface),typeof(TestDriveAudioSurface));g.transform.SetParent(root,false);
        g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<MeshRenderer>().sharedMaterial=material;g.GetComponent<MeshCollider>().sharedMesh=mesh;
        var s=new SerializedObject(g.GetComponent<TestDriveSurface>());s.FindProperty("kind").enumValueIndex=(int)kind;
        s.FindProperty("gripMultiplier").floatValue=kind==TestDriveSurface.Kind.Mud?.58f:.78f;
        s.FindProperty("resistanceAcceleration").floatValue=kind==TestDriveSurface.Kind.Mud?.55f:.12f;
        s.FindProperty("velocityResistance").floatValue=kind==TestDriveSurface.Kind.Mud?.5f:.22f;
        s.FindProperty("mudBogResistance").floatValue=kind==TestDriveSurface.Kind.Mud?4.5f:0;
        s.FindProperty("recoveryPoint").objectReferenceValue=kind==TestDriveSurface.Kind.Mud?recovery:null;
        s.FindProperty("dustColor").colorValue=kind==TestDriveSurface.Kind.Mud?new Color(.25f,.14f,.065f,.85f):new Color(.68f,.88f,.9f,.8f);
        s.ApplyModifiedPropertiesWithoutUndo();
        var a=new SerializedObject(g.GetComponent<TestDriveAudioSurface>());a.FindProperty("kind").enumValueIndex=(int)TestDriveAudioSurface.Kind.Dirt;a.ApplyModifiedPropertiesWithoutUndo();
        return g;
    }
    static void Sign(int i,string title,Material material)
    {
        var direction=route[i+1]-route[i];direction.y=0;direction.Normalize();var side=Side(i);var p=route[i]+side*(-10f);
        var post=GameObject.CreatePrimitive(PrimitiveType.Cube);post.name=title+" post";post.transform.SetParent(root,false);post.transform.position=p+Vector3.up*1.4f;post.transform.localScale=new Vector3(.18f,2.8f,.18f);post.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(post.GetComponent<Collider>());
        var board=GameObject.CreatePrimitive(PrimitiveType.Cube);board.name=title+" board";board.transform.SetParent(root,false);board.transform.SetPositionAndRotation(p+Vector3.up*2.5f,Quaternion.LookRotation(direction));board.transform.localScale=new Vector3(4.2f,1.15f,.16f);board.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(board.GetComponent<Collider>());
        var label=new GameObject("Lettering",typeof(TextMeshPro));label.transform.SetParent(root,false);label.transform.SetPositionAndRotation(board.transform.position-direction*.09f,board.transform.rotation);var text=label.GetComponent<TextMeshPro>();text.text=title;text.fontSize=4.2f;text.alignment=TextAlignmentOptions.Center;text.color=new Color(.98f,.8f,.35f);text.rectTransform.sizeDelta=new Vector2(4f,1f);
    }
    static void AddSecondMud(Material mud, Material timber)
    {
        var recovery = new GameObject("Forest Mud Dry Recovery Point").transform;
        recovery.SetParent(root, false);
        var direction = route[529] - route[528]; direction.y = 0;
        recovery.SetPositionAndRotation(route[528] + Side(528) * 3f + Vector3.up * .8f, Quaternion.LookRotation(direction));
        Lane("Forest mud lane", 480, 525, mud, TestDriveSurface.Kind.Mud, recovery);
        Sign(465, "MUD AHEAD", timber);
    }
    public static string ExtendMud()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/PineRidgeScene.unity" || EditorApplication.isPlaying) throw new Exception("Open Pine Ridge in Edit Mode");
        root = scene.GetRootGameObjects().First(x => x.name == "Optional Mud and Water Lanes").transform;
        if (root.Find("Forest mud lane") != null || AssetDatabase.LoadAssetAtPath<Mesh>(Dir + "/Forest mud lane.asset") != null) throw new Exception("Forest mud lane already exists");
        var map = AssetDatabase.FindAssets("Trail map t:Mesh", new[] { "Assets/Environments/PineRidge" }).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Mesh>).First();
        var vertices = map.vertices; route = new Vector3[vertices.Length / 2 - 1];
        for (int i = 0; i < route.Length; i++) route[i] = (vertices[i * 2] + vertices[i * 2 + 1]) * .5f;
        var mud = AssetDatabase.LoadAssetAtPath<Material>("Assets/Environments/OffroadCourse/WetTrial/Wet Rutted Mud.mat");
        var timber = AssetDatabase.LoadAssetAtPath<Material>("Assets/Environments/PineRidge/Details/Trail timber.mat");
        if (mud == null || timber == null) throw new Exception("Required existing materials not found");
        AddSecondMud(mud, timber);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        return "Added a second mud lane, warning sign, and nearby dry recovery point.";
    }
    public static string Run()
    {
        var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/PineRidgeScene.unity"||EditorApplication.isPlaying)throw new Exception("Open Pine Ridge in Edit Mode");
        if(AssetDatabase.IsValidFolder(Dir))throw new Exception("Wet lanes already exist");AssetDatabase.CreateFolder("Assets/Environments/PineRidge","WetLanes");
        var mud=AssetDatabase.LoadAssetAtPath<Material>("Assets/Environments/OffroadCourse/WetTrial/Wet Rutted Mud.mat");
        var water=AssetDatabase.LoadAssetAtPath<Material>("Assets/Environments/OffroadCourse/WetTrial/Shallow Ford Water.mat");
        var timber=AssetDatabase.LoadAssetAtPath<Material>("Assets/Environments/PineRidge/Details/Trail timber.mat");
        if(mud==null||water==null||timber==null)throw new Exception("Required existing wet-trial materials not found");
        var map=AssetDatabase.FindAssets("Trail map t:Mesh",new[]{"Assets/Environments/PineRidge"}).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Mesh>).First();
        var verts=map.vertices;route=new Vector3[verts.Length/2-1];for(int i=0;i<route.Length;i++)route[i]=(verts[i*2]+verts[i*2+1])*.5f;
        root=new GameObject("Optional Mud and Water Lanes").transform;SceneManager.MoveGameObjectToScene(root.gameObject,scene);
        var recovery=new GameObject("Mud Dry Recovery Point").transform;recovery.SetParent(root,false);var direction=route[249]-route[248];direction.y=0;
        recovery.SetPositionAndRotation(route[248]+Side(248)*3f+Vector3.up*.8f,Quaternion.LookRotation(direction));
        var muddy=Lane("Mud lane",205,245,mud,TestDriveSurface.Kind.Mud,recovery);
        var ford=Lane("Shallow water lane",355,375,water,TestDriveSurface.Kind.Water,null);
        Sign(190,"MUD LANE",timber);Sign(340,"SHALLOW WATER",timber);
        AddSecondMud(mud, timber);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return "Added optional mud and shallow water lanes with spray, resistance, audio and dry-side recovery.";
    }
}
