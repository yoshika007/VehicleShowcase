using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;
using TMPro;
using Object=UnityEngine.Object;

public class CoursePhotoInputProbe:MonoBehaviour
{
    public Action sample;
    void Update(){var action=sample;sample=null;action?.Invoke();Destroy(gameObject);}
}
public static class CheckCoursePhotos
{
    static async Task Frames(int count){for(int i=0;i<count;i++){EditorApplication.Step();await Task.Delay(25);}}
    public static async Task<string> Run()
    {
        await Frames(3);
        var manager=Object.FindAnyObjectByType<VehicleShowcaseManager>();
        if(!Object.FindAnyObjectByType<FreeRideCourseMenu>()){manager.OpenDriveMenu();manager.FreeRun();}
        await Frames(3);
        var menu=Object.FindAnyObjectByType<FreeRideCourseMenu>();string result="not sampled";
        var probe=new GameObject("Course photo input check").AddComponent<CoursePhotoInputProbe>();
        probe.sample=()=>
        {
            var names=new[]{"TestCircuit","LakeTown","MountainPass","HarborStreet"};
            var expected=new[]{TestDriveSelection.CourseKind.TestCircuit,TestDriveSelection.CourseKind.LakeTown,TestDriveSelection.CourseKind.MountainPass,TestDriveSelection.CourseKind.HarborStreet};
            var details=new List<string>();
            for(int i=0;i<names.Length;i++)
            {
                var card=menu.transform.Find("Dialog/"+names[i]+"Course");
                var image=card.Find("CoursePhoto").GetComponent<UnityEngine.UI.Image>();
                if(!image.sprite||image.raycastTarget||!card.Find("Details").gameObject.activeSelf)throw new Exception("Invalid photo card "+names[i]);
                var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,image.transform.position)};
                var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
                if(hits.Count==0||ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)!=card.gameObject)throw new Exception("Photo tap blocked: "+names[i]);
                ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,pointer,ExecuteEvents.pointerClickHandler);
                if(manager.FreeRideCourse!=expected[i])throw new Exception("Wrong selected course: "+names[i]);
                var description=card.Find("Details").GetComponent<TMP_Text>();
                description.ForceMeshUpdate();
                if(description.isTextOverflowing)throw new Exception("Description overflow "+names[i]);
                if(card.Find("CourseLayout"))throw new Exception("Unused map remains on card "+names[i]);
                details.Add(names[i]+"="+image.sprite.name+" / "+description.text.Replace("\n",", "));
            }
            result="Actual runtime screen="+Screen.width+"x"+Screen.height+"; four photo taps selected correct courses; "+string.Join(", ",details);
        };
        await Frames(3);
        if(result=="not sampled")throw new Exception("Runtime card check did not finish; inspect the Console.");
        manager.SetFreeRideNight();await Frames(1);manager.SetFreeRideDay();
        for(int i=0;i<2;i++){manager.BackToDriveModes();manager.FreeRun();await Frames(2);}
        int photos=menu.GetComponentsInChildren<UnityEngine.UI.Image>().Count(image=>image.name=="CoursePhoto");
        if(photos!=4)throw new Exception("Repeated opening duplicated photos: "+photos);
        var overflow=menu.GetComponentsInChildren<TMP_Text>().Where(t=>{t.ForceMeshUpdate();return t.isTextOverflowing;}).Select(t=>t.name).ToArray();
        await Frames(2);
        return result+"; reopen photo count="+photos+"; overflow="+string.Join(",",overflow)+"; frame="+Time.frameCount;
    }
}
