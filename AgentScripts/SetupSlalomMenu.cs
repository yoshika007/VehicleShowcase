using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;
using Object=UnityEngine.Object;
public static class SetupSlalomMenu {
 public static string Main(){
 if(EditorApplication.isPlaying)throw new Exception("Edit Mode required");var original=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath("Assets/Scenes/ShowcaseScene.unity");bool opened=!scene.IsValid()||!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/ShowcaseScene.unity",OpenSceneMode.Additive);
 try{
 var manager=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<VehicleShowcaseManager>(true)).Single();var so=new SerializedObject(manager);var panel=(GameObject)so.FindProperty("challengePanel").objectReferenceValue;var dialog=(RectTransform)panel.transform.Find("Dialog");if(dialog.Find("SlalomButton")!=null)throw new Exception("Slalom already configured");
 Undo.RecordObject(dialog,"Make room for Slalom");dialog.sizeDelta=new Vector2(600,640);
 var clean=(RectTransform)dialog.Find("CleanLapButton");var sprint=(RectTransform)dialog.Find("CheckpointSprintButton");foreach(var b in new[]{clean,sprint}){Undo.RecordObject(b,"Fit challenge cards");b.sizeDelta=new Vector2(520,104);}clean.anchoredPosition=new Vector2(0,-136);sprint.anchoredPosition=new Vector2(0,-256);
 var slalom=Object.Instantiate(clean.gameObject,dialog,false);slalom.name="SlalomButton";((RectTransform)slalom.transform).anchoredPosition=new Vector2(0,-376);slalom.transform.Find("Title").GetComponent<TMP_Text>().text="SLALOM";slalom.transform.Find("Description").GetComponent<TMP_Text>().text="Six gates. Precision steering.";
 var button=slalom.GetComponent<UnityEngine.UI.Button>();button.onClick=new UnityEngine.UI.Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(button.onClick,manager.Slalom);
 var back=(RectTransform)dialog.Find("BackButton");back.SetAsLastSibling();back.anchoredPosition=new Vector2(0,40);
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return "Added Slalom as the third challenge; existing scene settings preserved.";
 }finally{if(opened)EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(original);}
 }
}
