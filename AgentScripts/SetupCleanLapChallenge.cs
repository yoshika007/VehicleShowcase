using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;
using Object=UnityEngine.Object;

public static class SetupCleanLapChallenge
{
    static void Set(Object target,string name,Object value){var serialized=new SerializedObject(target);serialized.FindProperty(name).objectReferenceValue=value;serialized.ApplyModifiedPropertiesWithoutUndo();}
    static RectTransform Rect(string name,Transform parent,Vector2 anchor,Vector2 pivot,Vector2 position,Vector2 size)
    {
        var rect=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=anchor;rect.pivot=pivot;rect.anchoredPosition=position;rect.sizeDelta=size;return rect;
    }
    static TMP_Text Text(string name,Transform parent,TMP_FontAsset font,float size,Vector2 position,Vector2 dimensions,string content,Color tint)
    {
        var rect=Rect(name,parent,new Vector2(.5f,1),new Vector2(.5f,1),position,dimensions);var text=rect.gameObject.AddComponent<TextMeshProUGUI>();text.font=font;text.fontSize=size;text.text=content;text.color=tint;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;return text;
    }
    public static string Main()
    {
        if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean Edit mode required");
        if(SceneManager.GetActiveScene().path!="Assets/Scenes/TestTrackScene.unity")EditorSceneManager.OpenScene("Assets/Scenes/TestTrackScene.unity");
        if(Object.FindAnyObjectByType<TestDriveCleanLapChallenge>()!=null)throw new Exception("Challenge already configured");
        var manager=Object.FindAnyObjectByType<TestDriveSceneManager>();var controls=Object.FindAnyObjectByType<TestDriveTouchControls>();var safe=GameObject.Find("DrivingSafeArea").transform;
        var road=GameObject.Find("Asphalt — 12m wide");var checkers=GameObject.Find("Start finish markings").GetComponentsInChildren<Renderer>().Where(r=>r.name=="Checker").ToArray();
        if(checkers.Length!=24||road.GetComponent<MeshCollider>()==null)throw new Exception("Unexpected track geometry");
        var root=new GameObject("Clean Lap Challenge");var challenge=root.AddComponent<TestDriveCleanLapChallenge>();
        var line=new GameObject("Start Finish Gate").transform;line.SetParent(root.transform,false);line.position=checkers.Aggregate(Vector3.zero,(v,r)=>v+r.bounds.center)/checkers.Length;line.rotation=Quaternion.LookRotation(road.transform.TransformDirection(Vector3.right),Vector3.up);
        Set(challenge,"circuit",road.GetComponent<MeshFilter>());Set(challenge,"asphalt",road.GetComponent<MeshCollider>());Set(challenge,"legalCurbs",GameObject.Find("Red and white curbs").transform);Set(challenge,"startLine",line);Set(manager,"cleanLapChallenge",challenge);
        var panel=Rect("CleanLapPanel",safe,new Vector2(0,1),new Vector2(0,1),new Vector2(36,-128),new Vector2(320,204));
        var face=panel.gameObject.AddComponent<UnityEngine.UI.Image>();face.color=new Color(.055f,.075f,.10f,.94f);face.raycastTarget=false;
        var edge=panel.gameObject.AddComponent<UnityEngine.UI.Outline>();edge.effectColor=new Color(.98f,.77f,.25f,.65f);edge.effectDistance=new Vector2(1,-1);
        var font=GameObject.Find("BackToShowroomButton").GetComponentInChildren<TMP_Text>().font;var gold=new Color(.98f,.77f,.25f);
        var heading=Text("Heading",panel,font,20,new Vector2(0,-8),new Vector2(300,28),"CLEAN LAP",gold);
        var clock=Text("Clock",panel,font,32,new Vector2(0,-36),new Vector2(300,44),"00:00.00",Color.white);
        var status=Text("Status",panel,font,16,new Vector2(0,-82),new Vector2(300,24),"Asphalt + curbs. No resets.",Color.white);
        var targets=Text("MedalTargets",panel,font,14,new Vector2(0,-108),new Vector2(300,40),"GOLD 35s / SILVER 50s\nBRONZE: FINISH CLEAN",new Color(.73f,.81f,.82f));
        var buttonRect=Rect("CleanLapAction",panel,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,8),new Vector2(296,44));var buttonFace=buttonRect.gameObject.AddComponent<UnityEngine.UI.Image>();buttonFace.color=gold;
        var button=buttonRect.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=buttonFace;var tint=button.colors;tint.normalColor=tint.highlightedColor=tint.selectedColor=Color.white;tint.pressedColor=new Color(.75f,.75f,.75f);tint.fadeDuration=.08f;button.colors=tint;
        var actionLabel=Text("Label",buttonRect,font,22,new Vector2(0,0),new Vector2(280,44),"START LAP",new Color(.035f,.045f,.055f));
        UnityEventTools.AddPersistentListener(button.onClick,challenge.HandleAction);
        var view=panel.gameObject.AddComponent<TestDriveCleanLapView>();Set(view,"heading",heading);Set(view,"clock",clock);Set(view,"status",status);Set(view,"targets",targets);Set(view,"actionLabel",actionLabel);Set(view,"action",button);Set(challenge,"view",view);Set(controls,"cleanLapPanel",panel);
        var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();return "Clean-lap challenge installed at the existing start line. Optional countdown, ordered lap progress, off-track/reset rejection, three medals, per-car saved bests and safe-area HUD.";
    }
}
