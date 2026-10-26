using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class CheckHarborWaterfront
{
    public static async Task<int> Frames(int count){for(int i=0;i<count;i++){EditorApplication.Step();await Task.Delay(25);}return Time.frameCount;}
    public static async Task<string> Load(bool night)
    {
        TestDriveSelection.Select(0,lighting:night?TestDriveSelection.TimeOfDay.Night:TestDriveSelection.TimeOfDay.Day,course:TestDriveSelection.CourseKind.HarborStreet);
        SceneManager.LoadScene("HarborStreetScene");await Frames(3);Object.FindAnyObjectByType<TestDriveIntro>()?.Skip();await Frames(2);
        return State();
    }
    public static string State()
    {
        var root=GameObject.Find("Harbor waterfront details");
        if(!root||root.GetComponentsInChildren<Collider>().Length!=0)throw new Exception("Waterfront details must not block driving.");
        var lamps=root.GetComponentsInChildren<Light>();var glass=root.GetComponentsInChildren<Renderer>().Where(r=>r.name=="Quay lantern glass"||r.name=="Pier bollard glass"||r.name=="Pier mast lantern").ToArray();
        if(lamps.Length!=24||glass.Length!=24)throw new Exception("Missing marina lights.");
        bool night=TestDriveSelection.IsNight;
        if(!night&&lamps.Any(l=>l.enabled))throw new Exception("Marina lights enabled during the day.");
        if(glass.Any(r=>r.sharedMaterial.name!=(night?"Warm marina glow":"Lantern glass")))throw new Exception("Incorrect lantern appearance.");
        return (night?"Night":"Day")+": 24 marina lights, nearby enabled="+lamps.Count(l=>l.enabled)+", lantern materials correct, no scenery colliders; frame="+Time.frameCount;
    }
    public static async Task<string> View(bool wide=true)
    {
        Object.FindAnyObjectByType<TestDriveCameraFollow>().enabled=false;
        foreach(var canvas in Object.FindObjectsByType<Canvas>())canvas.enabled=false;
        var camera=Camera.main;
        camera.transform.position=wide?new Vector3(-120,90,-390):new Vector3(-60,14,-269);
        camera.transform.LookAt(wide?new Vector3(15,0,-220):new Vector3(-35,1,-233));camera.fieldOfView=wide?65:58;
        await Frames(15);return State();
    }
}
