using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEditor;

public class DriveIntroProbe : MonoBehaviour
{
    public Action sample;
    private void Update(){var action=sample;sample=null;action?.Invoke();Destroy(gameObject);}
}
public static class ProbeDriveIntro
{
    public static async Task<string> Run()
    {
        string result="not sampled";
        var probe=new GameObject("Intro input check").AddComponent<DriveIntroProbe>();
        probe.sample=()=>
        {
            var intro=UnityEngine.Object.FindAnyObjectByType<TestDriveIntro>();
            var button=intro.transform.Find("IntroSafeArea/SkipIntro").GetComponent<UnityEngine.UI.Button>();
            var point=RectTransformUtility.WorldToScreenPoint(null,button.transform.position);
            var pointer=new PointerEventData(EventSystem.current){position=point};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            bool found=hits.Any(h=>h.gameObject==button.gameObject);
            result="Runtime screen="+Screen.width+"x"+Screen.height+", point="+point+", hits="+string.Join(",",hits.Select(h=>h.gameObject.name));
            if(found)ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
            result+=", skipped="+!intro.IsPlaying;
        };
        for(int i=0;i<3;i++){EditorApplication.Step();await Task.Delay(25);}
        return result;
    }
}
