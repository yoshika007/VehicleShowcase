using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System;

public static class StyleTouchDrivingControls
{
    const string Folder = "Assets/UI/DrivingControls";
    public static string Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != "Assets/Scenes/TestTrackScene.unity")
            throw new InvalidOperationException("Open TestTrackScene first.");
        if (!AssetDatabase.IsValidFolder("Assets/UI")) AssetDatabase.CreateFolder("Assets", "UI");
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/UI", "DrivingControls");
        var fill = SpriteAsset("Rounded Fill", 0);
        var edge = SpriteAsset("Rounded Border", 1);
        var arrow = SpriteAsset("Direction Arrow", 2);
        var brake = SpriteAsset("Brake Symbol", 3);
        int count = 0;
        foreach (var control in UnityEngine.Object.FindObjectsByType<TestDriveTouchButton>())
        {
            bool gas = control.name == "Accelerator";
            bool stopping = control.name == "BrakeReverse";
            bool left = control.name == "SteerLeft";
            if (!gas && !stopping && !left && control.name != "SteerRight") continue;
            var button = control.GetComponent<UnityEngine.UI.Button>();
            var background = control.GetComponent<UnityEngine.UI.Image>();
            background.sprite = fill;
            background.type = UnityEngine.UI.Image.Type.Sliced;
            background.raycastTarget = true;
            button.targetGraphic = background;
            button.transition = Selectable.Transition.None;
            var border = ImageChild(control.transform, "Control Border", edge);
            Stretch(border.rectTransform);
            border.type = UnityEngine.UI.Image.Type.Sliced;
            var icon = ImageChild(control.transform, "Control Icon", stopping ? brake : arrow);
            Center(icon.rectTransform, new Vector2(0, 35), new Vector2(66, 66));
            icon.rectTransform.localRotation = Quaternion.Euler(0, 0, gas ? 90 : left ? 180 : 0);
            var label = control.transform.Find("Label").GetComponent<TMP_Text>();
            label.text = gas ? "GAS" : stopping ? "BRAKE" : left ? "LEFT" : "RIGHT";
            label.enableAutoSizing = false;
            label.fontSize = gas ? 32 : stopping ? 28 : 26;
            label.fontStyle = FontStyles.Normal;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            Center(label.rectTransform, new Vector2(0, -35), new Vector2(152, 44));
            label.color = gas ? new Color(.10f, .13f, .15f) : new Color(.94f, .96f, .98f);
            if (stopping)
            {
                var child = control.transform.Find("Reverse Label");
                var secondary = child != null ? child.GetComponent<TextMeshProUGUI>()
                    : new GameObject("Reverse Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
                secondary.transform.SetParent(control.transform, false);
                secondary.font = label.font;
                secondary.fontSharedMaterial = label.fontSharedMaterial;
                secondary.text = "REV";
                secondary.fontSize = 21;
                secondary.alignment = TextAlignmentOptions.Center;
                secondary.color = new Color(.78f, .83f, .87f);
                secondary.raycastTarget = false;
                Center(secondary.rectTransform, new Vector2(0, -74), new Vector2(130, 32));
            }
            var normal = gas ? new Color(1f, .75f, .25f, .88f) : new Color(.065f, .11f, .15f, .72f);
            var held = gas ? new Color(1f, .86f, .44f, 1f) : stopping ? new Color(.58f, .13f, .13f, .94f) : new Color(.12f, .26f, .30f, .94f);
            var normalEdge = gas ? new Color(1f, .88f, .50f, .9f) : new Color(.77f, .85f, .89f, .4f);
            var heldEdge = stopping ? new Color(1f, .42f, .34f) : new Color(1f, .81f, .32f);
            var normalIcon = gas ? new Color(.12f, .16f, .18f) : new Color(.94f, .97f, 1f);
            var heldIcon = gas ? normalIcon : stopping ? Color.white : new Color(1f, .84f, .40f);
            background.color = normal;
            border.color = normalEdge;
            icon.color = normalIcon;
            var visual = control.GetComponent<TestDriveTouchButtonVisual>();
            if (visual == null) visual = control.gameObject.AddComponent<TestDriveTouchButtonVisual>();
            var serialized = new SerializedObject(visual);
            serialized.FindProperty("background").objectReferenceValue = background;
            serialized.FindProperty("border").objectReferenceValue = border;
            serialized.FindProperty("icon").objectReferenceValue = icon;
            serialized.FindProperty("normalBackground").colorValue = normal;
            serialized.FindProperty("heldBackground").colorValue = held;
            serialized.FindProperty("normalBorder").colorValue = normalEdge;
            serialized.FindProperty("heldBorder").colorValue = heldEdge;
            serialized.FindProperty("normalIcon").colorValue = normalIcon;
            serialized.FindProperty("heldIcon").colorValue = heldIcon;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            count++;
        }
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        return "Styled " + count + " controls; original button RectTransforms and input components preserved.";
    }

    static UnityEngine.UI.Image ImageChild(Transform parent, string name, Sprite sprite)
    {
        var child = parent.Find(name);
        var image = child != null ? child.GetComponent<UnityEngine.UI.Image>()
            : new GameObject(name, typeof(RectTransform)).AddComponent<UnityEngine.UI.Image>();
        image.transform.SetParent(parent, false);
        image.sprite = sprite;
        image.raycastTarget = false;
        return image;
    }
    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    static void Center(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
    }
    static Sprite SpriteAsset(string name, int kind)
    {
        string path = Folder + "/" + name + ".asset";
        var existing = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var asset in existing) if (asset is Sprite sprite) return sprite;
        var texture = new Texture2D(128, 128, TextureFormat.RGBA32, false)
            { name = name, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color[128 * 128];
        for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
        {
            var p = new Vector2(x + .5f - 64f, y + .5f - 64f);
            var q = new Vector2(Mathf.Abs(p.x) - 39f, Mathf.Abs(p.y) - 39f);
            float distance = new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude
                + Mathf.Min(Mathf.Max(q.x, q.y), 0) - 24f;
            float alpha = Mathf.Clamp01(.5f - distance);
            if (kind == 1) alpha -= Mathf.Clamp01(.5f - distance - 2f);
            if (kind == 2)
            {
                var a = new Vector2(-31, -42); var b = new Vector2(41, 0); var c = new Vector2(-31, 42);
                alpha = Mathf.Clamp01(Mathf.Min(Edge(a, b, p), Mathf.Min(Edge(b, c, p), Edge(c, a, p))) + .5f);
            }
            if (kind == 3)
            {
                float ring = Mathf.Abs(p.magnitude - 27f) - 2.5f;
                float brackets = Mathf.Max(Mathf.Abs(p.magnitude - 44f) - 2.5f, 34f - Mathf.Abs(p.x));
                float stem = Mathf.Max(Mathf.Abs(p.x) - 3f, Mathf.Abs(p.y - 7f) - 11f);
                float dot = (p - new Vector2(0, -15f)).magnitude - 3.5f;
                alpha = Mathf.Clamp01(.5f - Mathf.Min(Mathf.Min(ring, brackets), Mathf.Min(stem, dot)));
            }
            pixels[y * 128 + x] = new Color(1, 1, 1, alpha);
        }
        texture.SetPixels(pixels); texture.Apply();
        AssetDatabase.CreateAsset(texture, path);
        var result = Sprite.Create(texture, new Rect(0, 0, 128, 128), new Vector2(.5f, .5f), 100, 0,
            SpriteMeshType.FullRect, kind < 2 ? new Vector4(28, 28, 28, 28) : Vector4.zero);
        result.name = name;
        AssetDatabase.AddObjectToAsset(result, texture);
        AssetDatabase.SaveAssets();
        return result;
    }
    static float Edge(Vector2 a, Vector2 b, Vector2 p)
        => ((b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x)) / (b - a).magnitude;
}
