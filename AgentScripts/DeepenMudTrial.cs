using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// One-time upgrade of the existing wet trial, preserving user materials and vehicle tuning.
public static class DeepenMudTrial
{
    const string Dir = "Assets/Environments/OffroadCourse/WetTrial";
    const float Start = .29f, End = .44f, Depth = .34f;
    static Transform trial;
    static Vector3 Point(float u) => new Vector3(155+80*u,.12f,-30-40*Mathf.Pow(Mathf.Sin(Mathf.PI*u),2));
    static Vector3 Side(float u) => Vector3.Cross(Vector3.up,Point(u+.0001f)-Point(u-.0001f)).normalized;
    static float Drop(float t,float lateral)
    {
        float longitudinal = Mathf.Pow(Mathf.Sin(t*Mathf.PI),2);
        float cross = 1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(4.4f,6f,Mathf.Abs(lateral)));
        float ruts = .025f*(Mathf.Exp(-Mathf.Pow((lateral-.85f)/.32f,2))+Mathf.Exp(-Mathf.Pow((lateral+.85f)/.32f,2)));
        return longitudinal*cross*(Depth+ruts);
    }
    static Mesh Mesh(string name,List<Vector3> vertices,List<int> triangles,List<Vector2> uv=null)
    {
        var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);
        if(uv!=null)mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
    static void Quad(List<Vector3> v,List<int> t,List<Vector2> uv,Transform relative,float x0,float x1,float z0,float z1,float y,Bounds full)
    {
        int n=v.Count;
        foreach(var p in new[]{new Vector3(x0,y,z0),new Vector3(x0,y,z1),new Vector3(x1,y,z1),new Vector3(x1,y,z0)})
        {v.Add(relative.InverseTransformPoint(p));uv.Add(new Vector2(Mathf.InverseLerp(full.min.x,full.max.x,p.x),Mathf.InverseLerp(full.min.z,full.max.z,p.z)));}
        t.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
    }
    static void CutGround(GameObject ground,Bounds hole)
    {
        var box=ground.GetComponent<BoxCollider>();if(box==null)throw new Exception("Expected original box ground: "+ground.name);
        Bounds full=box.bounds;float y=full.max.y;
        var v=new List<Vector3>();var t=new List<int>();var uv=new List<Vector2>();
        Quad(v,t,uv,ground.transform,full.min.x,hole.min.x,full.min.z,full.max.z,y,full);
        Quad(v,t,uv,ground.transform,hole.max.x,full.max.x,full.min.z,full.max.z,y,full);
        Quad(v,t,uv,ground.transform,hole.min.x,hole.max.x,full.min.z,hole.min.z,y,full);
        Quad(v,t,uv,ground.transform,hole.min.x,hole.max.x,hole.max.z,full.max.z,y,full);
        var mesh=Mesh(ground.name+" mud cutout",v,t,uv);AssetDatabase.CreateAsset(mesh,Dir+"/"+mesh.name+".asset");
        // Rendering and collision share the same local opening. Other ground remains at its old height.
        ground.GetComponent<MeshFilter>().sharedMesh=mesh;Object.DestroyImmediate(box);
        ground.AddComponent<MeshCollider>().sharedMesh=mesh;
    }
    static void Pocket(Bounds hole,Material soil)
    {
        const int nx=32,nz=32;var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
        for(int z=0;z<=nz;z++)for(int x=0;x<=nx;x++)
        {
            float wx=Mathf.Lerp(hole.min.x,hole.max.x,x/(float)nx),wz=Mathf.Lerp(hole.min.z,hole.max.z,z/(float)nz);
            float edge=Mathf.Min(wx-hole.min.x,hole.max.x-wx,wz-hole.min.z,hole.max.z-wz);
            float y=Mathf.Lerp(-.05f,-.65f,Mathf.SmoothStep(0,1,edge/2.3f));v.Add(new Vector3(wx,y,wz));uv.Add(new Vector2(wx/4,wz/4));
            if(x<nx&&z<nz){int n=z*(nx+1)+x;t.AddRange(new[]{n,n+nx+1,n+1,n+1,n+nx+1,n+nx+2});}
        }
        var mesh=Mesh("Mud pocket ground",v,t,uv);AssetDatabase.CreateAsset(mesh,Dir+"/Mud pocket ground.asset");
        var go=new GameObject("Mud pocket ground",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));go.transform.SetParent(trial,false);
        go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=soil;go.GetComponent<MeshCollider>().sharedMesh=mesh;
    }
    static void DepthPosts()
    {
        var timber=AssetDatabase.LoadAssetAtPath<Material>("Assets/Environments/OffroadCourse/Timber.mat");
        var gold=AssetDatabase.LoadAssetAtPath<Material>(Dir+"/Ford Depth Marker.mat");
        foreach(float u in new[]{.32f,.365f,.41f})foreach(float sign in new[]{-1f,1f})
        {
            var p=Point(u)+Side(u)*(sign*6.5f);p.y=.50f;
            var post=GameObject.CreatePrimitive(PrimitiveType.Cube);post.name="Deep mud depth post";post.transform.SetParent(trial,false);
            post.transform.position=p;post.transform.localScale=new Vector3(.13f,1.15f,.13f);post.GetComponent<Renderer>().sharedMaterial=timber;Object.DestroyImmediate(post.GetComponent<Collider>());
            foreach(float height in new[]{.0f,.25f})
            {
                var band=GameObject.CreatePrimitive(PrimitiveType.Cube);band.name="Mud depth band";band.transform.SetParent(post.transform,false);
                band.transform.position=new Vector3(p.x,height,p.z);band.transform.localScale=new Vector3(1.12f,.055f/1.15f,1.12f);band.GetComponent<Renderer>().sharedMaterial=gold;Object.DestroyImmediate(band.GetComponent<Collider>());
            }
        }
    }
    static void UpdateMap()
    {
        var root=trial.parent;var names=new[]{"Dirt Hill 1","Dirt Hill 2","Gravel Corner 1","Gravel Corner 2","Dirt access road","Truck ridge branch"};
        var list=names.Select(n=>root.Find(n).GetComponent<MeshFilter>()).ToList();list.AddRange(trial.GetComponentsInChildren<TestDriveSurface>().Select(s=>s.GetComponent<MeshFilter>()));list.Add(GameObject.Find("Asphalt — 12m wide").GetComponent<MeshFilter>());
        var map=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Environments/OffroadCourse/CourseMap.asset");map.Clear();map.CombineMeshes(list.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=f.transform.localToWorldMatrix}).ToArray());EditorUtility.SetDirty(map);Object.FindAnyObjectByType<TestDriveMinimap>().SetVerticesDirty();
    }
    static void DeformBed(MeshFilter bed)
    {
        const int rows=80,across=24;var v=new List<Vector3>();var t=new List<int>();var uv=new List<Vector2>();
        for(int i=0;i<=rows;i++)
        {
            float f=i/(float)rows,u=Mathf.Lerp(Start,End,f);var p=Point(u);var side=Side(u);
            for(int j=0;j<=across;j++)
            {
                float lateral=Mathf.Lerp(-6,6,j/(float)across);var q=p+side*lateral;q.y-=Drop(f,lateral);v.Add(q);uv.Add(new Vector2(j/(float)across*3,i/(float)rows*5));
                if(i<rows&&j<across){int n=i*(across+1)+j;t.AddRange(new[]{n,n+across+1,n+1,n+1,n+across+1,n+across+2});}
            }
        }
        // Match original upward winding regardless of road tangent.
        if(Vector3.Cross(v[t[1]]-v[t[0]],v[t[2]]-v[t[0]]).y<0)for(int i=0;i<t.Count;i+=3){int swap=t[i+1];t[i+1]=t[i+2];t[i+2]=swap;}
        var mesh=bed.sharedMesh;mesh.Clear();mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
        var collider=bed.GetComponent<MeshCollider>();collider.sharedMesh=null;collider.sharedMesh=mesh;
    }
    public static string RefreshBed()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
        trial=GameObject.Find("Offroad Course/Mud and Water Trial").transform;
        var bed=trial.Find("Deep mud bed").GetComponent<MeshFilter>();DeformBed(bed);UpdateMap();
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(trial.gameObject.scene);EditorSceneManager.SaveScene(trial.gameObject.scene);Physics.SyncTransforms();
        return "Mud bed minimum: "+bed.sharedMesh.bounds.min.y;
    }
    public static string Main()
    {
        var scene=SceneManager.GetActiveScene();if(EditorApplication.isPlaying||scene.isDirty||scene.path!="Assets/Scenes/TestTrackScene.unity")throw new Exception("Clean TestTrackScene in Edit mode required");
        trial=GameObject.Find("Offroad Course/Mud and Water Trial").transform;if(trial.Find("Mud pocket ground")!=null)throw new Exception("Deep mud already installed");
        Undo.RegisterFullObjectHierarchyUndo(trial.gameObject,"Deepen mud trial");
        var bed=trial.Find("Deep mud bed").GetComponent<MeshFilter>();var hole=bed.GetComponent<Collider>().bounds;hole.Expand(new Vector3(5,0,5));
        CutGround(GameObject.Find("Ground"),hole);CutGround(GameObject.Find("Offroad ground"),hole);
        Pocket(hole,AssetDatabase.LoadAssetAtPath<Material>(Dir+"/Wet Bank Soil.mat"));
        DeformBed(bed);
        var so=new SerializedObject(bed.GetComponent<TestDriveSurface>());so.FindProperty("visibleWetSurface").objectReferenceValue=trial.Find("Mud slurry surface").GetComponent<Renderer>();so.ApplyModifiedPropertiesWithoutUndo();
        DepthPosts();UpdateMap();
        foreach(var text in trial.Find("Mud warning").GetComponentsInChildren<TMPro.TMP_Text>())text.text="DEEP MUD\n<size=55%>UP TO 0.5m / KEEP MOVING</size>";
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Physics.SyncTransforms();
        Selection.activeGameObject=bed.gameObject;if(SceneView.lastActiveSceneView)SceneView.lastActiveSceneView.LookAt(Point(.365f),Quaternion.Euler(35,0,0),23);
        return $"Deepened mud: bed minimum {bed.sharedMesh.bounds.min.y:F3}m, visible slurry .25m; two local ground cutouts, sloped pocket floor and depth markers. Material/vehicle tuning preserved.";
    }
}
