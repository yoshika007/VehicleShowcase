using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using System;
public static class EnlargeDrivingControls
{
 public static string Run()
 {
  if(EditorApplication.isPlaying)throw new Exception("Stop Play mode first");
  if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Save current scene first");
  var original=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
  foreach(var path in new[]{"Assets/Scenes/TestTrackScene.unity","Assets/Scenes/LakeTownScene.unity"})
  {
   var scene=EditorSceneManager.OpenScene(path);var controls=UnityEngine.Object.FindAnyObjectByType<TestDriveTouchControls>();var serialized=new SerializedObject(controls);
   foreach(var field in new[]{"steeringGroup","pedalGroup"})
   {
    var group=(RectTransform)serialized.FindProperty(field).objectReferenceValue;
    group.sizeDelta=new Vector2(456,320);
    foreach(Transform child in group)
    {
     if(child.GetComponent<TestDriveTouchButton>()==null)continue;
     var rect=(RectTransform)child;rect.sizeDelta=new Vector2(220,320);
     rect.anchoredPosition=new Vector2(child.name=="SteerRight"||child.name=="Accelerator"?236:0,0);
    }
   }
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  }
  EditorSceneManager.OpenScene(original);
  return "Four controls enlarged to 220 x 320 in Test Circuit and Lake Town; 16-unit gaps preserved.";
 }
}
