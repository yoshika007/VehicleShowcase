using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class RemoveRoadwayLakeTownLights
{
    static bool OnRoad(Light lamp, MeshCollider[] roads)
    {
        // Include the pole footprint, not just its center.
        foreach (var offset in new[] { Vector3.zero, Vector3.left * .1f, Vector3.right * .1f, Vector3.forward * .1f, Vector3.back * .1f })
        {
            var p = lamp.transform.position + offset; p.y = 20;
            if (roads.Any(r => r.Raycast(new Ray(p, Vector3.down), out var hit, 30))) return true;
        }
        return false;
    }
    public static string Run()
    {
        if (EditorApplication.isPlaying) throw new Exception("Edit Mode required");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/LakeTownScene.unity") throw new Exception("Open Lake Town first");
        var controller = Object.FindAnyObjectByType<LakeTownNightLights>();
        var roads = controller.GetComponentsInChildren<MeshCollider>().Where(r => r.GetComponent<TestDriveAudioSurface>() != null).ToArray();
        if (roads.Length != 4) throw new Exception("Expected all four road collision surfaces");
        var bad = controller.GetComponentsInChildren<Light>().Where(l => OnRoad(l, roads)).ToArray();
        // Validate representation before deleting anything: these lamps have separate pole/lens siblings.
        foreach (var lamp in bad)
            if (lamp.transform.parent.name != "Additional town streetlights" && lamp.transform.parent.name != "Opposite streetlight")
                throw new Exception("Unexpected lamp representation: " + lamp.name);
        foreach (var lamp in bad)
        {
            if (lamp.transform.parent.name == "Opposite streetlight") { Object.DestroyImmediate(lamp.transform.parent.gameObject); continue; }
            var position = lamp.transform.position;
            var parts = lamp.transform.parent.Cast<Transform>().Where(t => Mathf.Abs(t.position.x - position.x) < .01f && Mathf.Abs(t.position.z - position.z) < .01f &&
                (t.name == "Night streetlight" || t.name == "Streetlight pole" || t.name == "Warm lantern")).ToArray();
            foreach (var part in parts) Object.DestroyImmediate(part.gameObject);
        }
        var so = new SerializedObject(controller);
        foreach (string name in new[] { "streetlights", "lanterns" })
        {
            var array = so.FindProperty(name);
            var surviving = Enumerable.Range(0, array.arraySize).Select(i => array.GetArrayElementAtIndex(i).objectReferenceValue).Where(o => o != null).ToArray();
            array.arraySize = surviving.Length;
            for (int i = 0; i < surviving.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = surviving[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        if (controller.GetComponentsInChildren<Light>().Any(l => OnRoad(l, roads))) throw new Exception("Road lamp remains");
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        return "Removed " + bad.Length + " complete lamps; remaining " + controller.GetComponentsInChildren<Light>().Length + "; zero pole footprints overlap any driving road.";
    }
}
