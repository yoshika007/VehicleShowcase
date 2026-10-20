using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;
using Object=UnityEngine.Object;

public static class SetupDriveModes
{
    static RectTransform Rect(string name,Transform parent,Vector2 anchor,Vector2 pivot,Vector2 position,Vector2 size)
    {
        var r=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=anchor;r.pivot=pivot;r.anchoredPosition=position;r.sizeDelta=size;return r;
    }
    static TMP_Text Text(string name,Transform parent,TMP_FontAsset font,string value,float fontSize,Vector2 position,Vector2 dimensions,Color color)
    {
        var r=Rect(name,parent,new Vector2(.5f,1),new Vector2(.5f,1),position,dimensions);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.fontSize=fontSize;t.text=value;t.color=color;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;return t;
    }
    static UnityEngine.UI.Button Choice(string name,Transform parent,TMP_FontAsset font,string title,string description,float top,Color color,Color ink,UnityEngine.Events.UnityAction callback)
    {
        var r=Rect(name,parent,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-top),new Vector2(520,104));
        var face=r.gameObject.AddComponent<UnityEngine.UI.Image>();face.color=color;
        var outline=r.gameObject.AddComponent<UnityEngine.UI.Outline>();outline.effectColor=new Color(.98f,.77f,.25f,.55f);outline.effectDistance=new Vector2(1,-1);
        var b=r.gameObject.AddComponent<UnityEngine.UI.Button>();b.targetGraphic=face;var tint=b.colors;tint.normalColor=tint.highlightedColor=tint.selectedColor=Color.white;tint.pressedColor=new Color(.75f,.75f,.75f);tint.fadeDuration=.08f;b.colors=tint;
        Text("Title",r,font,title,26,new Vector2(0,-14),new Vector2(500,36),ink);Text("Description",r,font,description,18,new Vector2(0,-58),new Vector2(500,30),ink);
        UnityEventTools.AddPersistentListener(b.onClick,callback);return b;
    }
    public static string Main()
    {
        if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean Edit mode required");
        if(SceneManager.GetActiveScene().path!="Assets/Scenes/ShowcaseScene.unity")EditorSceneManager.OpenScene("Assets/Scenes/ShowcaseScene.unity");
        if(GameObject.Find("DriveModePanel")!=null)throw new Exception("Mode menu already exists");
        var manager=Object.FindAnyObjectByType<VehicleShowcaseManager>();var drive=GameObject.Find("TestDriveButton");if(drive==null)throw new Exception("Missing original drive button");
        var font=drive.GetComponentInChildren<TMP_Text>().font;drive.name="DriveButton";foreach(var t in drive.GetComponentsInChildren<TMP_Text>())t.text=t.text.Replace("TEST DRIVE","DRIVE");
        var driveButton=drive.GetComponent<UnityEngine.UI.Button>();driveButton.onClick=new UnityEngine.UI.Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(driveButton.onClick,manager.OpenDriveMenu);
        var root=Rect("DriveModePanel",drive.transform.parent,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,Vector2.zero);root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;root.SetAsLastSibling();
        var backdrop=root.gameObject.AddComponent<UnityEngine.UI.Image>();backdrop.color=new Color(0,0,0,.72f);backdrop.raycastTarget=true;root.gameObject.AddComponent<CanvasGroup>();
        var dialog=Rect("Dialog",root,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(600,520));var background=dialog.gameObject.AddComponent<UnityEngine.UI.Image>();background.color=new Color(.045f,.065f,.085f,.99f);
        var edge=dialog.gameObject.AddComponent<UnityEngine.UI.Outline>();edge.effectColor=new Color(.98f,.77f,.25f,.7f);edge.effectDistance=new Vector2(2,-2);
        var gold=new Color(.98f,.77f,.25f);Text("Heading",dialog,font,"CHOOSE DRIVE MODE",30,new Vector2(0,-28),new Vector2(550,44),gold);Text("Description",dialog,font,"Choose how you'd like to drive.",18,new Vector2(0,-80),new Vector2(550,30),new Color(.73f,.81f,.82f));
        Choice("FreeRunButton",dialog,font,"FREE RUN","Explore the circuit and off-road routes.",136,new Color(.07f,.25f,.28f),Color.white,manager.FreeRun);
        Choice("ChallengeButton",dialog,font,"CHALLENGE","Clean lap: aim for bronze, silver or gold.",256,gold,new Color(.035f,.045f,.055f),manager.Challenge);
        var back=Rect("BackButton",dialog,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,28),new Vector2(240,60));var backFace=back.gameObject.AddComponent<UnityEngine.UI.Image>();backFace.color=new Color(.11f,.15f,.18f);var backButton=back.gameObject.AddComponent<UnityEngine.UI.Button>();backButton.targetGraphic=backFace;var colors=backButton.colors;colors.normalColor=colors.highlightedColor=colors.selectedColor=Color.white;colors.pressedColor=new Color(.75f,.75f,.75f);backButton.colors=colors;Text("Label",back,font,"BACK",24,new Vector2(0,-4),new Vector2(220,52),Color.white);UnityEventTools.AddPersistentListener(backButton.onClick,manager.CloseDriveMenu);
        var menu=root.gameObject.AddComponent<DriveModeMenu>();var data=new SerializedObject(menu);data.FindProperty("dialog").objectReferenceValue=dialog;data.ApplyModifiedPropertiesWithoutUndo();var so=new SerializedObject(manager);so.FindProperty("driveModePanel").objectReferenceValue=root.gameObject;so.ApplyModifiedPropertiesWithoutUndo();root.gameObject.SetActive(false);
        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);EditorSceneManager.SaveScene(manager.gameObject.scene);AssetDatabase.SaveAssets();return "DRIVE now opens Free Run / Challenge / Back. Dialog fits the safe area; existing showroom layout is unchanged.";
    }
}
