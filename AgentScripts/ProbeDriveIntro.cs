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
    public static async Task<string> Run(bool anywhere = false)
    {
        string result="not sampled";
        var probe=new GameObject("Intro input check").AddComponent<DriveIntroProbe>();
        probe.sample=()=>
        {
            var intro=UnityEngine.Object.FindAnyObjectByType<TestDriveIntro>();
            var button=intro.transform.Find("TapAnywhereToSkip").GetComponent<UnityEngine.UI.Button>();
            var point=RectTransformUtility.WorldToScreenPoint(null,button.transform.position);
            var pointer=new PointerEventData(EventSystem.current){position=point};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            bool found=hits.Any(h=>h.gameObject==button.gameObject);
            result="Runtime screen="+Screen.width+"x"+Screen.height+", point="+point+", hits="+string.Join(",",hits.Select(h=>h.gameObject.name));
            if(anywhere)
            {
                foreach(var normalized in new[]{new Vector2(.5f,.5f),new Vector2(.03f,.03f),new Vector2(.03f,.97f),new Vector2(.97f,.5f),new Vector2(.2f,.12f)})
                {
                    pointer.position=new Vector2(Screen.width*normalized.x,Screen.height*normalized.y);
                    hits.Clear();EventSystem.current.RaycastAll(pointer,hits);
                    bool covered=hits.Count>0 && hits[0].gameObject.name=="TapAnywhereToSkip";
                    if(!covered)throw new Exception("Tap missed at "+normalized);
                }
                pointer.position=point;
                ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerDownHandler);
                ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerUpHandler);
            }
            if(found)ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
            result+=", skipped="+!intro.IsPlaying;
            if(anywhere)result+=", five screen regions passed, held="+UnityEngine.Object.FindAnyObjectByType<TestDriveMovement>().IsChallengeStopped+
                ", follow="+UnityEngine.Object.FindAnyObjectByType<TestDriveCameraFollow>().enabled;
        };
        for(int i=0;i<3;i++){EditorApplication.Step();await Task.Delay(25);}
        return result;
    }
}
