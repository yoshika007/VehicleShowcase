using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using TMPro;

public static class CheckMountainPass
{
    public static async Task<int> Frames(int count)
    {
        for(int i=0;i<count;i++)
        {
            EditorApplication.Step();
            await Task.Delay(25);
        }
        return Time.frameCount;
    }
    public static async Task<string> Menu()
    {
        await Frames(3);
        var m=UnityEngine.Object.FindAnyObjectByType<VehicleShowcaseManager>();
        if(UnityEngine.Object.FindAnyObjectByType<FreeRideCourseMenu>()==null){m.OpenDriveMenu();m.FreeRun();}
        var panel=UnityEngine.Object.FindAnyObjectByType<FreeRideCourseMenu>();
        panel.transform.Find("Dialog/MountainPassCourse").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        await Frames(3);
        var overflows=panel.GetComponentsInChildren<TMP_Text>().Where(t=> {t.ForceMeshUpdate();return t.isTextOverflowing;}).Select(t=>t.name).ToArray();
        return "Course="+m.FreeRideCourse+", overflows="+string.Join(",",overflows)+", frame="+Time.frameCount;
    }
    public static async Task<string> Drive()
    {
        var p=UnityEngine.Object.FindAnyObjectByType<FreeRideCourseMenu>();
        p.transform.Find("Dialog/Go").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        await Frames(140);
        return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name+" frame="+Time.frameCount;
    }
    public static string Geometry()
    {
        var road=GameObject.Find("Mountain Pass asphalt").GetComponent<MeshCollider>().sharedMesh;
        var vertices=road.vertices;var centers=Enumerable.Range(0,vertices.Length/2-1).Select(i=>(vertices[2*i]+vertices[2*i+1])*.5f).ToArray();
        Physics.SyncTransforms();
        int blocked=0,missing=0;float maxGrade=0,minNormal=1;var problems=new List<string>();
        for(int i=0;i<centers.Length;i++)
        {
            var d=centers[(i+1)%centers.Length]-centers[i];
            maxGrade=Mathf.Max(maxGrade,Mathf.Abs(d.y)/new Vector2(d.x,d.z).magnitude);
            foreach(float offset in new[]{-4.5f,0,4.5f})
            {
                var right=(vertices[2*i+1]-vertices[2*i]).normalized;
                var p=centers[i]+right*offset;
                var hits=Physics.RaycastAll(p+Vector3.up*2,Vector3.down,4).OrderBy(h=>h.distance).ToArray();
                if(hits.Length==0){missing++;if(problems.Count<10)problems.Add("missing "+i);}
                else if(hits[0].collider.name!="Mountain Pass asphalt"){blocked++;if(problems.Count<10)problems.Add(hits[0].collider.name+" at "+i);}
                else minNormal=Mathf.Min(minNormal,hits[0].normal.y);
            }
        }
        return "Samples="+centers.Length*3+", missing="+missing+", blocked="+blocked+", maxGrade="+maxGrade.ToString("P1")+", minNormalY="+minNormal+", "+string.Join(",",problems);
    }
    public static async Task<string> HairpinsAndDescent()
    {
        var results=new List<string>();
        foreach(int index in new[]{518,727,937,1313})results.Add(await PlaceAt(index));
        return string.Join("; ",results);
    }
    public static async Task<string> UphillAndReset()
    {
        await PlaceAt(580);
        var car=UnityEngine.Object.FindAnyObjectByType<TestDriveMovement>();
        var controls=UnityEngine.Object.FindAnyObjectByType<TestDriveTouchControls>();
        var gas=(TestDriveTouchButton)new SerializedObject(controls).FindProperty("gas").objectReferenceValue;
        var start=car.transform.position;
        gas.OnPointerDown(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){pointerId=42});
        await Frames(100);
        gas.Release();
        float progress=Vector3.Distance(start,car.transform.position),speed=car.GetComponent<Rigidbody>().linearVelocity.magnitude;
        car.ResetVehicle();await Frames(25);
        return "Uphill displacement="+progress+", speed="+speed+", resetCount="+car.ResetCount+", resetPosition="+car.transform.position;
    }
    public static string Overview()
    {
        var follow=UnityEngine.Object.FindAnyObjectByType<TestDriveCameraFollow>();follow.enabled=false;
        var camera=Camera.main;camera.transform.position=new Vector3(700,750,-780);camera.transform.LookAt(new Vector3(70,35,-15));camera.farClipPlane=2400;camera.fieldOfView=48;
        return "Overview camera positioned";
    }
    public static async Task<string> PlaceAt(int index)
    {
        var road=GameObject.Find("Mountain Pass asphalt").GetComponent<MeshCollider>().sharedMesh;
        var vertices=road.vertices;int count=vertices.Length/2-1;index%=count;
        var p=(vertices[index*2]+vertices[index*2+1])*.5f;
        var next=(vertices[((index+1)%count)*2]+vertices[((index+1)%count)*2+1])*.5f;
        var vehicle=UnityEngine.Object.FindAnyObjectByType<TestDriveMovement>();
        var rb=vehicle.GetComponent<Rigidbody>();
        rb.linearVelocity=Vector3.zero;rb.angularVelocity=Vector3.zero;
        rb.position=p-(vertices[index*2+1]-vertices[index*2]).normalized*2.6f+Vector3.up*.5f;
        rb.rotation=Quaternion.LookRotation(next-p);
        Physics.SyncTransforms();
        var follow=UnityEngine.Object.FindAnyObjectByType<TestDriveCameraFollow>();follow.enabled=true;follow.SnapToTarget();
        await Frames(35);
        int grounded=vehicle.GetComponentsInChildren<WheelCollider>().Count(w=>w.isGrounded);
        return "index="+index+", wheels grounded="+grounded+", position="+vehicle.transform.position+", speed="+rb.linearVelocity.magnitude;
    }
}
