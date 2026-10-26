using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class FitHarborSignBoards
{
    public static string View(int index)
    {
        var names=new[]{"HARBOR STREET","HOTEL HAIRPIN","SEASIDE TUNNEL","CHICANE / SLOW"};
        var label=GameObject.Find("Harbor Street Course").GetComponentsInChildren<TMP_Text>().Single(t=>t.text==names[index]&&Mathf.Abs(t.fontSize-5)<.001f);
        UnityEngine.Object.FindAnyObjectByType<TestDriveCameraFollow>().enabled=false;
        var forward=label.transform.forward;var side=Vector3.Cross(Vector3.up,forward).normalized;
        var camera=Camera.main;camera.transform.position=label.transform.position-forward*11+side*3+Vector3.up;
        camera.transform.LookAt(label.transform.position-Vector3.up*.7f);camera.fieldOfView=52;
        label.ForceMeshUpdate();if(label.isTextOverflowing||Mathf.Abs(label.fontSize-5)>.001f)throw new Exception("Label changed or clipped.");
        return names[index]+": text size="+label.fontSize+", no text overflow";
    }
    public static string Run() => Fit(new[]{"HARBOR STREET","HOTEL HAIRPIN","SEASIDE TUNNEL","CHICANE / SLOW"});
    public static string Chicane() => Fit(new[]{"CHICANE / SLOW"});
    static string Fit(string[] names)
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(EditorApplication.isPlaying||scene.isDirty||scene.name!="HarborStreetScene")throw new Exception("Use saved Harbor Street outside Play Mode.");
        var course=GameObject.Find("Harbor Street Course");
        var labels=course.GetComponentsInChildren<TMP_Text>().Where(t=>names.Contains(t.text)&&Mathf.Abs(t.fontSize-5)<.001f).ToArray();
        if(labels.Length!=names.Length)throw new Exception("Missing roadside labels.");
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Environments/HarborStreet/Harbor navy scenery.asset");
        var vertices=mesh.vertices;var messages=new List<string>();
        foreach(var label in labels)
        {
            var center=label.transform.position+label.transform.forward*.12f;
            var inverse=Quaternion.Inverse(label.transform.rotation);int match=-1;float oldWidth=0;
            for(int i=0;i<vertices.Length;i+=24)
            {
                var bounds=new Bounds(vertices[i],Vector3.zero);for(int j=1;j<24;j++)bounds.Encapsulate(vertices[i+j]);
                if((bounds.center-center).sqrMagnitude>.0001f)continue;
                var local=new Bounds(inverse*(vertices[i]-center),Vector3.zero);
                for(int j=1;j<24;j++)local.Encapsulate(inverse*(vertices[i+j]-center));
                if(Mathf.Abs(local.size.y-1.7f)>.01f||Mathf.Abs(local.size.z-.18f)>.01f)continue;
                match=i;oldWidth=local.size.x;break;
            }
            if(match<0)throw new Exception("Missing backing board: "+label.text);
            float width=label.GetPreferredValues(label.text).x+1f;
            for(int j=0;j<24;j++)
            {
                var local=inverse*(vertices[match+j]-center);local.x*=width/oldWidth;
                vertices[match+j]=center+label.transform.rotation*local;
            }
            messages.Add(label.text+": "+oldWidth.ToString("F2")+"m -> "+width.ToString("F2")+"m; text size stays "+label.fontSize);
        }
        mesh.vertices=vertices;mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssetIfDirty(mesh);
        return string.Join("; ",messages)+". Board height, text, posts and braces unchanged.";
    }
}
