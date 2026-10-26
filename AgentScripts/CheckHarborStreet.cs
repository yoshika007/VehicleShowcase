using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using TMPro;
using Object=UnityEngine.Object;

public static class CheckHarborStreet
{
    public static async Task<int> Frames(int count)
    {
        for(int i=0;i<count;i++){EditorApplication.Step();await Task.Delay(25);}return Time.frameCount;
    }
    public static string Geometry()
    {
        Physics.SyncTransforms();
        var road=GameObject.Find("Harbor Street asphalt").GetComponent<MeshCollider>().sharedMesh;
        var v=road.vertices;int count=v.Length/2-1;
        int missing=0,blocked=0;float grade=0,radius=float.MaxValue;var errors=new List<string>();
        for(int i=0;i<count;i++)
        {
            Vector3 p=(v[i*2]+v[i*2+1])*.5f;
            var next=(v[((i+1)%count)*2]+v[((i+1)%count)*2+1])*.5f;
            var prev=(v[((i+count-1)%count)*2]+v[((i+count-1)%count)*2+1])*.5f;
            var d=next-p;grade=Mathf.Max(grade,Mathf.Abs(d.y)/new Vector2(d.x,d.z).magnitude);
            float turn=Vector2.Angle(new Vector2(p.x-prev.x,p.z-prev.z),new Vector2(d.x,d.z))*Mathf.Deg2Rad;
            if(turn>.0001f)radius=Mathf.Min(radius,new Vector2(d.x,d.z).magnitude/turn);
            foreach(float offset in new[]{-4.4f,0,4.4f})
            {
                var location=p+(v[2*i+1]-v[2*i]).normalized*offset;
                var hits=Physics.RaycastAll(location+Vector3.up*2,Vector3.down,4).OrderBy(h=>h.distance).ToArray();
                if(hits.Length==0){missing++;if(errors.Count<10)errors.Add("missing "+i);}
                else if(hits[0].collider.name!="Harbor Street asphalt")
                {blocked++;if(errors.Count<10)errors.Add(hits[0].collider.name+" at "+i);}
            }
        }
        return "samples="+count*3+", missing="+missing+", blocked="+blocked+", maxGrade="+grade.ToString("P1")+", minimum radius="+radius.ToString("F1")+"m; "+string.Join("; ",errors);
    }
    public static async Task<string> Menu()
    {
        var manager=Object.FindAnyObjectByType<VehicleShowcaseManager>();manager.OpenDriveMenu();manager.FreeRun();await Frames(3);
        var menu=Object.FindAnyObjectByType<FreeRideCourseMenu>();
        menu.transform.Find("Dialog/HarborStreetCourse").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();await Frames(2);
        var labels=menu.GetComponentsInChildren<TMP_Text>();
        foreach(var t in labels)t.ForceMeshUpdate();
        return "selected="+manager.FreeRideCourse+", overflow="+string.Join(",",labels.Where(t=>t.isTextOverflowing).Select(t=>t.name+":"+t.text))+
            ", cards="+menu.transform.Find("Dialog").GetComponentsInChildren<UnityEngine.UI.Button>().Length;
    }
    public static async Task<string> Drive()
    {
        Object.FindAnyObjectByType<FreeRideCourseMenu>().transform.Find("Dialog/Go").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        for(int i=0;i<150;i++){await Frames(1);if(SceneManager.GetActiveScene().name=="HarborStreetScene")break;}
        await Frames(3);return State();
    }
    public static string State()
    {
        var intro=Object.FindAnyObjectByType<TestDriveIntro>();var car=Object.FindAnyObjectByType<TestDriveMovement>();
        return "scene="+SceneManager.GetActiveScene().name+", frame="+Time.frameCount+", intro="+(intro!=null)+", held="+(car!=null&&car.IsChallengeStopped)+", car="+(car?car.name:"none")+
            ", title="+(intro?intro.GetComponentsInChildren<TMP_Text>().First(t=>t.name=="IntroTitle").text:"-");
    }
    public static async Task<string> LoadNight()
    {
        TestDriveSelection.Select(0,lighting:TestDriveSelection.TimeOfDay.Night,course:TestDriveSelection.CourseKind.HarborStreet,vehicleName:"CITYLINE");
        SceneManager.LoadScene("HarborStreetScene");await Frames(3);
        var intro=Object.FindAnyObjectByType<TestDriveIntro>();if(intro)intro.Skip();await Frames(3);return State();
    }
    public static async Task<string> Place(float x,float z,bool driver=false)
    {
        var v=GameObject.Find("Harbor Street asphalt").GetComponent<MeshCollider>().sharedMesh.vertices;
        int n=v.Length/2-1;
        int i=Enumerable.Range(0,n).OrderBy(j=>new Vector2((v[j*2].x+v[j*2+1].x)*.5f-x,(v[j*2].z+v[j*2+1].z)*.5f-z).sqrMagnitude).First();
        var p=(v[i*2]+v[i*2+1])*.5f;var next=(v[((i+1)%n)*2]+v[((i+1)%n)*2+1])*.5f;
        var car=Object.FindAnyObjectByType<TestDriveMovement>();var rb=car.GetComponent<Rigidbody>();
        rb.linearVelocity=Vector3.zero;rb.angularVelocity=Vector3.zero;rb.position=p-(v[i*2+1]-v[i*2]).normalized*2+Vector3.up*.5f;rb.rotation=Quaternion.LookRotation(next-p);
        Physics.SyncTransforms();var follow=Object.FindAnyObjectByType<TestDriveCameraFollow>();follow.enabled=true;
        if(driver!=follow.IsDriverView)follow.ToggleView();follow.SnapToTarget();await Frames(25);
        return "road index="+i+", grounded="+car.GetComponentsInChildren<WheelCollider>().Count(w=>w.isGrounded)+", position="+car.transform.position+", driver="+follow.IsDriverView;
    }
    public static string Overview()
    {
        Object.FindAnyObjectByType<TestDriveCameraFollow>().enabled=false;var camera=Camera.main;
        camera.transform.position=new Vector3(-410,520,-540);camera.transform.LookAt(new Vector3(10,0,10));camera.fieldOfView=47;return "Harbor overview";
    }
    public static async Task<string> CorneringRun(int carIndex=0)
    {
        TestDriveSelection.Select(carIndex,course:TestDriveSelection.CourseKind.HarborStreet);
        SceneManager.LoadScene("HarborStreetScene");await Frames(3);
        Object.FindAnyObjectByType<TestDriveIntro>()?.Skip();await Frames(2);
        await Place(4,125);
        var car=Object.FindAnyObjectByType<TestDriveMovement>();var rb=car.GetComponent<Rigidbody>();
        var controls=Object.FindAnyObjectByType<TestDriveTouchControls>();var data=new SerializedObject(controls);
        var gas=(TestDriveTouchButton)data.FindProperty("gas").objectReferenceValue;
        var left=(TestDriveTouchButton)data.FindProperty("left").objectReferenceValue;
        var right=(TestDriveTouchButton)data.FindProperty("right").objectReferenceValue;
        var reverse=(TestDriveTouchButton)data.FindProperty("reverse").objectReferenceValue;
        var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){pointerId=84};
        var v=GameObject.Find("Harbor Street asphalt").GetComponent<MeshCollider>().sharedMesh.vertices;
        var points=Enumerable.Range(0,v.Length/2-1).Select(i=>(v[i*2]+v[i*2+1])*.5f).ToArray();
        int start=Enumerable.Range(0,points.Length).OrderBy(i=>(points[i]-car.transform.position).sqrMagnitude).First();
        rb.position=points[start]+Vector3.up*.4f;rb.linearVelocity=car.transform.forward*3;
        float error=0;int advance=0,steps=0;
        try
        {
            for(;steps<900 && advance<48;steps++)
            {
                int nearest=Enumerable.Range(0,points.Length).OrderBy(i=>(points[i]-car.transform.position).sqrMagnitude).First();
                advance=(nearest-start+points.Length)%points.Length;
                if(advance>points.Length/2)advance=0;
                var target=points[(nearest+4)%points.Length];
                float angle=Vector3.SignedAngle(car.transform.forward,target-car.transform.position,Vector3.up);
                left.Release();right.Release();gas.Release();reverse.Release();
                if(angle< -2)left.OnPointerDown(pointer);else if(angle>2)right.OnPointerDown(pointer);
                if(rb.linearVelocity.magnitude<4.3f)gas.OnPointerDown(pointer);
                else if(rb.linearVelocity.magnitude>5){gas.OnPointerDown(pointer);reverse.OnPointerDown(pointer);}
                error=Mathf.Max(error,new Vector2(car.transform.position.x-points[nearest].x,car.transform.position.z-points[nearest].z).magnitude);
                await Frames(1);
            }
        }
        finally{controls.ReleaseAll();}
        return "vehicle="+car.name+", route samples advanced="+advance+", steps="+steps+", max lateral error="+error.ToString("F2")+
            "m, wheels grounded="+car.GetComponentsInChildren<WheelCollider>().Count(w=>w.isGrounded)+", speed="+rb.linearVelocity.magnitude.ToString("F1");
    }
    public static async Task<string> Regression()
    {
        TestDriveSelection.Select(1,course:TestDriveSelection.CourseKind.HarborStreet);
        SceneManager.LoadScene("HarborStreetScene");await Frames(3);Object.FindAnyObjectByType<TestDriveIntro>()?.Skip();await Frames(2);
        string hairpin=await Place(17,99);string tunnel=await Place(195,20,true);
        var car=Object.FindAnyObjectByType<TestDriveMovement>();car.ResetVehicle();await Frames(30);
        string reset="reset count="+car.ResetCount+", position="+car.transform.position+", grounded="+car.GetComponentsInChildren<WheelCollider>().Count(w=>w.isGrounded);
        Object.FindAnyObjectByType<TestDriveSceneManager>().ReturnToShowroom();
        for(int i=0;i<100;i++){await Frames(1);if(SceneManager.GetActiveScene().name=="ShowcaseScene")break;}
        await Frames(3);
        return "Pickup hairpin: "+hairpin+"; tunnel: "+tunnel+"; "+reset+"; returned to="+SceneManager.GetActiveScene().name;
    }
    public static async Task<string> Hero()
    {
        TestDriveSelection.Select(0,course:TestDriveSelection.CourseKind.HarborStreet);
        SceneManager.LoadScene("HarborStreetScene");await Frames(3);Object.FindAnyObjectByType<TestDriveIntro>()?.Skip();await Frames(2);
        Object.FindAnyObjectByType<TestDriveCameraFollow>().enabled=false;
        foreach(var canvas in Object.FindObjectsByType<Canvas>())canvas.enabled=false;
        Camera.main.transform.position=new Vector3(-180,66,-283);Camera.main.transform.LookAt(new Vector3(-20,12,-143));Camera.main.fieldOfView=56;
        await Frames(2);return "Harbor vista ready";
    }
}
