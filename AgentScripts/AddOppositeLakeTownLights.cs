using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class AddOppositeLakeTownLights
{
    public static Vector3[] Route()
    {
        var knots = new[] { new Vector3(-250,0,70),new Vector3(-160,0,65),new Vector3(-70,0,90),new Vector3(20,0,115),new Vector3(105,0,115),new Vector3(180,0,95),new Vector3(250,0,140),new Vector3(290,0,40),new Vector3(250,0,-40),new Vector3(180,0,-80),new Vector3(130,0,-40),new Vector3(80,0,-80),new Vector3(25,0,-135),new Vector3(-80,0,-140),new Vector3(-170,0,-90),new Vector3(-255,0,-100),new Vector3(-290,0,-35) };
        var points = new List<Vector3>();
        for (int i=0;i<knots.Length;i++) for(int j=0;j<18;j++)
        {
            float t=j/18f;var a=knots[(i+knots.Length-1)%knots.Length];var b=knots[i];var c=knots[(i+1)%knots.Length];var d=knots[(i+2)%knots.Length];
            points.Add(.5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t));
        }
        return points.ToArray();
    }

    static Vector3 Closest(Vector3 point, Vector3 a, Vector3 b)
    {
        var direction = b - a;
        return a + direction * Mathf.Clamp01(Vector3.Dot(point - a, direction) / direction.sqrMagnitude);
    }
    public static string Run()
    {
        if (EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty) throw new Exception("Clean Edit Mode required");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/LakeTownScene.unity");
        var controller = Object.FindAnyObjectByType<LakeTownNightLights>(); var town = controller.transform;
        if (town.Find("Opposite side streetlights") != null) throw new Exception("Already configured");
        var holder = new GameObject("Opposite side streetlights").transform; holder.SetParent(town, false);
        var route = Route(); var segments = new List<Vector3[]>();
        for (int i = 0; i < route.Length; i++) segments.Add(new[] { route[i], route[(i+1)%route.Length] });
        segments.Add(new[] {new Vector3(-90,0,-145),new Vector3(-90,0,91)});
        segments.Add(new[] {new Vector3(85,0,-88),new Vector3(85,0,120)});
        segments.Add(new[] {new Vector3(-290,0,-5),new Vector3(294,0,-5)});
        var so = new SerializedObject(controller); var lamps = so.FindProperty("streetlights"); var lenses = so.FindProperty("lanterns");
        var existing = Enumerable.Range(0, lamps.arraySize).Select(i => (Light)lamps.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
        var positions = existing.Select(l => new Vector3(l.transform.position.x,0,l.transform.position.z)).ToList();
        var poleMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Environments/LakeTownTrial/Sign Navy.mat");
        var lensMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Environments/LakeTownTrial/Warm Yellow.mat");
        int added = 0, skipped = 0;
        foreach (var source in existing)
        {
            var p = new Vector3(source.transform.position.x,0,source.transform.position.z);
            var road = segments.OrderBy(s => (Closest(p,s[0],s[1])-p).sqrMagnitude).First();
            var center = Closest(p, road[0], road[1]); var opposite = center * 2 - p;
            // Keep poles off junctions and away from existing poles.
            if (segments.Min(s => Vector3.Distance(opposite,Closest(opposite,s[0],s[1]))) < 7.5f || positions.Any(q => Vector3.Distance(q,opposite)<8)) { skipped++; continue; }
            positions.Add(opposite);
            var assembly = new GameObject("Opposite streetlight").transform; assembly.SetParent(holder, false); assembly.position=opposite;
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cube); pole.name="Streetlight pole"; pole.transform.SetParent(assembly,false); pole.transform.localPosition=Vector3.up*3.5f; pole.transform.localScale=new Vector3(.18f,7,.18f); pole.GetComponent<Renderer>().sharedMaterial=poleMaterial; Object.DestroyImmediate(pole.GetComponent<Collider>()); pole.isStatic=true;
            var bulb = GameObject.CreatePrimitive(PrimitiveType.Cube); bulb.name="Warm lantern"; bulb.transform.SetParent(assembly,false); bulb.transform.localPosition=Vector3.up*7; bulb.transform.localScale=new Vector3(.8f,.7f,.8f); bulb.GetComponent<Renderer>().sharedMaterial=lensMaterial; Object.DestroyImmediate(bulb.GetComponent<Collider>()); bulb.isStatic=true;
            var light = Object.Instantiate(source,assembly,false); light.name="Night streetlight"; light.transform.localPosition=Vector3.up*6.5f; light.enabled=false;
            int index=lamps.arraySize; lamps.InsertArrayElementAtIndex(index); lamps.GetArrayElementAtIndex(index).objectReferenceValue=light;
            index=lenses.arraySize; lenses.InsertArrayElementAtIndex(index); lenses.GetArrayElementAtIndex(index).objectReferenceValue=bulb.GetComponent<Renderer>(); added++;
        }
        so.ApplyModifiedPropertiesWithoutUndo(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        return "Added "+added+" opposite-side lamps; "+skipped+" junction/duplicate positions omitted; total "+lamps.arraySize+" lights and "+lenses.arraySize+" lanterns.";
    }
}
