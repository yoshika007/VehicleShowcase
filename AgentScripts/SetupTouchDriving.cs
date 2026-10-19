using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;
public static class SetupTouchDriving
{
 static TMP_FontAsset font;
 static GameObject Node(string name,Transform parent,params Type[] components)
 {
  var go=new GameObject(name,new[]{typeof(RectTransform)}.Concat(components).ToArray());go.layer=5;go.transform.SetParent(parent,false);return go;
 }
 static void Place(RectTransform r,Vector2 anchor,Vector2 pivot,Vector2 position,Vector2 size)
 { r.anchorMin=r.anchorMax=anchor;r.pivot=pivot;r.anchoredPosition=position;r.sizeDelta=size; }
 static TMP_Text Label(string name,Transform parent,string text,float fontSize,Color color)
 {
  var go=Node(name,parent,typeof(TextMeshProUGUI));var r=(RectTransform)go.transform;
  r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(8,6);r.offsetMax=new Vector2(-8,-6);
  var label=go.GetComponent<TextMeshProUGUI>();label.font=font;label.text=text;label.fontSize=fontSize;label.alignment=TextAlignmentOptions.Center;label.color=color;label.raycastTarget=false;return label;
 }
 static UnityEngine.UI.Button Button(string name,Transform parent,Vector2 anchor,Vector2 pivot,Vector2 position,Vector2 size,string text,Color color,Color textColor)
 {
  var go=Node(name,parent,typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button));Place((RectTransform)go.transform,anchor,pivot,position,size);
  var image=go.GetComponent<UnityEngine.UI.Image>();image.color=color;image.raycastTarget=true;
  var outline=go.AddComponent<UnityEngine.UI.Outline>();outline.effectColor=new Color(1,1,1,.22f);outline.effectDistance=new Vector2(1,-1);
  var button=go.GetComponent<UnityEngine.UI.Button>();button.targetGraphic=image;
  var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=Color.white;colors.pressedColor=new Color(.65f,.70f,.75f);colors.selectedColor=Color.white;colors.fadeDuration=.06f;button.colors=colors;
  var nav=button.navigation;nav.mode=UnityEngine.UI.Navigation.Mode.None;button.navigation=nav;
  Label("Label",go.transform,text,30,textColor);return button;
 }
 static void Ref(SerializedObject so,string property,UnityEngine.Object value) => so.FindProperty(property).objectReferenceValue=value;
 public static string Main()
 {
  var scene=SceneManager.GetActiveScene();
  if(EditorApplication.isPlaying||scene.path!="Assets/Scenes/TestTrackScene.unity"||scene.isDirty)throw new Exception("Clean TestTrackScene in Edit mode required.");
  var canvas=UnityEngine.Object.FindObjectsByType<Canvas>().Single(c=>c.name=="TestDriveHUD");
  if(canvas.transform.Find("TouchDrivingControls")!=null)throw new Exception("Touch panel already exists");
  font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Nunito/Nunito-Bold SDF.asset");if(font==null)throw new Exception("Existing HUD font missing");
  var root=Node("TouchDrivingControls",canvas.transform,typeof(TestDriveTouchControls));
  var rr=(RectTransform)root.transform;rr.anchorMin=Vector2.zero;rr.anchorMax=Vector2.one;rr.offsetMin=rr.offsetMax=Vector2.zero;
  var area=Node("SafeArea",root.transform,typeof(CanvasGroup));var ar=(RectTransform)area.transform;ar.anchorMin=Vector2.zero;ar.anchorMax=Vector2.one;ar.offsetMin=ar.offsetMax=Vector2.zero;
  var steering=Node("Steering",area.transform);Place((RectTransform)steering.transform,Vector2.zero,Vector2.zero,new Vector2(36,36),new Vector2(304,160));
  var pedals=Node("Pedals",area.transform);Place((RectTransform)pedals.transform,new Vector2(1,0),new Vector2(1,0),new Vector2(-36,36),new Vector2(416,184));
  Color dark=new Color(.035f,.07f,.085f,.78f), gold=new Color(.98f,.77f,.25f,.94f), light=new Color(.96f,.98f,1f), ink=new Color(.04f,.05f,.06f);
  var left=Button("SteerLeft",steering.transform,Vector2.zero,Vector2.zero,Vector2.zero,new Vector2(144,160),"<\nLEFT",dark,light).gameObject.AddComponent<TestDriveTouchButton>();
  var right=Button("SteerRight",steering.transform,Vector2.zero,Vector2.zero,new Vector2(160,0),new Vector2(144,160),">\nRIGHT",dark,light).gameObject.AddComponent<TestDriveTouchButton>();
  var reverse=Button("Reverse",pedals.transform,Vector2.zero,Vector2.zero,Vector2.zero,new Vector2(112,120),"REV",dark,light).gameObject.AddComponent<TestDriveTouchButton>();
  var brake=Button("Brake",pedals.transform,Vector2.zero,Vector2.zero,new Vector2(128,0),new Vector2(128,160),"BRAKE",new Color(.36f,.12f,.10f,.85f),light).gameObject.AddComponent<TestDriveTouchButton>();
  var gas=Button("Accelerator",pedals.transform,Vector2.zero,Vector2.zero,new Vector2(272,0),new Vector2(144,184),"GAS",gold,ink).gameObject.AddComponent<TestDriveTouchButton>();
  var reset=Button("TouchReset",area.transform,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,36),new Vector2(160,68),"RESET",dark,light);
  reset.GetComponentInChildren<TMP_Text>().fontSize=24;
  var controls=root.GetComponent<TestDriveTouchControls>();var so=new SerializedObject(controls);
  Ref(so,"controls",area.GetComponent<CanvasGroup>());Ref(so,"safeArea",ar);Ref(so,"steeringGroup",steering.transform);Ref(so,"pedalGroup",pedals.transform);Ref(so,"resetButton",reset.transform);
  Ref(so,"left",left);Ref(so,"right",right);Ref(so,"gas",gas);Ref(so,"reverse",reverse);Ref(so,"brake",brake);Ref(so,"speedPanel",canvas.transform.Find("SpeedPanel"));Ref(so,"keyboardResetHint",canvas.transform.Find("ResetHint").gameObject);
  so.ApplyModifiedPropertiesWithoutUndo();UnityEventTools.AddPersistentListener(reset.onClick,controls.ResetVehicle);
  var manager=UnityEngine.Object.FindAnyObjectByType<TestDriveSceneManager>();var managerSo=new SerializedObject(manager);Ref(managerSo,"touchControls",controls);managerSo.ApplyModifiedPropertiesWithoutUndo();
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  return "Added safe-area multi-touch steering, gas, brake, reverse and reset. Existing HUD and EventSystem retained.";
 }
}
