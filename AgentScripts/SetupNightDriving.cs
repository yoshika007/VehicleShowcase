using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;
using Object=UnityEngine.Object;
public static class SetupNightDriving
{
    static void Choice(Transform dialog,string oldName,string newName,string title,string description,UnityEngine.Events.UnityAction action)
    {
        var go=dialog.Find(oldName).gameObject;go.name=newName;
        go.transform.Find("Title").GetComponent<TMP_Text>().text=title;
        var desc=go.transform.Find("Description");desc.GetComponent<TMP_Text>().text=description;desc.gameObject.SetActive(!string.IsNullOrEmpty(description));
        if(string.IsNullOrEmpty(description)){var titleRect=(RectTransform)go.transform.Find("Title");titleRect.anchorMin=titleRect.anchorMax=titleRect.pivot=new Vector2(.5f,.5f);titleRect.anchoredPosition=Vector2.zero;titleRect.sizeDelta=new Vector2(500,90);}
        var b=go.GetComponent<UnityEngine.UI.Button>();b.onClick=new UnityEngine.UI.Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(b.onClick,action);
    }
    static bool Headlamp(Mesh mesh,int[] t,int i)
    {
        var v=mesh.vertices;var uv=mesh.uv;int a=t[i],b=t[i+1],c=t[i+2];var p=(v[a]+v[b]+v[c])/3;var sample=(uv[a]+uv[b]+uv[c])/3;
        return sample.x>.24f&&sample.x<.32f&&sample.y>.93f&&p.z>mesh.bounds.center.z+mesh.bounds.size.z*.25f&&p.y<mesh.bounds.min.y+mesh.bounds.size.y*.72f;
    }
    public static string Main()
    {
        if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean Edit Mode required");
        var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/ShowcaseScene.unity")throw new Exception("Open ShowcaseScene first");
        var manager=Object.FindAnyObjectByType<VehicleShowcaseManager>();var so=new SerializedObject(manager);var mode=(GameObject)so.FindProperty("driveModePanel").objectReferenceValue;
        if(so.FindProperty("timeOfDayPanel").objectReferenceValue!=null)throw new Exception("Already configured");
        var time=Object.Instantiate(mode,mode.transform.parent,false);time.name="TimeOfDayPanel";time.transform.SetAsLastSibling();var dialog=time.transform.Find("Dialog");dialog.Find("Heading").GetComponent<TMP_Text>().text="FREE RUN · DAY OR NIGHT";
        var desc=dialog.Find("Description");if(desc!=null)desc.GetComponent<TMP_Text>().text="Choose the light for your drive.";
        Choice(dialog,"FreeRunButton","DayButton","DAY","",manager.DayDrive);
        Choice(dialog,"ChallengeButton","NightButton","NIGHT","",manager.NightDrive);
        var back=dialog.Find("BackButton").GetComponent<UnityEngine.UI.Button>();back.onClick=new UnityEngine.UI.Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(back.onClick,manager.BackToDriveModes);
        so.FindProperty("timeOfDayPanel").objectReferenceValue=time;so.ApplyModifiedPropertiesWithoutUndo();time.SetActive(false);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        // Reuse the independently authored front lenses and lamp positions.
        const string folder="Assets/Prefabs/TestDrive/Lighting";
        foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs/TestDrive"}))
        {
            string path=AssetDatabase.GUIDToAssetPath(guid);var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(prefab.GetComponent<TestDriveMovement>()==null)continue;
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                if(root.GetComponent<DrivingHeadlights>()!=null)throw new Exception("Already configured: "+path);
                string showcaseName=root.name.Replace("_TestDrive","_Showcase");var showcase=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Vehicles/"+showcaseName+".prefab");
                var filter=root.GetComponent<MeshFilter>();var original=filter.sharedMesh;var triangles=original.GetTriangles(1);
                var body=Object.Instantiate(original);body.name=root.name+" Body with Independent Lamps";body.SetTriangles(Enumerable.Range(0,triangles.Length/3).Where(i=>!Headlamp(original,triangles,i*3)).SelectMany(i=>new[]{triangles[i*3],triangles[i*3+1],triangles[i*3+2]}).ToArray(),1);
                AssetDatabase.CreateAsset(body,folder+"/"+root.name+" Headlight Body.asset");filter.sharedMesh=body;
                var lenses=Object.Instantiate(showcase.transform.Find("Showcase Headlamp Lenses").gameObject,root.transform,false);lenses.name="Driving Headlamp Lenses";
                var lights=new System.Collections.Generic.List<Light>();foreach(var source in showcase.GetComponentsInChildren<Light>(true))
                {
                    var go=Object.Instantiate(source.gameObject,root.transform,false);var light=go.GetComponent<Light>();light.name=source.name.Replace("Beam","Driving Beam");light.range=50;light.spotAngle=62;light.innerSpotAngle=32;light.intensity=180;light.enabled=false;light.transform.localRotation=Quaternion.Euler(5,0,0);lights.Add(light);
                }
                // Driving variants inherit the showroom pulse; remove it here.
                var pulse=root.GetComponent<ShowcaseHeadlights>();if(pulse!=null)Object.DestroyImmediate(pulse);
                foreach(string name in new[]{"Showcase Headlamp Lenses","Left Headlight Beam","Right Headlight Beam"}) {var child=root.transform.Find(name);if(child!=null)Object.DestroyImmediate(child.gameObject);}
                var head=root.AddComponent<DrivingHeadlights>();var h=new SerializedObject(head);h.FindProperty("lenses").objectReferenceValue=lenses.GetComponent<Renderer>();var array=h.FindProperty("beams");array.arraySize=lights.Count;for(int i=0;i<lights.Count;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=lights[i];h.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        var track=EditorSceneManager.OpenScene("Assets/Scenes/TestTrackScene.unity",OpenSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(track);var roots=track.GetRootGameObjects();var sun=roots.SelectMany(r=>r.GetComponentsInChildren<Light>(true)).First(l=>l.type==LightType.Directional);
            var sky=new Material(Shader.Find("Skybox/Procedural")){name="Circuit Night Sky"};sky.SetFloat("_SunSize",.025f);sky.SetFloat("_AtmosphereThickness",.35f);sky.SetColor("_SkyTint",new Color(.10f,.16f,.30f));sky.SetColor("_GroundColor",new Color(.025f,.035f,.065f));sky.SetFloat("_Exposure",.18f);AssetDatabase.CreateAsset(sky,"Assets/Environments/CircuitFacilities/CircuitNightSky.mat");
            var host=roots.SelectMany(r=>r.GetComponentsInChildren<TestDriveSceneManager>(true)).Single().gameObject;var night=host.AddComponent<TestDriveNightLighting>();var ns=new SerializedObject(night);ns.FindProperty("sun").objectReferenceValue=sun;ns.FindProperty("nightSky").objectReferenceValue=sky;ns.ApplyModifiedPropertiesWithoutUndo();EditorSceneManager.MarkSceneDirty(track);EditorSceneManager.SaveScene(track);
        }
        finally{EditorSceneManager.CloseScene(track,true);SceneManager.SetActiveScene(scene);}
        AssetDatabase.SaveAssets();return "Time-of-day menu, 9 driving headlamp pairs, and runtime-only night environment configured.";
    }
}
