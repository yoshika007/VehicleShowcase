using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class CenterShowroomButtons
{
 public static string Main()
 {
  var scene=SceneManager.GetActiveScene();
  if(EditorApplication.isPlaying||scene.path!="Assets/Scenes/ShowcaseScene.unity"||scene.isDirty)throw new Exception("Clean showroom in Edit mode required.");
  var buttons=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UnityEngine.UI.Button>(true)).ToArray();
  foreach(var button in buttons)
  {
   if(button.name!="PreviousButton"&&button.name!="NextButton")continue;
   bool left=button.name=="PreviousButton";
   var rect=(RectTransform)button.transform;
   rect.anchorMin=rect.anchorMax=new Vector2(.5f,0);
   rect.pivot=new Vector2(.5f,.5f);
   rect.anchoredPosition=new Vector2(left?-260:260,102);
  }
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  return "Previous and Next are centered around the vehicle counter; the top buttons keep their original positions.";
 }
}
