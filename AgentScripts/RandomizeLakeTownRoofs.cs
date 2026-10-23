using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

// Run through Pipeline in Edit Mode. Colors are baked into the existing roof batch;
// no runtime component or extra renderer is needed. Re-running gives the same result.
public static class RandomizeLakeTownRoofs
{
    const string Dir = "Assets/Environments/LakeTownTrial";
    const string PalettePath = Dir + "/Roof palette.asset";
    static readonly Color[] Palette = {
        new Color(.57f, .22f, .14f), // terracotta
        new Color(.44f, .19f, .17f), // brick red
        new Color(.38f, .27f, .20f), // warm brown
        new Color(.28f, .33f, .37f), // slate
        new Color(.31f, .37f, .29f), // moss green
        new Color(.64f, .39f, .26f)  // weathered clay
    };

    public static string Run()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.isDirty || scene.path != "Assets/Scenes/LakeTownScene.unity")
            throw new Exception("Open a saved LakeTownScene in Edit Mode first.");
        var town = UnityEngine.Object.FindAnyObjectByType<LakeTownNightLights>().transform;
        var roofs = town.GetComponentsInChildren<Transform>(true)
            .Where(t => t.name.StartsWith("Gabled roof "))
            .OrderBy(t => int.Parse(t.name.Substring("Gabled roof ".Length))).ToArray();
        var filter = town.GetComponentsInChildren<MeshFilter>(true).Single(f => f.name == "Scenery batch 3");
        var mesh = filter.sharedMesh;
        var source = AssetDatabase.LoadAssetAtPath<Mesh>(Dir + "/Gabled Roof.asset");
        var material = filter.GetComponent<MeshRenderer>().sharedMaterial;
        if (AssetDatabase.GetAssetPath(mesh) != Dir + "/SceneryBatch3.asset" ||
            AssetDatabase.GetAssetPath(material) != Dir + "/Terracotta Roof.mat" ||
            roofs.Length != 51 || source == null || source.vertexCount != 6 ||
            mesh.vertexCount != roofs.Length * source.vertexCount || mesh.subMeshCount != 1)
            throw new Exception("Roof batch differs from the expected 51-house layout; no changes made.");

        var vertices = mesh.vertices;
        var normals = mesh.normals;
        var triangles = mesh.triangles;
        var sourceVertices = source.vertices;
        var sourceTriangles = source.triangles;
        if (triangles.Length != roofs.Length * sourceTriangles.Length)
            throw new Exception("Unexpected roof triangle count; no changes made.");
        var assignment = Enumerable.Range(0, roofs.Length).Select(i => i % Palette.Length).ToArray();
        var random = new System.Random(231023);
        for (int i = assignment.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            int value = assignment[i]; assignment[i] = assignment[j]; assignment[j] = value;
        }
        var uv = new Vector2[vertices.Length];
        var used = new HashSet<int>();
        for (int block = 0; block < roofs.Length; block++)
        {
            int first = block * source.vertexCount;
            // Match retained roof transforms to baked vertices instead of assuming hierarchy order.
            var matches = Enumerable.Range(0, roofs.Length).Where(r =>
                Enumerable.Range(0, source.vertexCount).All(v =>
                    (filter.transform.TransformPoint(vertices[first + v]) -
                     roofs[r].TransformPoint(sourceVertices[v])).sqrMagnitude < .000001f)).ToArray();
            if (matches.Length != 1 || !used.Add(matches[0]))
                throw new Exception("Cannot uniquely match roof geometry; no changes made.");
            for (int t = 0; t < sourceTriangles.Length; t++)
                if (triangles[block * sourceTriangles.Length + t] != first + sourceTriangles[t])
                    throw new Exception("Unexpected roof indices; no changes made.");
            var sample = new Vector2((assignment[matches[0]] + .5f) / Palette.Length, .5f);
            for (int v = 0; v < source.vertexCount; v++) uv[first + v] = sample;
        }

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(PalettePath);
        if (texture != null && (texture.width != Palette.Length || texture.height != 1 || !texture.isReadable))
            throw new Exception("Existing roof palette has incompatible dimensions or readability.");
        Undo.RecordObjects(new UnityEngine.Object[] { mesh, material }, "Vary Lake Town roof colors");
        if (texture == null)
        {
            // Explicit linear storage matches the town's existing residential palette.
            texture = new Texture2D(Palette.Length, 1, TextureFormat.RGB24, false, true) { name = "Roof palette" };
            AssetDatabase.CreateAsset(texture, PalettePath);
        }
        else Undo.RecordObject(texture, "Vary Lake Town roof palette");
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        for (int i = 0; i < Palette.Length; i++) texture.SetPixel(i, 0, Palette[i].linear);
        texture.Apply();
        mesh.uv = uv;
        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", Color.white);
        material.SetTextureScale("_BaseMap", Vector2.one);
        material.SetTextureOffset("_BaseMap", Vector2.zero);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
        if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
        // The only geometry change is the new UV channel. Keep lighting and topology exact.
        if (!mesh.vertices.SequenceEqual(vertices) || !mesh.normals.SequenceEqual(normals) ||
            !mesh.triangles.SequenceEqual(triangles)) throw new Exception("Roof geometry unexpectedly changed.");
        EditorUtility.SetDirty(mesh); EditorUtility.SetDirty(material); EditorUtility.SetDirty(texture);
        AssetDatabase.SaveAssets();
        SceneView.RepaintAll();
        return "Colored all " + roofs.Length + " roofs with six stable shuffled colors (" +
            string.Join(", ", Enumerable.Range(0, Palette.Length).Select(c => assignment.Count(a => a == c))) +
            "); one existing batch, " + mesh.vertexCount + " vertices, " + triangles.Length / 3 + " triangles; no scene objects changed.";
    }
}
