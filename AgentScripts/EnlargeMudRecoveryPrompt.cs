using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;

public static class EnlargeMudRecoveryPrompt
{
    public static string Main()
    {
        if (EditorApplication.isPlaying) throw new Exception("Edit Mode required");
        var original = SceneManager.GetActiveScene();
        var scene = SceneManager.GetSceneByPath("Assets/Scenes/TestTrackScene.unity");
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene("Assets/Scenes/TestTrackScene.unity", OpenSceneMode.Additive);
        try
        {
            if (scene.isDirty) throw new Exception("Save TestTrackScene changes before running this builder");
            var prompt = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TestDriveRecoveryPrompt>(true)).Single();
            var panel = (RectTransform)prompt.transform;
            Undo.RecordObject(panel, "Enlarge mud recovery prompt");
            panel.sizeDelta = new Vector2(640, 244);
            string[] names = { "Heading", "Message", "VehicleAdvice" };
            float[] sizes = { 46, 38, 30 };
            float[] tops = { 8, 78, 192 };
            float[] heights = { 64, 108, 44 };
            for (int i = 0; i < names.Length; i++)
            {
                var text = panel.Find(names[i]).GetComponent<TMP_Text>();
                Undo.RecordObject(text, "Enlarge recovery text");
                Undo.RecordObject(text.rectTransform, "Fit recovery text");
                text.fontSize = sizes[i];
                text.rectTransform.sizeDelta = new Vector2(608, heights[i]);
                text.rectTransform.anchoredPosition = new Vector2(0, -tops[i]);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "Enlarged recovery panel from 500 x 184 to 640 x 244; text sizes 46 / 38 / 30.";
        }
        finally
        {
            if (opened) EditorSceneManager.CloseScene(scene, true);
            SceneManager.SetActiveScene(original);
        }
    }
}
