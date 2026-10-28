using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class HighlandClimbBackdrop
{
    const string Dir = "Assets/Environments/HighlandClimb";
    public static void Generate(Scene scene, Transform root)
    {
        if (root.Find("Distant desert hills") != null) throw new Exception("Backdrop already exists");
        string meshPath = Dir + "/Distant desert hills.asset", materialPath = Dir + "/Distant desert hills.mat";
        if (AssetDatabase.LoadAssetAtPath<Mesh>(meshPath) != null || AssetDatabase.LoadAssetAtPath<Material>(materialPath) != null) throw new Exception("Backdrop assets already exist");
        var terrain = root.Find("Desert terrain")?.GetComponent<MeshCollider>();
        if (terrain == null) throw new Exception("Desert terrain collider is missing");
        var vertices = new List<Vector3>(); var triangles = new List<int>();
        const int count=128; float[] radii={430f,520f,700f,1000f,1400f};
        for (int i=0;i<=count;i++)
        {
            float angle=i*Mathf.PI*2/count; float x=Mathf.Cos(angle),z=Mathf.Sin(angle);
            float ridge=80f+28f*Mathf.Sin(3*angle+.5f)+19f*Mathf.Sin(7*angle+1.4f)+12f*Mathf.Sin(11*angle+.9f);
            float[] ground=new float[2];
            for(int k=0;k<2;k++) {var ray=new Ray(new Vector3(x*radii[k],300,z*radii[k]),Vector3.down);ground[k]=terrain.Raycast(ray,out var hit,600)?hit.point.y-0.35f:-35f;}
            for(int j=0;j<radii.Length;j++)
            {
                float y=j==0?ground[0]:j==1?ground[1]:j==2?Mathf.Lerp(ground[1],ridge,.28f):j==3?ridge:ridge*.82f+25f;
                vertices.Add(new Vector3(x*radii[j],y,z*radii[j]));
                if(i>0&&j>0){int b=i*radii.Length+j,a=b-radii.Length;triangles.AddRange(new[]{a-1,b-1,a,a,b-1,b});}
            }
        }
        var mesh=new Mesh{name="Distant desert hills"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,meshPath);
        var mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Distant desert hills",color=new Color(.50f,.43f,.35f)};mat.SetFloat("_Smoothness",0f);AssetDatabase.CreateAsset(mat,materialPath);
        var go=new GameObject("Distant desert hills",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=mat;go.isStatic=true;
        AddDetails(scene, root);
    }
    public static void AddDetails(Scene scene, Transform root)
    {
        if (root.Find("Desert perimeter boundary") != null || root.Find("Distant hill rocks") != null) throw new Exception("Hill details already exist");
        var terrain = root.Find("Desert terrain").GetComponent<MeshCollider>();
        var hills = root.Find("Distant desert hills").GetComponent<MeshFilter>().sharedMesh.vertices;
        const int count=128; const float radius=420f;
        var vertices=new List<Vector3>(); var triangles=new List<int>();
        for(int i=0;i<=count;i++)
        {
            float a=i*Mathf.PI*2/count;float x=Mathf.Cos(a)*radius,z=Mathf.Sin(a)*radius;
            if(!terrain.Raycast(new Ray(new Vector3(x,300,z),Vector3.down),out var hit,600)) throw new Exception("Boundary does not meet terrain");
            vertices.Add(new Vector3(x,hit.point.y-5f,z));vertices.Add(new Vector3(x,hit.point.y+25f,z));
            if(i>0){int n=vertices.Count;triangles.AddRange(new[]{n-4,n-2,n-3,n-3,n-2,n-1,n-3,n-2,n-4,n-1,n-2,n-3});}
        }
        string wallPath=Dir+"/Desert perimeter collision.asset";
        if(AssetDatabase.LoadAssetAtPath<Mesh>(wallPath)!=null)throw new Exception("Boundary asset already exists");
        var wall=new Mesh{name="Desert perimeter collision"};wall.SetVertices(vertices);wall.SetTriangles(triangles,0);wall.RecalculateBounds();AssetDatabase.CreateAsset(wall,wallPath);
        var boundary=new GameObject("Desert perimeter boundary",typeof(MeshCollider));boundary.transform.SetParent(root,false);boundary.GetComponent<MeshCollider>().sharedMesh=wall;boundary.isStatic=true;
        var rocks=CreateRockInstances(hills);
        string rocksPath=Dir+"/Distant hill rocks.asset";if(AssetDatabase.LoadAssetAtPath<Mesh>(rocksPath)!=null)throw new Exception("Hill rock asset already exists");
        var rockMesh=new Mesh{name="Distant hill rocks",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};rockMesh.CombineMeshes(rocks.ToArray());AssetDatabase.CreateAsset(rockMesh,rocksPath);
        var detail=new GameObject("Distant hill rocks",typeof(MeshFilter),typeof(MeshRenderer));detail.transform.SetParent(root,false);detail.GetComponent<MeshFilter>().sharedMesh=rockMesh;detail.GetComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Dir+"/Ridge stone.mat");detail.isStatic=true;
    }
    // Sample the actual hill triangles so rocks are grounded between the mesh vertices.
    static List<CombineInstance> CreateRockInstances(Vector3[] hills)
    {
        var sphere=Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
        var rocks=new List<CombineInstance>();var random=new System.Random(6891);
        for(int i=0;i<128;i++)
        {
            int angle=random.Next(128), band=random.Next(1,4);
            var a=hills[angle*5+band];var b=hills[(angle+1)*5+band];
            var c=hills[angle*5+band+1];var d=hills[(angle+1)*5+band+1];
            float u=(float)random.NextDouble(),v=(float)random.NextDouble();
            if(u+v>1f){u=1f-u;v=1f-v;}
            Vector3 p=random.Next(2)==0 ? a+(b-a)*u+(c-a)*v : d+(b-d)*u+(c-d)*v;
            float size=8f+(float)random.NextDouble()*16f;
            var rotation=Quaternion.Euler((float)random.NextDouble()*16f-8f,(float)random.NextDouble()*360f,(float)random.NextDouble()*18f-9f);
            rocks.Add(new CombineInstance{mesh=sphere,transform=Matrix4x4.TRS(p+Vector3.up*size*.20f,rotation,new Vector3(size,size*.65f,size*.7f))});
        }
        return rocks;
    }
    public static string RedistributeRocks()
    {
        var scene=SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/HighlandClimbScene.unity"||scene.isDirty||EditorApplication.isPlaying)throw new Exception("Open the saved Highland Climb scene in Edit Mode");
        var root=scene.GetRootGameObjects().First(x=>x.name=="Highland Climb Trail").transform;
        var hills=root.Find("Distant desert hills").GetComponent<MeshFilter>().sharedMesh.vertices;
        var mesh=root.Find("Distant hill rocks").GetComponent<MeshFilter>().sharedMesh;
        mesh.Clear();mesh.CombineMeshes(CreateRockInstances(hills).ToArray());EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssets();
        return "Redistributed 128 rocks across hill triangles";
    }
    public static string AddDetailsToCurrentScene()
    {
        var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/HighlandClimbScene.unity"||scene.isDirty||EditorApplication.isPlaying)throw new Exception("Open the saved Highland Climb scene in Edit Mode");
        var root=scene.GetRootGameObjects().First(x=>x.name=="Highland Climb Trail").transform;AddDetails(scene,root);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();return "Added hill rock formations and a continuous perimeter collider";
    }
    public static string AddToCurrentScene()
    {
        var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/HighlandClimbScene.unity"||scene.isDirty||EditorApplication.isPlaying)throw new Exception("Open the saved Highland Climb scene in Edit Mode");
        var root=scene.GetRootGameObjects().First(x=>x.name=="Highland Climb Trail").transform;Generate(scene,root);RenderSettings.fogDensity=.0012f;EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();return "Added distant desert hills and gentle haze";
    }
}
