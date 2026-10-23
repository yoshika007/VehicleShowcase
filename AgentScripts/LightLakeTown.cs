using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class LightLakeTown
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
    const string Dir = "Assets/Environments/LakeTownTrial/";
    static Material Glow(string name, Color color, float strength)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(Dir + name + ".mat");
        if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, Dir + name + ".mat"); }
        material.color = color;
        material.SetColor("_EmissionColor", color * strength);
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
        material.EnableKeyword("_EMISSION");
        EditorUtility.SetDirty(material);
        return material;
    }
    public static string Run()
    {
        if (EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty) throw new Exception("Clean Edit Mode required");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/LakeTownScene.unity");
        var town = scene.GetRootGameObjects().Single(g => g.name.StartsWith("Lake Town —"));
        if (town.GetComponent<LakeTownNightLights>() != null) throw new Exception("Already configured");
        var poleMaterial = AssetDatabase.LoadAssetAtPath<Material>(Dir + "Sign Navy.mat");
        var dayLens = AssetDatabase.LoadAssetAtPath<Material>(Dir + "Warm Yellow.mat");
        var lamps = town.GetComponentsInChildren<Light>().ToList();
        foreach (var light in lamps) { light.range = 28; light.intensity = 45; light.enabled = false; light.shadows = LightShadows.None; }
        var positions = lamps.Select(l => new Vector3(l.transform.position.x, 0, l.transform.position.z)).ToList();
        var lanterns = new List<Renderer>();
        var holder = new GameObject("Additional town streetlights").transform; holder.SetParent(town.transform, false);
        Action<Vector3> add = p =>
        {
            if (positions.Any(q => Vector3.Distance(p, q) < 19)) return;
            positions.Add(p);
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cube); pole.name = "Streetlight pole"; pole.transform.SetParent(holder, false); pole.transform.position = p + Vector3.up * 3.5f; pole.transform.localScale = new Vector3(.18f, 7, .18f); pole.GetComponent<Renderer>().sharedMaterial = poleMaterial; Object.DestroyImmediate(pole.GetComponent<Collider>()); pole.isStatic = true;
            var lens = GameObject.CreatePrimitive(PrimitiveType.Cube); lens.name = "Warm lantern"; lens.transform.SetParent(holder, false); lens.transform.position = p + Vector3.up * 7; lens.transform.localScale = new Vector3(.8f, .7f, .8f); lens.GetComponent<Renderer>().sharedMaterial = dayLens; Object.DestroyImmediate(lens.GetComponent<Collider>()); lanterns.Add(lens.GetComponent<Renderer>()); lens.isStatic = true;
            var light = new GameObject("Night streetlight", typeof(Light)).GetComponent<Light>(); light.transform.SetParent(holder, false); light.transform.position = p + Vector3.up * 6.5f; light.type = LightType.Point; light.color = new Color(1, .81f, .52f); light.range = 28; light.intensity = 45; light.shadows = LightShadows.None; light.enabled = false; lamps.Add(light);
        };
        var route = Route(); float distance = 30;
        for (int i = 0; i < route.Length; i++)
        {
            var direction = (route[(i + 1) % route.Length] - route[i]).normalized;
            distance += Vector3.Distance(route[i], route[(i + 1) % route.Length]);
            if (distance < 25) continue;
            add(route[i] + Vector3.Cross(Vector3.up, direction) * 10.8f); distance = 0;
        }
        foreach (float x in new[] { -90f, 85f }) for (float z = -120; z <= 100; z += 28) add(new Vector3(x + 9, 0, z));
        for (float x = -270; x <= 280; x += 28) add(new Vector3(x, 0, 4));
        var windows = town.GetComponentsInChildren<Renderer>().Where(r => r.sharedMaterial != null && r.sharedMaterial.name == "Window Blue").ToArray();
        if (windows.Length == 0) throw new Exception("Window batch not found");
        var component = town.AddComponent<LakeTownNightLights>(); var so = new SerializedObject(component);
        Action<string, Object[]> array = (name, values) => { var a = so.FindProperty(name); a.arraySize = values.Length; for (int i = 0; i < values.Length; i++) a.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; };
        array("streetlights", lamps.Cast<Object>().ToArray()); array("windows", windows.Cast<Object>().ToArray()); array("lanterns", lanterns.Cast<Object>().ToArray());
        so.FindProperty("nightWindows").objectReferenceValue = Glow("Warm Night Windows", new Color(1, .62f, .23f), 2.2f);
        so.FindProperty("nightLanterns").objectReferenceValue = Glow("Warm Night Lanterns", new Color(1, .81f, .52f), 4);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        return lamps.Count + " streetlights; " + windows.Length + " window batches configured for night.";
    }
}
