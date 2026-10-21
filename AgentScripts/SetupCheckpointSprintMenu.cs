using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;
using Object=UnityEngine.Object;
public static class SetupCheckpointSprintMenu {
 public static string Main(){
 if(EditorApplication.isPlaying)throw new Exception("Edit Mode required");var original=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath("Assets/Scenes/ShowcaseScene.unity");bool opened=!scene.IsValid()||!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/ShowcaseScene.unity",OpenSceneMode.Additive);
 try{
 if(scene.isDirty)throw new Exception("ShowcaseScene contains unsaved changes");SceneManager.SetActiveScene(scene);VehicleShowcaseManager manager=null;foreach(var root in scene.GetRootGameObjects()){manager=root.GetComponentInChildren<VehicleShowcaseManager>(true);if(manager!=null)break;}
 var so=new SerializedObject(manager);if(so.FindProperty("challengePanel").objectReferenceValue!=null)throw new Exception("Already configured");var source=(GameObject)so.FindProperty("driveModePanel").objectReferenceValue;var panel=Object.Instantiate(source,source.transform.parent,false);panel.name="ChallengeSelectionPanel";panel.transform.SetAsLastSibling();var dialog=panel.transform.Find("Dialog");dialog.Find("Heading").GetComponent<TMP_Text>().text="CHOOSE A CHALLENGE";
 var clean=dialog.Find("FreeRunButton");clean.name="CleanLapButton";clean.Find("Title").GetComponent<TMP_Text>().text="CLEAN LAP";clean.Find("Description").GetComponent<TMP_Text>().text="One lap. Avoid the barriers.";var b=clean.GetComponent<UnityEngine.UI.Button>();b.onClick=new UnityEngine.UI.Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(b.onClick,manager.CleanLap);
 var sprint=dialog.Find("ChallengeButton");sprint.name="CheckpointSprintButton";sprint.Find("Title").GetComponent<TMP_Text>().text="CHECKPOINT SPRINT";sprint.Find("Description").GetComponent<TMP_Text>().text="Four checkpoints. Beat the clock.";b=sprint.GetComponent<UnityEngine.UI.Button>();b.onClick=new UnityEngine.UI.Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(b.onClick,manager.CheckpointSprint);
 b=dialog.Find("BackButton").GetComponent<UnityEngine.UI.Button>();b.onClick=new UnityEngine.UI.Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(b.onClick,manager.BackToDriveModes);
 so.FindProperty("challengePanel").objectReferenceValue=panel;so.ApplyModifiedPropertiesWithoutUndo();panel.SetActive(false);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return "Challenge now opens Clean Lap / Checkpoint Sprint / Back.";
 }finally{if(opened)EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(original);}
 }
}
