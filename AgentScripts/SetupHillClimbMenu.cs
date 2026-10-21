using System;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.Events;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;using TMPro;
public static class SetupHillClimbMenu {
 public static string Main(){
 if(EditorApplication.isPlaying)throw new Exception("Edit Mode required");var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/ShowcaseScene.unity"||scene.isDirty)throw new Exception("Clean ShowcaseScene required");
 var manager=UnityEngine.Object.FindAnyObjectByType<VehicleShowcaseManager>();var so=new SerializedObject(manager);var panel=(GameObject)so.FindProperty("challengePanel").objectReferenceValue;var dialog=(RectTransform)panel.transform.Find("Dialog");if(dialog.Find("HillClimbButton")!=null)throw new Exception("Already configured");
 Undo.RecordObject(dialog,"Fit five challenge options");dialog.sizeDelta=new Vector2(1120,660);
 var clean=(RectTransform)dialog.Find("CleanLapButton");var trail=UnityEngine.Object.Instantiate(clean.gameObject,dialog,false);Undo.RegisterCreatedObjectUndo(trail,"Add Hill Climb");trail.name="HillClimbButton";trail.transform.Find("Title").GetComponent<TMP_Text>().text="HILL CLIMB";trail.transform.Find("Description").GetComponent<TMP_Text>().text="Three gates. One summit.";
 var button=trail.GetComponent<UnityEngine.UI.Button>();button.onClick=new UnityEngine.UI.Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(button.onClick,manager.HillClimb);
 string[] names={"CleanLapButton","CheckpointSprintButton","SlalomButton","OffroadTrailButton","HillClimbButton"};for(int i=0;i<names.Length;i++){var r=(RectTransform)dialog.Find(names[i]);Undo.RecordObject(r,"Arrange challenge cards");r.sizeDelta=new Vector2(520,116);r.anchoredPosition=new Vector2(i==4?0:i%2==0?-272:272,-136*(i/2+1));}
 var back=(RectTransform)dialog.Find("BackButton");back.SetAsLastSibling();back.anchoredPosition=new Vector2(0,40);
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return "Five challenge cards in a 1120 x 660 panel, Hill Climb wired.";
 }
}
