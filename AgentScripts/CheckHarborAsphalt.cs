using System;
using System.Linq;
using UnityEngine;
using UnityEditor;

public static class CheckHarborAsphalt
{
    public static string Inspect()
    {
        var road=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Environments/HarborStreet/Harbor Street asphalt.asset");
        if(road.uv.Length!=road.vertexCount||road.colors.Length!=road.vertexCount)throw new Exception("Missing street texture coordinates.");
        var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Environments/HarborStreet/City asphalt.mat");
        var messages=ShaderUtil.GetShaderMessages(material.shader);if(messages.Any(m=>m.severity.ToString()=="Error"))throw new Exception("Asphalt shader failed.");
        var details=GameObject.Find("Street asphalt details");
        if(!details||details.GetComponentsInChildren<Collider>().Length!=0)throw new Exception("Street details must stay cosmetic.");
        return "Asphalt shader="+material.shader.name+", shader messages="+messages.Length+", UV/colors="+road.vertexCount+", utility-detail colliders=0";
    }
    public static string ViewCover()
    {
        var v=GameObject.Find("Harbor Street asphalt").GetComponent<MeshCollider>().sharedMesh.vertices;
        var center=(v[0]+v[1])*.5f;var next=(v[2]+v[3])*.5f;
        var forward=(next-center).normalized;var side=(v[1]-v[0]).normalized;var up=Vector3.Cross(forward,side).normalized;
        var cover=center+side*3.75f;
        UnityEngine.Object.FindAnyObjectByType<TestDriveCameraFollow>().enabled=false;
        foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>())canvas.enabled=false;
        Camera.main.transform.position=cover+up*2.8f-forward*3.3f-side*1.4f;Camera.main.transform.LookAt(cover+forward*.3f);Camera.main.fieldOfView=55;
        return "Close street detail view";
    }
}
