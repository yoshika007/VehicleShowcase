using UnityEngine;using UnityEditor;using UnityEditor.Events;using UnityEditor.SceneManagement;using TMPro;using System;
public static class SetupFreeRideCourses
{
 static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
 {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=position;r.sizeDelta=size;return r;}
 static TMP_Text Label(string name,Transform parent,TMP_FontAsset font,string value,float size,Vector2 position,Vector2 dimensions,Color color)
 {var r=Rect(name,parent,position,dimensions);var text=r.gameObject.AddComponent<TextMeshProUGUI>();text.font=font;text.text=value;text.fontSize=size;text.alignment=TextAlignmentOptions.Center;text.color=color;text.raycastTarget=false;return text;}
 static UnityEngine.UI.Button Button(string name,Transform parent,TMP_FontAsset font,string label,Vector2 p,Vector2 size,UnityEngine.Events.UnityAction callback)
 {var r=Rect(name,parent,p,size);var image=r.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=new Color(.08f,.25f,.29f);var button=r.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=image;var nav=button.navigation;nav.mode=UnityEngine.UI.Navigation.Mode.None;button.navigation=nav;UnityEventTools.AddPersistentListener(button.onClick,callback);Label("Label",r,font,label,30,Vector2.zero,size-new Vector2(20,8),Color.white);return button;}
 public static string Run()
 {
  if(EditorApplication.isPlaying)throw new Exception("Stop Play mode");
  var scene=EditorSceneManager.OpenScene("Assets/Scenes/ShowcaseScene.unity");var manager=UnityEngine.Object.FindAnyObjectByType<VehicleShowcaseManager>();var data=new SerializedObject(manager);
  if(data.FindProperty("courseSelectionPanel").objectReferenceValue!=null)throw new Exception("Course picker already exists");
  var modes=(GameObject)data.FindProperty("driveModePanel").objectReferenceValue;var font=modes.GetComponentInChildren<TMP_Text>(true).font;
  var panel=Rect("FreeRideCoursePanel",modes.transform.parent,Vector2.zero,Vector2.zero);panel.anchorMin=Vector2.zero;panel.anchorMax=Vector2.one;panel.offsetMin=panel.offsetMax=Vector2.zero;panel.SetAsLastSibling();panel.gameObject.AddComponent<UnityEngine.UI.Image>().color=new Color(0,0,0,.76f);panel.gameObject.AddComponent<CanvasGroup>();
  var dialog=Rect("Dialog",panel,Vector2.zero,new Vector2(820,670));dialog.gameObject.AddComponent<UnityEngine.UI.Image>().color=new Color(.045f,.08f,.11f,.99f);var outline=dialog.gameObject.AddComponent<UnityEngine.UI.Outline>();outline.effectColor=new Color(.98f,.77f,.25f,.8f);outline.effectDistance=new Vector2(2,-2);
  var gold=new Color(.98f,.77f,.25f);Label("Heading",dialog,font,"CHOOSE YOUR COURSE",42,new Vector2(0,265),new Vector2(760,60),gold);
  Label("Subtitle",dialog,font,"FREE RUN  /  Pick a course to start driving",25,new Vector2(0,211),new Vector2(760,40),new Color(.76f,.84f,.87f));
  var circuit=Button("TestCircuitCourse",dialog,font,"TEST CIRCUIT",new Vector2(-190,35),new Vector2(350,238),manager.TestCircuitRide);
  var lake=Button("LakeTownCourse",dialog,font,"LAKE TOWN",new Vector2(190,35),new Vector2(350,238),manager.LakeTownRide);
  foreach(var b in new[]{circuit,lake}){var label=b.GetComponentInChildren<TMP_Text>();label.rectTransform.anchoredPosition=new Vector2(0,54);label.rectTransform.sizeDelta=new Vector2(326,58);label.fontSize=37;}
  Label("Details",circuit.transform,font,"Original test track\nOval + off-road routes",26,new Vector2(0,-29),new Vector2(320,100),new Color(.8f,.88f,.9f));
  Label("Details",lake.transform,font,"New trial course\nLakeside town + winding roads",26,new Vector2(0,-29),new Vector2(320,100),new Color(.8f,.88f,.9f));
  Label("Trial",lake.transform,font,"TRIAL",20,new Vector2(0,-95),new Vector2(300,30),gold);
  var day=Button("Day",dialog,font,"DAY",new Vector2(-135,-165),new Vector2(240,60),manager.SetFreeRideDay);
  var night=Button("Night",dialog,font,"NIGHT",new Vector2(135,-165),new Vector2(240,60),manager.SetFreeRideNight);
  Button("Back",dialog,font,"BACK",new Vector2(0,-265),new Vector2(260,60),manager.BackToDriveModes);
  var fit=panel.gameObject.AddComponent<DriveModeMenu>();var fitData=new SerializedObject(fit);fitData.FindProperty("dialog").objectReferenceValue=dialog;fitData.ApplyModifiedPropertiesWithoutUndo();
  var view=panel.gameObject.AddComponent<FreeRideCourseMenu>();var viewData=new SerializedObject(view);viewData.FindProperty("manager").objectReferenceValue=manager;viewData.FindProperty("day").objectReferenceValue=day;viewData.FindProperty("night").objectReferenceValue=night;viewData.ApplyModifiedPropertiesWithoutUndo();
  data.FindProperty("courseSelectionPanel").objectReferenceValue=panel.gameObject;data.ApplyModifiedPropertiesWithoutUndo();
  var description=modes.transform.Find("Dialog/FreeRunButton/Description").GetComponent<TMP_Text>();description.text="Choose Test Circuit or Lake Town.";
  panel.gameObject.SetActive(false);EditorSceneManager.SaveScene(scene);return "Free Run now opens course choices with Day/Night and Back; selecting a course starts the guarded driving transition.";
 }
}
