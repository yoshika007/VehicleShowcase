using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class StyleShowroomTestDrive
{
 public static string Main()
 {
  var scene=SceneManager.GetActiveScene();
  if(EditorApplication.isPlaying || scene.path!="Assets/Scenes/ShowcaseScene.unity" || scene.isDirty)throw new Exception("Clean showroom in Edit mode required.");
  var button=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UnityEngine.UI.Button>(true)).Single(b=>b.name=="TestDriveButton");
  var rect=(RectTransform)button.transform;
  // Center the animation pivot without moving the button's resting screen bounds.
  var pivot=new Vector2(.5f,.5f);rect.anchoredPosition+=Vector2.Scale(rect.rect.size,pivot-rect.pivot);rect.pivot=pivot;
  var image=button.GetComponent<UnityEngine.UI.Image>();image.enabled=false;image.raycastTarget=false;
  var faceTransform=button.transform.Find("MotorsportFace");
  if(faceTransform==null){var go=new GameObject("MotorsportFace",typeof(RectTransform),typeof(CanvasRenderer),typeof(TestDriveButtonFace));go.layer=button.gameObject.layer;go.transform.SetParent(button.transform,false);faceTransform=go.transform;}
  faceTransform.SetAsFirstSibling();
  var faceRect=(RectTransform)faceTransform;faceRect.anchorMin=Vector2.zero;faceRect.anchorMax=Vector2.one;faceRect.offsetMin=faceRect.offsetMax=Vector2.zero;
  var face=faceTransform.GetComponent<TestDriveButtonFace>();face.color=Color.white;face.raycastTarget=true;
  var shadow=face.GetComponent<UnityEngine.UI.Shadow>()??face.gameObject.AddComponent<UnityEngine.UI.Shadow>();shadow.effectColor=new Color(.015f,.025f,.035f,.28f);shadow.effectDistance=new Vector2(0,-4);
  button.targetGraphic=face;button.transition=UnityEngine.UI.Selectable.Transition.None;
  var label=button.GetComponentInChildren<TMPro.TMP_Text>(true);
  var lr=label.rectTransform;lr.anchorMin=Vector2.zero;lr.anchorMax=Vector2.one;lr.pivot=new Vector2(.5f,.5f);lr.offsetMin=new Vector2(66,8);lr.offsetMax=new Vector2(-42,-8);
  label.text="TEST DRIVE";label.fontSize=30;label.enableAutoSizing=false;label.alignment=TMPro.TextAlignmentOptions.Center;label.raycastTarget=false;
  var feedback=button.GetComponent<TestDriveButtonFeedback>()??button.gameObject.AddComponent<TestDriveButtonFeedback>();
  var serialized=new SerializedObject(feedback);serialized.FindProperty("face").objectReferenceValue=face;serialized.ApplyModifiedPropertiesWithoutUndo();
  Canvas.ForceUpdateCanvases();label.ForceMeshUpdate(true);
  if(label.preferredWidth>lr.rect.width || label.preferredHeight>lr.rect.height)throw new Exception("Label does not fit: "+label.preferredWidth+" x "+label.preferredHeight+" in "+lr.rect);
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  return "Styled TEST DRIVE. Label preferred="+label.preferredWidth+"x"+label.preferredHeight+" available="+lr.rect.size+" callback="+button.onClick.GetPersistentMethodName(0)+" canvas="+button.GetComponentInParent<Canvas>().renderMode;
 }
}
