using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

public static class CheckMountainPassTechnicalSection
{
    public static string Metrics()
    {
        var v=GameObject.Find("Mountain Pass asphalt").GetComponent<MeshCollider>().sharedMesh.vertices;
        var p=Enumerable.Range(0,v.Length/2-1).Select(i=>(v[2*i]+v[2*i+1])*.5f).ToArray();
        float radius=10000,width=10000;
        for(int i=2;i<p.Length-2;i++)
        {
            if(p[i].z> -300||p[i].x< -70||p[i].x>170)continue;
            var a=p[i]-p[i-2];var b=p[i+2]-p[i];a.y=b.y=0;
            float angle=Vector3.Angle(a,b)*Mathf.Deg2Rad;
            if(angle>.0001f)radius=Mathf.Min(radius,(a.magnitude+b.magnitude)*.5f/angle);
            var direction=(p[i+1]-p[i-1]).normalized;
            width=Mathf.Min(width,Vector3.Cross(direction,v[2*i+1]-v[2*i]).magnitude);
        }
        return "Minimum centerline radius="+radius.ToString("F1")+"m, minimum perpendicular road width="+width.ToString("F1")+"m";
    }
    public static async Task<string> CorneringRun()
    {
        await Place(140);
        var car=UnityEngine.Object.FindAnyObjectByType<TestDriveMovement>();var rb=car.GetComponent<Rigidbody>();
        var controls=UnityEngine.Object.FindAnyObjectByType<TestDriveTouchControls>();var data=new SerializedObject(controls);
        var gas=(TestDriveTouchButton)data.FindProperty("gas").objectReferenceValue;
        var left=(TestDriveTouchButton)data.FindProperty("left").objectReferenceValue;
        var right=(TestDriveTouchButton)data.FindProperty("right").objectReferenceValue;
        var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){pointerId=74};
        var v=GameObject.Find("Mountain Pass asphalt").GetComponent<MeshCollider>().sharedMesh.vertices;
        var p=Enumerable.Range(0,v.Length/2-1).Select(i=>(v[2*i]+v[2*i+1])*.5f).ToArray();
        rb.linearVelocity=car.transform.forward*7;
        float maxError=0;int steps=0;
        try
        {
            for(;steps<700&&car.transform.position.x>35;steps++)
            {
                int nearest=Enumerable.Range(0,p.Length).OrderBy(i=>(p[i]-car.transform.position).sqrMagnitude).First();
                int ahead=(nearest+5)%p.Length;
                var target=p[ahead]-(v[ahead*2+1]-v[ahead*2]).normalized*2.2f;
                float angle=Vector3.SignedAngle(car.transform.forward,target-car.transform.position,Vector3.up);
                left.Release();right.Release();
                if(angle< -2)left.OnPointerDown(pointer);else if(angle>2)right.OnPointerDown(pointer);
                if(rb.linearVelocity.magnitude<8)gas.OnPointerDown(pointer);else gas.Release();
                maxError=Mathf.Max(maxError,Vector3.Distance(car.transform.position,p[nearest]));
                EditorApplication.Step();await Task.Delay(25);
            }
        }
        finally{controls.ReleaseAll();}
        return "End x="+car.transform.position.x.ToString("F1")+", steps="+steps+", max centerline distance="+maxError.ToString("F1")+", grounded="+car.GetComponentsInChildren<WheelCollider>().Count(w=>w.isGrounded);
    }
    public static async Task<string> Place(float x)
    {
        var v=GameObject.Find("Mountain Pass asphalt").GetComponent<MeshCollider>().sharedMesh.vertices;
        int i=Enumerable.Range(0,v.Length/2-1).Where(j=>v[j*2].z< -300).OrderBy(j=>Mathf.Abs((v[j*2].x+v[j*2+1].x)*.5f-x)).First();
        var p=(v[i*2]+v[i*2+1])*.5f;var next=(v[(i+1)*2]+v[(i+1)*2+1])*.5f;
        var car=UnityEngine.Object.FindAnyObjectByType<TestDriveMovement>();var rb=car.GetComponent<Rigidbody>();
        rb.linearVelocity=Vector3.zero;rb.angularVelocity=Vector3.zero;
        rb.position=p-(v[i*2+1]-v[i*2]).normalized*2.6f+Vector3.up*.5f;rb.rotation=Quaternion.LookRotation(next-p);
        Physics.SyncTransforms();
        var camera=UnityEngine.Object.FindAnyObjectByType<TestDriveCameraFollow>();camera.enabled=true;camera.SnapToTarget();
        for(int frame=0;frame<35;frame++){EditorApplication.Step();await Task.Delay(25);}
        return "x="+x+", grounded="+car.GetComponentsInChildren<WheelCollider>().Count(w=>w.isGrounded)+", position="+car.transform.position;
    }
}
