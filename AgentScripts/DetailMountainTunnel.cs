using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class DetailMountainTunnel
{
    const string Dir="Assets/Environments/MountainPass/TunnelDetails";
    static Transform root;
    static Dictionary<Material,List<CombineInstance>> batches;
    static Mesh cube;
    static Material concrete,trim,rock,dark,warm,amber;
    static Material Mat(string name,Color color,float emission=0)
    {
        var mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,color=color};mat.SetFloat("_Smoothness",.15f);
        if(emission>0){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*emission);}
        AssetDatabase.CreateAsset(mat,Dir+"/"+name+".mat");return mat;
    }
    static void Box(Vector3 position,Vector3 size,Material material,Quaternion rotation)
    {
        if(!batches.ContainsKey(material))batches[material]=new List<CombineInstance>();
        batches[material].Add(new CombineInstance{mesh=cube,transform=Matrix4x4.TRS(position,rotation,size)});
    }
    static Vector3[] Centers()
    {
        var f=GameObject.Find("Summit tunnel vault").GetComponent<MeshFilter>();var v=f.sharedMesh.vertices;
        return Enumerable.Range(0,v.Length/13).Select(i=>f.transform.TransformPoint((v[i*13]+v[i*13+12])*.5f)-Vector3.up).ToArray();
    }
    static void Arch(Vector3 p,Vector3 side,Vector3 forward,float inset,float thickness,Material material)
    {
        const int segments=24;var v=new List<Vector3>();var tri=new List<int>();
        // Elliptical ring remains outside the original tunnel opening.
        for(int i=0;i<=segments;i++){
            float a=i*Mathf.PI/segments;
            for(int z=0;z<2;z++)for(int edge=0;edge<2;edge++)v.Add(p+side*(Mathf.Cos(a)*(8.23f+edge*thickness))+Vector3.up*(1+Mathf.Sin(a)*(7.53f+edge*thickness))+forward*(inset+z*.25f));
            if(i==0)continue;int n=(i-1)*4;
            for(int k=0;k<4;k++){int next=k==0?1:k==1?3:k==3?2:0;tri.AddRange(new[]{n+k,n+next,n+4+k,n+next,n+4+next,n+4+k});}
        }
        var mesh=new Mesh{name="Portal arch "+material.name};mesh.SetVertices(v);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        string path=Dir+"/"+mesh.name+" "+root.childCount+".asset";AssetDatabase.CreateAsset(mesh,path);
        var go=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;go.isStatic=true;
    }
    public static string Run()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.name!="MountainPassScene"||scene.isDirty||EditorApplication.isPlaying)throw new Exception("Expected saved MountainPassScene outside Play Mode.");
        if(GameObject.Find("Mountain tunnel details"))throw new Exception("Tunnel details already exist.");
        var centers=Centers();var colliderCount=Object.FindObjectsByType<Collider>(FindObjectsInactive.Include).Length;
        if(!AssetDatabase.IsValidFolder(Dir))AssetDatabase.CreateFolder("Assets/Environments/MountainPass","TunnelDetails");
        root=new GameObject("Mountain tunnel details").transform;Undo.RegisterCreatedObjectUndo(root.gameObject,"Detail mountain tunnel");batches=new Dictionary<Material,List<CombineInstance>>();
        var temp=GameObject.CreatePrimitive(PrimitiveType.Cube);cube=temp.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(temp);
        concrete=Mat("Weathered portal concrete",new Color(.54f,.55f,.51f));trim=Mat("Dark arch concrete",new Color(.24f,.26f,.25f));
        rock=Mat("Portal fractured rock",new Color(.34f,.37f,.34f));dark=Mat("Tunnel fixture metal",new Color(.09f,.11f,.11f));
        warm=Mat("Warm tunnel lamp",new Color(1,.71f,.35f),2.8f);amber=Mat("Amber wall reflector",new Color(1,.48f,.09f),.7f);
        int fixtures=0,reflectors=0;
        for(int n=0;n<2;n++){
            int i=n==0?0:centers.Length-1;var p=centers[i];var forward=(n==0?centers[1]-p:p-centers[i-1]).normalized;forward.y=0;forward.Normalize();var side=Vector3.Cross(Vector3.up,forward);var outward=forward*(n==0?-1:1);var rotation=Quaternion.LookRotation(forward);
            Arch(p,side,outward,.12f,.85f,concrete);Arch(p,side,outward,.4f,.19f,trim);
            foreach(int sign in new[]{-1,1}){
                Box(p+side*sign*9.25f+Vector3.up*4+outward*.6f,new Vector3(2.1f,8.2f,.55f),concrete,rotation);
                Box(p+side*sign*9.25f+Vector3.up*.35f+outward*.85f,new Vector3(2.45f,.7f,1),trim,rotation);
                // Horizontal formwork seams and water-stain strips on the outer piers.
                for(int row=1;row<6;row++)Box(p+side*sign*9.25f+Vector3.up*(row*1.25f)+outward*.89f,new Vector3(2.05f,.035f,.025f),trim,rotation);
                for(int k=0;k<4;k++){
                    var pos=p+side*sign*(12.4f+k*2.1f)+outward*(2+k%2*2);
                    var hits=Physics.RaycastAll(pos+Vector3.up*15,Vector3.down,120).Where(h=>h.collider.name=="Alpine terrain"||h.collider.name=="Road embankment").OrderByDescending(h=>h.point.y).ToArray();
                    if(hits.Length==0)throw new Exception("No ground for entrance boulder");
                    float height=4.5f+(k%3)*1.375f;pos.y=hits[0].point.y+height*.34f;
                    Box(pos,new Vector3(2.8f+k*.2f,height,2.5f),rock,Quaternion.Euler(8+k*4,sign*(20+k*19),sign*12));
                }
            }
            // A thin concrete cap reinforces the existing lintel without narrowing its opening.
            Box(p+Vector3.up*9.2f+outward*.65f,new Vector3(20.5f,1.5f,.65f),concrete,rotation);
        }
        for(int i=0;i<centers.Length;i++){
            var p=centers[i];var forward=(centers[Mathf.Min(i+1,centers.Length-1)]-centers[Mathf.Max(i-1,0)]).normalized;forward.y=0;forward.Normalize();var side=Vector3.Cross(Vector3.up,forward);var rotation=Quaternion.LookRotation(forward);
            if(i<centers.Length-1){var q=centers[i+1];float length=Vector3.Distance(p,q);var mid=(p+q)*.5f;
                foreach(int sign in new[]{-1,1}){Box(mid+side*sign*7.88f+Vector3.up*.2f,new Vector3(.32f,.4f,length+.04f),concrete,rotation);Box(mid+side*sign*7.62f+Vector3.up*.045f,new Vector3(.15f,.05f,length+.04f),dark,rotation);}}
            if(i%10==0){
                Box(p+Vector3.up*7.15f,new Vector3(2.2f,.5f,2.2f),dark,rotation);
                Box(p+Vector3.up*6.9f,new Vector3(2.02f,.08f,.66f),warm,rotation);fixtures++;
                foreach(int sign in new[]{-1,1}){Box(p+side*sign*7.83f+Vector3.up*1.15f,new Vector3(.1f,.4f,.3f),dark,rotation);Box(p+side*sign*7.76f+Vector3.up*1.15f,new Vector3(.05f,.24f,.17f),amber,rotation);reflectors++;}
            }
        }
        foreach(var pair in batches){var mesh=new Mesh{name=pair.Key.name+" details",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.CombineMeshes(pair.Value.ToArray());AssetDatabase.CreateAsset(mesh,Dir+"/"+mesh.name+".asset");var go=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=pair.Key;go.isStatic=true;}
        if(Object.FindObjectsByType<Collider>(FindObjectsInactive.Include).Length!=colliderCount)throw new Exception("Collider count changed");
        RefineBoulders();
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return "Two concrete portal surrounds, 16 grounded boulders, "+fixtures+" warm emissive fixtures, "+reflectors+" reflectors, maintenance curbs and drainage. No added real-time lights or colliders. Tunnel rings="+centers.Length;
    }
    public static string RefineBoulders()
    {
        var mesh=GameObject.Find("Mountain tunnel details").GetComponentsInChildren<MeshFilter>().Single(f=>f.name=="Portal fractured rock details").sharedMesh;
        var old=mesh.vertices;var vertices=new List<Vector3>();var triangles=new List<int>();var random=new System.Random(1701);
        for(int i=0;i<old.Length;i+=24){var points=old.Skip(i).Take(24).ToArray();var center=points.Aggregate(Vector3.zero,(a,b)=>a+b)/24;float bottom=points.Min(p=>p.y);float height=3.6f+(i/24%3)*.55f;int start=vertices.Count;
            for(int ring=0;ring<3;ring++)for(int j=0;j<8;j++){float angle=j*Mathf.PI/4+ring*.13f;float radius=(ring==0?1.8f:ring==1?2.5f:.8f)*( .8f+(float)random.NextDouble()*.35f);vertices.Add(new Vector3(center.x+Mathf.Cos(angle)*radius,bottom+height*(ring==0?0:ring==1?.45f:1)+((float)random.NextDouble()-.5f)*.25f,center.z+Mathf.Sin(angle)*radius));}
            for(int ring=0;ring<2;ring++)for(int j=0;j<8;j++){int a=start+ring*8+j,b=start+ring*8+(j+1)%8,c=a+8,d=b+8;triangles.AddRange(new[]{a,c,b,b,c,d});}
            for(int j=1;j<7;j++)triangles.AddRange(new[]{start+16,start+16+j+1,start+16+j});
        }
        // Separate face vertices retain the fractured low-poly rock surface.
        var flat=triangles.Select(i=>vertices[i]).ToArray();mesh.Clear();mesh.vertices=flat;mesh.triangles=Enumerable.Range(0,flat.Length).ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssetIfDirty(mesh);return "16 faceted entrance boulders refined.";
    }
    public static string Capture()
    {
        var centers=Centers();var dir="/Users/yoshi/Documents/ChatGPT/Unity Project/Artifacts/MountainPass/Tunnel";Directory.CreateDirectory(dir);
        var go=new GameObject("Temporary tunnel inspection camera",typeof(Camera));var camera=go.GetComponent<Camera>();camera.enabled=false;camera.fieldOfView=64;camera.aspect=1.6f;camera.nearClipPlane=.15f;camera.farClipPlane=1400;
        var rt=new RenderTexture(1440,900,24){antiAliasing=4};var previous=RenderTexture.active;
        try{camera.targetTexture=rt;for(int view=0;view<3;view++){int i=view==0?0:view==1?centers.Length-1:centers.Length/2;var p=centers[i];var forward=(centers[Mathf.Min(i+1,centers.Length-1)]-centers[Mathf.Max(i-1,0)]).normalized;var outward=view==1?forward:-forward;camera.transform.position=view==2?p+Vector3.up*1.6f:p+outward*28+Vector3.up*2;camera.transform.LookAt(view==2?p+forward*30+Vector3.up*2:p+Vector3.up*4);camera.Render();RenderTexture.active=rt;var tex=new Texture2D(1440,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1440,900),0,0);tex.Apply();File.WriteAllBytes(dir+"/"+new[]{"west-entrance","east-entrance","interior"}[view]+".png",tex.EncodeToPNG());Object.DestroyImmediate(tex);}}
        finally{RenderTexture.active=previous;Object.DestroyImmediate(go);rt.Release();Object.DestroyImmediate(rt);}return "Three driver-height tunnel views captured.";
    }
}
