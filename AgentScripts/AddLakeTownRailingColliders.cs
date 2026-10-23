using System;
using System.Linq;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEditor;
using Object=UnityEngine.Object;
public static class AddLakeTownRailingColliders
{
    public static string Run()
    {
        if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean Edit Mode required");
        var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/LakeTownScene.unity")throw new Exception("Open Lake Town first");
        var bars=Object.FindAnyObjectByType<LakeTownNightLights>().GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Lakeside railing").ToArray();
        if(bars.Length==0)throw new Exception("No railing bars found");
        int added=0;
        foreach(var bar in bars)
        {
            if(bar.GetComponent<Collider>()!=null)continue;
            var collider=bar.gameObject.AddComponent<BoxCollider>();collider.center=Vector3.zero;collider.size=Vector3.one;collider.isTrigger=false;added++;
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return "Added "+added+" solid Box Colliders, each matching its visible bar; existing posts, disabled objects and openings unchanged.";
    }
}
