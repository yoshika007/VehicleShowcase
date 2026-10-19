using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class SetupDrivingAudio
{
    public static string Main()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != "Assets/Scenes/TestTrackScene.unity" || scene.isDirty)
            throw new Exception("Clean TestTrackScene in Edit mode required.");
        const string dir = "Assets/Audio/Driving/";
        foreach (var name in new[]{"EngineLoop", "AsphaltLoop", "DirtLoop", "GravelLoop"})
        {
            var importer = (AudioImporter)AssetImporter.GetAtPath(dir + name + ".wav");
            importer.forceToMono = true;
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            settings.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
            settings.sampleRateOverride = 44100;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }
        int count = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[]{"Assets/Prefabs/TestDrive"}))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (!root.TryGetComponent<TestDriveMovement>(out _)) continue;
                var audio = root.GetComponent<TestDriveAudio>() ?? root.AddComponent<TestDriveAudio>();
                var so = new SerializedObject(audio);
                so.FindProperty("engineLoop").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(dir+"EngineLoop.wav");
                so.FindProperty("asphaltLoop").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(dir+"AsphaltLoop.wav");
                so.FindProperty("dirtLoop").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(dir+"DirtLoop.wav");
                so.FindProperty("gravelLoop").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(dir+"GravelLoop.wav");
                float tone = path.Contains("SportsCar") ? 1.12f : path.Contains("Hatchback") ? 1.16f : path.Contains("Muscle") ? .83f : path.Contains("Military") ? .72f : path.Contains("Monster") ? .77f : path.Contains("Pickup") ? .82f : path.Contains("Van") ? .9f : 1f;
                so.FindProperty("engineTone").floatValue = tone;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root,path);
                count++;
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        var report = new System.Text.StringBuilder();
        foreach (var col in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Include))
        {
            if (col.gameObject.scene != scene || col is WheelCollider) continue;
            int kind = -1;
            if (col.TryGetComponent<TestDriveSurface>(out _)) kind = col.name.Contains("Gravel") ? 2 : 1;
            else if (col.TryGetComponent<Renderer>(out var renderer))
            {
                var mats = renderer.sharedMaterials.Where(m=>m!=null).Select(m=>m.name.ToLowerInvariant()).ToArray();
                if (mats.Any(m=>m.Contains("asphalt"))) kind=0;
                else if(mats.Any(m=>m.Contains("grass"))) kind=3;
            }
            if(kind<0) continue;
            var marker=col.GetComponent<TestDriveAudioSurface>() ?? col.gameObject.AddComponent<TestDriveAudioSurface>();
            var so=new SerializedObject(marker);
            so.FindProperty("kind").enumValueIndex=kind;
            so.ApplyModifiedPropertiesWithoutUndo();
            report.AppendLine(col.name+" = "+(TestDriveAudioSurface.Kind)kind);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        return "Audio configured on "+count+" driving prefabs.\n"+report;
    }
}
