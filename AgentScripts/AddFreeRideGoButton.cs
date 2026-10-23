using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;
using Object=UnityEngine.Object;
public static class AddFreeRideGoButton
{
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit Mode required");
        var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/ShowcaseScene.unity")throw new Exception("Open Showcase first");
        var manager=Object.FindAnyObjectByType<VehicleShowcaseManager>();var data=new SerializedObject(manager);
        var panel=(GameObject)data.FindProperty("courseSelectionPanel").objectReferenceValue;var dialog=panel.transform.Find("Dialog");
        if(dialog.Find("Go")!=null)throw new Exception("GO already exists");
        var back=dialog.Find("Back").GetComponent<RectTransform>();back.anchoredPosition=new Vector2(-145,-265);
        var go=Object.Instantiate(back.gameObject,dialog,false);go.name="Go";go.GetComponent<RectTransform>().anchoredPosition=new Vector2(145,-265);
        var button=go.GetComponent<UnityEngine.UI.Button>();button.onClick=new UnityEngine.UI.Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(button.onClick,manager.StartFreeRide);
        go.GetComponent<UnityEngine.UI.Image>().color=new Color(.98f,.77f,.25f);var label=go.GetComponentInChildren<TMP_Text>();label.text="GO";label.color=new Color(.04f,.06f,.08f);
        dialog.Find("Subtitle").GetComponent<TMP_Text>().text="FREE RUN  /  Choose course and lighting, then GO";
        var menu=panel.GetComponent<FreeRideCourseMenu>();var so=new SerializedObject(menu);
        foreach(var name in new[]{"TestCircuitCourse","LakeTownCourse"})
        {
            var card=dialog.Find(name).GetComponent<UnityEngine.UI.Button>();var outline=card.GetComponent<UnityEngine.UI.Outline>()??card.gameObject.AddComponent<UnityEngine.UI.Outline>();outline.effectColor=new Color(.98f,.77f,.25f);outline.effectDistance=new Vector2(3,-3);outline.enabled=name=="TestCircuitCourse";
            so.FindProperty(name=="TestCircuitCourse"?"circuit":"lakeTown").objectReferenceValue=card;
        }
        so.ApplyModifiedPropertiesWithoutUndo();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return "Added GO to the right of Back; course cards select only, gold border marks the chosen course.";
    }
}
