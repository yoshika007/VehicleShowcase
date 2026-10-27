using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using TMPro;
public static class MatchShowcasePanelPalette
{
    static void ColorGraphic(UnityEngine.UI.Graphic graphic,Color color){if(!graphic)return;Undo.RecordObject(graphic,"Match Showcase panel palette");graphic.color=color;EditorUtility.SetDirty(graphic);}
    static void Border(UnityEngine.UI.Outline outline,Color color){if(!outline)return;Undo.RecordObject(outline,"Match Showcase panel border");outline.effectColor=color;EditorUtility.SetDirty(outline);}
    public static string Run()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.name!="ShowcaseScene"||EditorApplication.isPlaying)throw new Exception("Use ShowcaseScene outside Play Mode");
        var dir="/Users/yoshi/Documents/ChatGPT/Unity Project/Artifacts/Showcase/Palette";Directory.CreateDirectory(dir);if(scene.isDirty)EditorSceneManager.SaveScene(scene,dir+"/before-palette.unity",true);
        var all=UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include);var reference=all.Single(t=>t.name=="CourseSelectionPanel");var dialog=reference.Find("Dialog");
        Color background=dialog.GetComponent<UnityEngine.UI.Image>().color,border=dialog.GetComponent<UnityEngine.UI.Outline>().effectColor,heading=dialog.Find("Heading").GetComponent<TMP_Text>().color,body=dialog.Find("TestCircuitCourse/Details").GetComponent<TMP_Text>().color,secondary=dialog.Find("Subtitle").GetComponent<TMP_Text>().color,card=dialog.Find("TestCircuitCourse").GetComponent<UnityEngine.UI.Image>().color,action=dialog.Find("Back").GetComponent<UnityEngine.UI.Image>().color;
        var states=dialog.Find("Back").GetComponent<UnityEngine.UI.Button>().colors;
        foreach(string name in new[]{"DriveModePanel","TimeOfDayPanel","ChallengeSelectionPanel"})
        {
            var panel=all.Single(t=>t.name==name);ColorGraphic(panel.GetComponent<UnityEngine.UI.Image>(),reference.GetComponent<UnityEngine.UI.Image>().color);var content=panel.Find("Dialog");ColorGraphic(content.GetComponent<UnityEngine.UI.Image>(),background);Border(content.GetComponent<UnityEngine.UI.Outline>(),border);
            foreach(var button in content.GetComponentsInChildren<UnityEngine.UI.Button>(true)){ColorGraphic(button.targetGraphic,button.name=="BackButton"?action:card);var optionBorder=button.GetComponent<UnityEngine.UI.Outline>();Border(optionBorder,border);if(optionBorder)optionBorder.enabled=false;Undo.RecordObject(button,"Match Showcase button tint states");button.colors=states;EditorUtility.SetDirty(button);}
            foreach(var text in content.GetComponentsInChildren<TMP_Text>(true))ColorGraphic(text,text.name=="Heading"?heading:text.name=="Description"?body:Color.white);
        }
        var info=all.Single(t=>t.name=="VehicleInfoPanel");ColorGraphic(info.GetComponent<UnityEngine.UI.Image>(),background);
        var infoBorder=info.GetComponent<UnityEngine.UI.Outline>();if(!infoBorder)infoBorder=Undo.AddComponent<UnityEngine.UI.Outline>(info.gameObject);Border(infoBorder,border);infoBorder.effectDistance=dialog.GetComponent<UnityEngine.UI.Outline>().effectDistance;infoBorder.useGraphicAlpha=true;
        foreach(var text in info.GetComponentsInChildren<TMP_Text>(true))ColorGraphic(text,text.name=="VehicleNameText"?heading:text.name=="DescriptionText"?body:secondary);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return "Vehicle Info, Drive Mode, Time of Day and Challenge Selection matched to CourseSelectionPanel: navy panels, gold headings/borders, teal buttons, white labels and pale descriptions. Paint swatches and layout preserved.";
    }
}
