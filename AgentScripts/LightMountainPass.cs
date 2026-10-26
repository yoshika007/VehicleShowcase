using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class LightMountainPass
{
    const string Dir="Assets/Environments/MountainPass";
    static Transform root;
    static GameObject Box(string name,Vector3 p,Vector3 size,Material mat)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(root,false);
        g.transform.position=p;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;
        Object.DestroyImmediate(g.GetComponent<Collider>());g.isStatic=true;return g;
    }
    public static string Run()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(EditorApplication.isPlaying||scene.isDirty||scene.name!="MountainPassScene")
            throw new Exception("Open the saved MountainPassScene outside Play Mode.");
        if(GameObject.Find("Sparse mountain streetlights")!=null)throw new Exception("Streetlights already exist.");
        root=new GameObject("Sparse mountain streetlights").transform;
        var wood=AssetDatabase.LoadAssetAtPath<Material>(Dir+"/Pine bark.mat");
        var dark=AssetDatabase.LoadAssetAtPath<Material>(Dir+"/Sign forest.mat");
        var metal=AssetDatabase.LoadAssetAtPath<Material>(Dir+"/Galvanized rail.mat");
        var lens=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Streetlight glass",color=new Color(.9f,.84f,.64f)};
        AssetDatabase.CreateAsset(lens,Dir+"/Streetlight glass.mat");
        var glow=new Material(lens){name="Streetlight warm glow"};
        glow.EnableKeyword("_EMISSION");glow.SetColor("_EmissionColor",new Color(1,.64f,.25f)*2.5f);
        AssetDatabase.CreateAsset(glow,Dir+"/Streetlight warm glow.mat");
        var road=GameObject.Find("Mountain Pass asphalt").GetComponent<MeshCollider>().sharedMesh.vertices;
        var lights=new List<Light>();var lenses=new List<Renderer>();
        int[] indices={20,65,518,727,937,985,1100,1250};
        int[] sides={1,1,-1,1,-1,-1,1,1};
        Physics.SyncTransforms();
        for(int n=0;n<indices.Length;n++)
        {
            int i=indices[n];
            var p=(road[2*i]+road[2*i+1])*.5f;
            var side=(road[2*i+1]-road[2*i]).normalized*sides[n];
            var forward=((road[2*(i+1)]+road[2*(i+1)+1])*.5f-p).normalized;
            var foot=p+side*9.8f;
            if(Physics.Raycast(foot+Vector3.up*20,Vector3.down,out var hit,50))foot.y=hit.point.y;
            Box("Lamp "+(n+1)+" foundation",foot+Vector3.up*.15f,new Vector3(.65f,.3f,.65f),metal);
            Box("Lamp "+(n+1)+" timber pole",foot+Vector3.up*3.35f,new Vector3(.24f,6.7f,.24f),wood);
            var arm=Box("Shielded lamp arm",foot+Vector3.up*6.55f-side*1.4f,new Vector3(.15f,.15f,3f),dark);
            arm.transform.rotation=Quaternion.LookRotation(side);
            var head=foot+Vector3.up*6.45f-side*2.8f;
            var shield=Box("Downward lamp shade",head,new Vector3(.7f,.22f,1.1f),dark);
            shield.transform.rotation=Quaternion.LookRotation(forward);
            var glass=Box("Lamp lens",head-Vector3.up*.13f,new Vector3(.55f,.05f,.85f),lens);
            glass.transform.rotation=shield.transform.rotation;lenses.Add(glass.GetComponent<Renderer>());
            var lamp=new GameObject("Warm mountain streetlight",typeof(Light)).GetComponent<Light>();
            lamp.transform.SetParent(root,false);lamp.transform.position=head-Vector3.up*.2f;
            lamp.transform.rotation=Quaternion.LookRotation(p+forward*2-lamp.transform.position);
            lamp.type=LightType.Spot;lamp.color=new Color(1,.76f,.43f);lamp.range=25;
            lamp.spotAngle=100;lamp.innerSpotAngle=65;lamp.intensity=90;lamp.shadows=LightShadows.None;
            lamp.enabled=false;lights.Add(lamp);
        }
        // Share the existing proximity-faded night-light controller; no new per-frame system.
        var controller=root.gameObject.AddComponent<LakeTownNightLights>();var data=new SerializedObject(controller);
        var array=data.FindProperty("streetlights");array.arraySize=lights.Count;
        for(int i=0;i<lights.Count;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=lights[i];
        array=data.FindProperty("lanterns");array.arraySize=lenses.Count;
        for(int i=0;i<lenses.Count;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=lenses[i];
        data.FindProperty("windows").arraySize=0;
        data.FindProperty("nightLanterns").objectReferenceValue=glow;
        data.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return "Eight shielded warm streetlights: start (2), hairpins (3), tunnel approaches (2), descent (1).";
    }
}
