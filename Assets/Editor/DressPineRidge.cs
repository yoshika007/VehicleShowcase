using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Object=UnityEngine.Object;

public static class DressPineRidge
{
    const string Dir="Assets/Environments/PineRidge/Details";
    static Transform root; static Vector3[] route; static Mesh cube, sphere, cone;
    static Dictionary<Material,List<CombineInstance>> batches;
    static Material Mat(string name, Color color, float smoothness=.1f)
    {
        var mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,color=color};
        mat.SetFloat("_Smoothness",smoothness);AssetDatabase.CreateAsset(mat,Dir+"/"+name+".mat");return mat;
    }
    static void Part(Mesh mesh,Material material,Vector3 position,Vector3 scale,Quaternion rotation)
    {
        if(!batches.TryGetValue(material,out var list))batches[material]=list=new List<CombineInstance>();
        list.Add(new CombineInstance{mesh=mesh,transform=Matrix4x4.TRS(position,rotation,scale)});
    }
    static Vector3 Side(int i){var d=route[(i+1)%route.Length]-route[(i+route.Length-1)%route.Length];d.y=0;return Vector3.Cross(Vector3.up,d.normalized);}
    static float Ground(Vector3 p)
    {
        var hits=Physics.RaycastAll(new Vector3(p.x,100,p.z),Vector3.down,200);
        var valid=hits.Where(h=>h.collider.gameObject.scene==root.gameObject.scene && h.collider.name=="Forest terrain").OrderByDescending(h=>h.point.y).ToArray();
        return valid.Length>0?valid[0].point.y:p.y-2.2f;
    }
    static void Sign(int index, string message, float side, Material wood)
    {
        var direction=route[(index+1)%route.Length]-route[index]; direction.y=0;direction.Normalize();
        var p=route[index]+Side(index)*side; p.y=Ground(p); var rotation=Quaternion.LookRotation(direction);
        Part(cube,wood,p+Vector3.up*1.5f,new Vector3(.24f,3,.24f),rotation);
        var board=new GameObject(message+" sign",typeof(MeshFilter),typeof(MeshRenderer));board.transform.SetParent(root,false);
        board.GetComponent<MeshFilter>().sharedMesh=cube;board.GetComponent<MeshRenderer>().sharedMaterial=wood;
        board.transform.SetPositionAndRotation(p+Vector3.up*2.8f,rotation);board.transform.localScale=new Vector3(5,1.25f,.22f);
        var label=new GameObject("Lettering",typeof(TextMeshPro));label.transform.SetParent(root,false);
        label.transform.SetPositionAndRotation(board.transform.position-direction*.13f,rotation);
        var t=label.GetComponent<TextMeshPro>();t.text=message;t.fontSize=5;t.alignment=TextAlignmentOptions.Center;t.color=new Color(.96f,.91f,.72f);t.rectTransform.sizeDelta=new Vector2(4.7f,1.1f);
    }
    public static string Run()
    {
        var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/PineRidgeScene.unity"||EditorApplication.isPlaying)throw new Exception("Open Pine Ridge in Edit Mode");
        if(AssetDatabase.IsValidFolder(Dir))throw new Exception("Details already exist");AssetDatabase.CreateFolder("Assets/Environments/PineRidge","Details");
        var map=AssetDatabase.FindAssets("Trail map t:Mesh",new[]{"Assets/Environments/PineRidge"}).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Mesh>).First();
        var v=map.vertices;route=new Vector3[v.Length/2-1];for(int i=0;i<route.Length;i++)route[i]=(v[i*2]+v[i*2+1])*.5f;
        root=new GameObject("Pine Ridge Environment Details").transform; SceneManager.MoveGameObjectToScene(root.gameObject,scene);
        var a=GameObject.CreatePrimitive(PrimitiveType.Cube);cube=a.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(a);
        a=GameObject.CreatePrimitive(PrimitiveType.Sphere);sphere=a.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(a);
        cone=AssetDatabase.FindAssets("Pine crown t:Mesh",new[]{"Assets/Environments/PineRidge"}).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Mesh>).First();
        batches=new Dictionary<Material,List<CombineInstance>>();
        var stone=AssetDatabase.LoadAssetAtPath<Material>("Assets/Environments/PineRidge/Ridge stone.mat");
        var wood=Mat("Trail timber",new Color(.24f,.15f,.09f)); var marker=Mat("Trail marker gold",new Color(.95f,.68f,.19f));
        var bush=Mat("Forest shrubs",new Color(.19f,.34f,.18f));var leaf=Mat("Forest undergrowth",new Color(.40f,.46f,.25f));
        var water=Mat("Creek water",new Color(.20f,.47f,.48f),.18f);var roof=Mat("Ranger station roof",new Color(.20f,.22f,.16f));
        var random=new System.Random(4176);int boulders=0,shrubs=0;
        for(int i=8;i<route.Length;i+=8)
        {
            foreach(float sign in new[]{-1f,1f})
            {
                var p=route[i]+Side(i)*sign*(13f+(float)random.NextDouble()*8);p.y=Ground(p);
                if(i%16==0){float h=.8f+(float)random.NextDouble()*1.8f;Part(sphere,stone,p+Vector3.up*h*.25f,new Vector3(h*1.5f,h*.7f,h),Quaternion.Euler(0,random.Next(360),0));boulders++;}
                else {float h=1.2f+(float)random.NextDouble()*1.8f;Part(sphere,random.Next(2)==0?bush:leaf,p+Vector3.up*h*.32f,new Vector3(h*1.4f,h*.75f,h),Quaternion.identity);shrubs++;}
            }
        }
        for(int i=0;i<route.Length;i+=10)
        {
            foreach(float sign in new[]{-1f,1f})
            {
                var p=route[i]+Side(i)*sign*9f;p.y=route[i].y-.05f;
                Part(cube,wood,p+Vector3.up*.45f,new Vector3(.16f,.9f,.16f),Quaternion.identity);
                Part(cube,marker,p+Vector3.up*.82f,new Vector3(.23f,.16f,.23f),Quaternion.identity);
            }
        }
        // Narrow creek ribbons approach the drivable crossing from either side.
        int creekIndex=365;var center=route[creekIndex];var across=Side(creekIndex);var along=(route[creekIndex+5]-route[creekIndex-5]).normalized;along.y=0;along.Normalize();
        foreach(float direction in new[]{-1f,1f})
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int i=0;i<=24;i++)
            {
                float distance=7.5f+i*3f;var middle=center+across*direction*distance;middle.y=Ground(middle)+.12f;
                if(i==0)middle.y=center.y-.08f;
                var left=middle-along*5.5f; var right=middle+along*5.5f;
                left.y=Mathf.Max(left.y,Ground(left)+.25f); right.y=Mathf.Max(right.y,Ground(right)+.25f);
                vertices.Add(left);vertices.Add(right);
                if(i>0){int k=vertices.Count;triangles.AddRange(new[]{k-4,k-2,k-3,k-3,k-2,k-1,k-3,k-2,k-4,k-1,k-2,k-3});}
            }
            var mesh=new Mesh{name="Creek ribbon"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Dir+"/Creek "+(direction<0?"west":"east")+".asset");
            var g=new GameObject("Creek water",typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(root,false);g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<MeshRenderer>().sharedMaterial=water;
        }
        // A small trailhead set back from the road is visible but cannot obstruct driving.
        int stationIndex=25;var station=route[stationIndex]+Side(stationIndex)*35f;station.y=Ground(station);var heading=route[stationIndex+1]-route[stationIndex];heading.y=0;var rot=Quaternion.LookRotation(heading);
        Part(cube,wood,station+Vector3.up*2.1f,new Vector3(9,4.2f,6),rot);
        Part(cube,roof,station+Vector3.up*4.4f,new Vector3(10,.65f,7),rot);
        Part(cube,marker,station+rot*new Vector3(0,2.2f,-3.08f),new Vector3(2.5f,1.3f,.08f),rot);
        Sign(35,"PINE RIDGE",13f,wood);Sign(335,"CREEK AHEAD",-13f,wood);Sign(110,"RIDGE TRAIL",13f,wood);
        foreach(var item in batches)
        {
            var mesh=new Mesh{name="Pine Ridge "+item.Key.name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.CombineMeshes(item.Value.ToArray());AssetDatabase.CreateAsset(mesh,Dir+"/"+mesh.name+".asset");
            var go=new GameObject("Batched "+item.Key.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=item.Key;go.isStatic=true;
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return $"Added {boulders} roadside rocks, {shrubs} shrubs, trail markers, creek, signs and ranger station; batches={batches.Count}.";
    }
}
