using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

// Run in Edit mode with: unity command run_script --file AgentScripts/SetupVehicleExhaust.cs --entry SetupVehicleExhaust.Run
public static class SetupVehicleExhaust
{
    public static string Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before configuring exhaust prefabs.");
        if (!AssetDatabase.IsValidFolder("Assets/VFX")) AssetDatabase.CreateFolder("Assets", "VFX");
        if (!AssetDatabase.IsValidFolder("Assets/VFX/Exhaust")) AssetDatabase.CreateFolder("Assets/VFX", "Exhaust");
        const string materialPath = "Assets/VFX/Exhaust/Exhaust Vapor.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            var dust = AssetDatabase.LoadAssetAtPath<Material>("Assets/Environments/OffroadCourse/Offroad Dust.mat");
            if (dust == null) throw new InvalidOperationException("The existing soft particle material is missing.");
            material = new Material(dust) { name = "Exhaust Vapor" };
            AssetDatabase.CreateAsset(material, materialPath);
        }
        // Measured centers of the visible modeled tailpipe openings, in prefab root space.
        var positions = new Dictionary<string, Vector3[]>
        {
            { "ClassicCar", new[] { new Vector3(-.5f, .453f, -2.65f), new Vector3(.5f, .453f, -2.65f) } },
            { "Hatchback", new[] { new Vector3(-.6489f, .403f, -2.155f) } },
            { "MuscleCar", new[] { new Vector3(-.6048f, .386f, -2.235f), new Vector3(.6048f, .386f, -2.235f) } },
            { "PoliceCar", new[] { new Vector3(-.5929f, .331f, -2.604f) } },
            { "SportsCar", new[] { new Vector3(-.6305f, .2946f, -2.056f), new Vector3(.6305f, .2946f, -2.056f) } }
        };
        int outletCount = 0;
        foreach (var pair in positions)
        {
            string path = "Assets/Prefabs/TestDrive/" + pair.Key + "_TestDrive.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var exhaust = root.GetComponent<TestDriveExhaust>();
                if (exhaust == null) exhaust = root.AddComponent<TestDriveExhaust>();
                var serialized = new SerializedObject(exhaust);
                serialized.FindProperty("exhaustMaterial").objectReferenceValue = material;
                var outlets = serialized.FindProperty("outlets");
                outlets.arraySize = pair.Value.Length;
                for (int i = 0; i < pair.Value.Length; i++)
                {
                    string name = "Exhaust Outlet " + (i + 1);
                    var outlet = root.transform.Find(name);
                    if (outlet == null)
                    {
                        outlet = new GameObject(name).transform;
                        outlet.SetParent(root.transform, false);
                    }
                    outlet.localPosition = pair.Value[i];
                    outlet.localRotation = Quaternion.LookRotation(new Vector3(0f, -.15f, -1f));
                    outlets.GetArrayElementAtIndex(i).objectReferenceValue = outlet;
                    outletCount++;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
        return "Configured " + outletCount + " exhaust outlets across " + positions.Count + " driving prefabs. Showroom prefabs unchanged.";
    }
}
