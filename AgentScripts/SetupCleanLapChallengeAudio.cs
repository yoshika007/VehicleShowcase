using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class SetupCleanLapChallengeAudio
{
    public static string Main()
    {
        if (EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty)
            throw new Exception("Saved Edit mode required");
        if (SceneManager.GetActiveScene().path != "Assets/Scenes/TestTrackScene.unity")
            EditorSceneManager.OpenScene("Assets/Scenes/TestTrackScene.unity");
        AssetDatabase.Refresh();
        string folder = "Assets/Audio/SFX/Challenge/";
        string[] files = { "CountdownTick", "Start", "Finish", "Fail" };
        string[] fields = { "countdownTick", "startSound", "finishSound", "failSound" };
        var clips = new AudioClip[files.Length];
        for (int i = 0; i < files.Length; i++)
        {
            string path = folder + files[i] + ".ogg";
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) throw new Exception("Missing cue: " + path);
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = .9f;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = false;
            importer.SaveAndReimport();
            clips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clips[i] == null) throw new Exception("Cannot load cue: " + path);
        }
        var challenge = UnityEngine.Object.FindAnyObjectByType<TestDriveCleanLapChallenge>(FindObjectsInactive.Include);
        if (challenge == null) throw new Exception("Clean Lap challenge missing");
        var audio = challenge.GetComponent<CleanLapChallengeAudio>();
        if (audio == null) audio = Undo.AddComponent<CleanLapChallengeAudio>(challenge.gameObject);
        var source = audio.GetComponent<AudioSource>();
        Undo.RecordObject(source, "Set challenge cue mix");
        source.clip = null;
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0;
        source.dopplerLevel = 0;
        source.priority = 40;
        source.volume = .82f;
        source.pitch = 1;
        var data = new SerializedObject(audio);
        for (int i = 0; i < fields.Length; i++) data.FindProperty(fields[i]).objectReferenceValue = clips[i];
        data.ApplyModifiedProperties();
        var challengeData = new SerializedObject(challenge);
        challengeData.FindProperty("challengeAudio").objectReferenceValue = audio;
        challengeData.ApplyModifiedProperties();
        EditorUtility.SetDirty(source);
        EditorSceneManager.MarkSceneDirty(challenge.gameObject.scene);
        EditorSceneManager.SaveScene(challenge.gameObject.scene);
        return "Assigned CC0 countdown/start/finish/fail cues on a dedicated 2D source, volume 0.82. Driving/menu audio unchanged.";
    }
}
