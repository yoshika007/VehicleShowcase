using System;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.Events;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;using TMPro;
public static class SetupOffroadTrailMenu {
 public static string Main(){
 if(EditorApplication.isPlaying)throw new Exception("Edit Mode required");var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/ShowcaseScene.unity"||scene.isDirty)throw new Exception("Clean ShowcaseScene required");
 var manager=UnityEngine.Object.FindAnyObjectByType<VehicleShowcaseManager>();var so=new SerializedObject(manager);var panel=(GameObject)so.FindProperty("challengePanel").objectReferenceValue;var dialog=(RectTransform)panel.transform.Find("Dialog");if(dialog.Find("OffroadTrailButton")!=null)throw new Exception("Already configured");
 Undo.RecordObject(dialog,"Fit four challenge options");dialog.sizeDelta=new Vector2(1120,520);
 var clean=(RectTransform)dialog.Find("CleanLapButton");var trail=UnityEngine.Object.Instantiate(clean.gameObject,dialog,false);Undo.RegisterCreatedObjectUndo(trail,"Add Off-road Trail");trail.name="OffroadTrailButton";trail.transform.Find("Title").GetComponent<TMP_Text>().text="OFF-ROAD TRAIL";trail.transform.Find("Description").GetComponent<TMP_Text>().text="Dirt hills. Gravel turns. Eight gates.";
 var button=trail.GetComponent<UnityEngine.UI.Button>();button.onClick=new UnityEngine.UI.Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(button.onClick,manager.OffroadTrail);
 string[] names={"CleanLapButton","CheckpointSprintButton","SlalomButton","OffroadTrailButton"};for(int i=0;i<names.Length;i++){var r=(RectTransform)dialog.Find(names[i]);Undo.RecordObject(r,"Arrange challenge cards");r.sizeDelta=new Vector2(520,116);r.anchoredPosition=new Vector2(i%2==0?-272:272,i<2?-136:-272);}
 var back=(RectTransform)dialog.Find("BackButton");back.SetAsLastSibling();back.anchoredPosition=new Vector2(0,40);
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return "Four challenge cards in a 1120 x 520 panel, Off-road Trail wired.";
 }
}
