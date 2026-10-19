using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class AddOffroadSurfaceDetail
{
    const string Dir = "Assets/Environments/OffroadCourse/SurfaceDetail";
    static float PeriodicNoise(float x, float y, float frequency, float seed)
    {
        float angle = y * Mathf.PI * 2;
        return Mathf.PerlinNoise(x * frequency + seed + Mathf.Cos(angle) * 2, seed * .4f + Mathf.Sin(angle) * 2);
    }
    static Texture2D RoadTexture(bool gravel)
    {
        const int size = 512;
        var texture = new Texture2D(size, size, TextureFormat.RGB24, true)
        {
            name = gravel ? "Worn Gravel Surface" : "Tracked Dirt Surface",
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear,
            anisoLevel = 4
        };
        var pixels = new Color[size * size];
        var random = new System.Random(gravel ? 419 : 713);
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            float u = (x + .5f) / size, v = (y + .5f) / size;
            float mottling = PeriodicNoise(u, v, 17, 23);
            float grain = (float)random.NextDouble();
            Color baseColor = gravel ? new Color(.53f, .49f, .40f) : new Color(.43f, .28f, .15f);
            baseColor *= .84f + mottling * .21f + grain * (gravel ? .16f : .08f);
            float center = .5f + Mathf.Sin(v * Mathf.PI * 2) * .015f;
            float worn = Mathf.Exp(-Mathf.Pow((u - center) / .22f, 4));
            Color wornColor = gravel ? new Color(.46f, .425f, .35f) : new Color(.46f, .32f, .195f);
            baseColor = Color.Lerp(baseColor, wornColor * (.96f + grain * .07f), worn * .42f);
            float shift = Mathf.Sin(v * Mathf.PI * 2) * .009f;
            foreach (float trackCenter in new[] { .428f, .572f, .31f, .452f, .548f, .69f })
            {
                float distance = Mathf.Abs(u - trackCenter - shift);
                float track = 1 - Mathf.SmoothStep(.006f, .014f, distance);
                float broken = Mathf.SmoothStep(.24f, .67f, PeriodicNoise(u, v, 32, trackCenter * 51));
                float rib = .65f + .35f * Mathf.SmoothStep(.1f, .55f, Mathf.Abs(Mathf.Sin((v * 42 + u * 5) * Mathf.PI)));
                float strength = (trackCenter == .428f || trackCenter == .572f) ? .15f : .075f;
                baseColor *= 1 - track * broken * rib * strength;
            }
            // Loose aggregate collects toward the shoulders; the middle remains visually smoother.
            float edge = Mathf.SmoothStep(.19f, .38f, Mathf.Abs(u - .5f));
            float patch = Mathf.SmoothStep(.53f, .73f, PeriodicNoise(u, v, 10, 77)) * edge;
            float stone = grain > .89f ? (grain - .89f) / .11f : 0;
            baseColor = Color.Lerp(baseColor, new Color(.64f, .59f, .48f), patch * (.2f + stone * .65f));
            if (gravel && grain > .92f) baseColor *= 1.12f;
            pixels[y * size + x] = baseColor;
        }
        texture.SetPixels(pixels); texture.Apply(true, false);
        AssetDatabase.CreateAsset(texture, Dir + "/" + texture.name + ".asset"); return texture;
    }
    public static string Main()
    {
        if (EditorApplication.isPlaying) throw new System.Exception("Edit mode required.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new System.Exception("Save pending scene changes first.");
        if (AssetDatabase.IsValidFolder(Dir)) throw new System.Exception("Surface detail assets already exist; adjust the existing materials instead.");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/TestTrackScene.unity", OpenSceneMode.Single);
        var root = GameObject.Find("Offroad Course").transform;
        AssetDatabase.CreateFolder("Assets/Environments/OffroadCourse", "SurfaceDetail");
        var dirt = RoadTexture(false); var gravel = RoadTexture(true);
        var names = new[] { "Dirt Hill 1", "Dirt Hill 2", "Gravel Corner 1", "Gravel Corner 2", "Dirt access road", "Truck ridge branch" };
        for (int i = 0; i < names.Length; i++)
        {
            var go = root.Find(names[i]); var renderer = go.GetComponent<MeshRenderer>();
            var material = new Material(renderer.sharedMaterial) { name = names[i] + " Surface Detail" };
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BaseMap", names[i].StartsWith("Gravel") ? gravel : dirt);
            float width = names[i] == "Truck ridge branch" ? 12 : 14;
            material.SetTextureScale("_BaseMap", new Vector2(4 / width, .36f));
            material.SetTextureOffset("_BaseMap", new Vector2(0, i * .173f));
            material.SetFloat("_Smoothness", names[i].StartsWith("Gravel") ? .035f : .08f);
            AssetDatabase.CreateAsset(material, Dir + "/" + material.name + ".mat");
            renderer.sharedMaterial = material;
        }
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        return "Applied faint overlapping tire tracks, worn central lanes and loose gravel edge patches to 6 off-road surfaces. No meshes, colliders or grip settings changed.";
    }
}
