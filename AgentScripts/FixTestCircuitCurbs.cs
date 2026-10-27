using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

// Trim only overlapping existing curb boxes; positions, colors, colliders and gaps remain intact.
public static class FixTestCircuitCurbs
{
    static bool Overlap(Transform a,Transform b,float gap=0f)
    {
        var ar=Vector3.ProjectOnPlane(a.right,Vector3.up).normalized;var af=Vector3.ProjectOnPlane(a.forward,Vector3.up).normalized;
        var br=Vector3.ProjectOnPlane(b.right,Vector3.up).normalized;var bf=Vector3.ProjectOnPlane(b.forward,Vector3.up).normalized;
        var delta=b.position-a.position;var sa=a.lossyScale;var sb=b.lossyScale;
        foreach(var axis in new[]{ar,af,br,bf})
        {
            float ra=(Mathf.Abs(Vector3.Dot(ar,axis))*sa.x+Mathf.Abs(Vector3.Dot(af,axis))*sa.z)*.5f;
            float rb=(Mathf.Abs(Vector3.Dot(br,axis))*sb.x+Mathf.Abs(Vector3.Dot(bf,axis))*sb.z)*.5f;
            if(Mathf.Abs(Vector3.Dot(delta,axis))>=ra+rb+gap)return false;
        }
        return true;
    }
    public static string Run()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.name!="TestTrackScene"||EditorApplication.isPlaying)throw new Exception("Expected TestTrackScene outside Play Mode");
        var parent=GameObject.Find("Red and white curbs").transform;var all=parent.Cast<Transform>().ToArray();
        var original=all.ToDictionary(t=>t,t=>new {position=t.position,rotation=t.rotation,scale=t.localScale,active=t.gameObject.activeSelf,collider=t.GetComponent<Collider>(),material=t.GetComponent<Renderer>().sharedMaterial});
        var crossing=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).Where(t=>t.name.Contains("Trail edge marker")).ToDictionary(t=>t,t=>t.gameObject.activeSelf);
        var changed=new HashSet<Transform>();int repaired=0;
        for(int i=0;i<all.Length;i++)for(int j=i+1;j<all.Length;j++)
        {
            var a=all[i];var b=all[j];if(!a.gameObject.activeInHierarchy||!b.gameObject.activeInHierarchy||Vector3.Distance(a.position,b.position)>5||!Overlap(a,b))continue;
            var sa=a.localScale;var sb=b.localScale;float lo=0,hi=1;
            for(int n=0;n<24;n++){float f=(lo+hi)*.5f;a.localScale=new Vector3(sa.x,sa.y,sa.z*f);b.localScale=new Vector3(sb.x,sb.y,sb.z*f);if(Overlap(a,b,.015f))hi=f;else lo=f;}
            if(lo<.35f)throw new Exception("Unexpected intersection requires excessive trimming: "+a.name+"/"+b.name);
            Undo.RecordObject(a,"Smooth Test Circuit curbs");Undo.RecordObject(b,"Smooth Test Circuit curbs");
            a.localScale=new Vector3(sa.x,sa.y,sa.z*lo);b.localScale=new Vector3(sb.x,sb.y,sb.z*lo);changed.Add(a);changed.Add(b);repaired++;
        }
        Physics.SyncTransforms();int remaining=0;
        for(int i=0;i<all.Length;i++)for(int j=i+1;j<all.Length;j++)if(all[i].gameObject.activeInHierarchy&&all[j].gameObject.activeInHierarchy&&Vector3.Distance(all[i].position,all[j].position)<5&&Overlap(all[i],all[j]))remaining++;
        if(remaining!=0)throw new Exception("Remaining intersecting curb pairs="+remaining);
        foreach(var t in all){var x=original[t];if(t.position!=x.position||t.rotation!=x.rotation||t.gameObject.activeSelf!=x.active||t.GetComponent<Collider>()!=x.collider||t.GetComponent<Renderer>().sharedMaterial!=x.material||Mathf.Abs(t.localScale.x-x.scale.x)>.00001f||Mathf.Abs(t.localScale.y-x.scale.y)>.00001f)throw new Exception("Unexpected curb change: "+t.name);}
        foreach(var entry in crossing)if(entry.Key.gameObject.activeSelf!=entry.Value)throw new Exception("Crossing marker state changed");
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return "Resolved "+repaired+" overlapping pairs by trimming "+changed.Count+" existing curb blocks; remaining overlaps="+remaining+"; "+all.Length+" original objects and collider assignments preserved, "+crossing.Count+" crossing-marker states preserved.";
    }
}
