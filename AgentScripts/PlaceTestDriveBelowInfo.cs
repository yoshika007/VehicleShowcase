using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class PlaceTestDriveBelowInfo
{
 public static string Main()
 {
  var scene=SceneManager.GetActiveScene();
  if(EditorApplication.isPlaying||scene.path!="Assets/Scenes/ShowcaseScene.unity"||scene.isDirty)throw new Exception("Clean showroom in Edit mode required.");
  var rects=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RectTransform>(true)).ToArray();
  var panel=rects.Single(r=>r.name=="VehicleInfoPanel");
  var button=rects.Single(r=>r.name=="TestDriveButton");
  if(panel.parent!=button.parent)throw new Exception("Expected common Canvas parent");
  Canvas.ForceUpdateCanvases();
  button.anchorMin=button.anchorMax=panel.anchorMin;
  button.pivot=new Vector2(.5f,.5f);
  // Match the panel's right edge, with a 24-reference-pixel gap below it.
  button.anchoredPosition=panel.anchoredPosition+new Vector2(
   (1-panel.pivot.x)*panel.rect.width-button.rect.width*.5f,
   -panel.pivot.y*panel.rect.height-24-button.rect.height*.5f);
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  return "TEST DRIVE now right-aligned below VehicleInfoPanel with 24px gap. Anchored position="+button.anchoredPosition;
 }
}
