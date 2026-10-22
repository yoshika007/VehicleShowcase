using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class SetupTimeOfDayIcons
{
    public static string Main()
    {
        if (EditorApplication.isPlaying) throw new Exception("Edit Mode required");
        var manager = UnityEngine.Object.FindAnyObjectByType<VehicleShowcaseManager>();
        var panel = (GameObject)new SerializedObject(manager).FindProperty("timeOfDayPanel").objectReferenceValue;
        var dialog = panel.transform.Find("Dialog");
        AssetDatabase.Refresh();
        string folder = "Assets/UI/Icons/TimeOfDay/";
        string[] buttons = { "DayButton", "NightButton" };
        string[] sprites = { "Sun", "MoonStar" };
        for (int i = 0; i < buttons.Length; i++)
        {
            string path = folder + sprites[i] + ".png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            var button = dialog.Find(buttons[i]);
            if (button.Find("TimeOfDayIcon") != null) throw new Exception("Icon already exists");
            var title = button.Find("Title").GetComponent<TMP_Text>();
            float width = title.GetPreferredValues(title.text).x + 4f;
            Undo.RecordObject(title.rectTransform, "Center icon and label");
            title.rectTransform.sizeDelta = new Vector2(width, title.rectTransform.sizeDelta.y);
            title.rectTransform.anchoredPosition = new Vector2(28f, 0f);
            var icon = new GameObject("TimeOfDayIcon", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            Undo.RegisterCreatedObjectUndo(icon, "Add time of day icon");
            var rect = (RectTransform)icon.transform;
            rect.SetParent(button, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(40, 40);
            rect.anchoredPosition = new Vector2(-width * .5f - 8f, 0);
            var image = icon.GetComponent<UnityEngine.UI.Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            image.color = new Color(.76f, .93f, .91f, 1f);
            image.preserveAspect = true;
            image.raycastTarget = false;
        }
        EditorSceneManager.MarkSceneDirty(panel.scene);
        EditorSceneManager.SaveScene(panel.scene);
        return "Added matching Sun and MoonStar sprites beside centered DAY and NIGHT labels.";
    }
}
