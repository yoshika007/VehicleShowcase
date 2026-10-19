using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public static class BlendOffroadLandscape
{
    const string Dir = "Assets/Environments/OffroadCourse/";
    class Route { public Vector3[] points; public float halfWidth; }
    static Route[] routes;
    static Color grass, soil, gravel;
    static float Noise(Vector3 p, float seed) => Mathf.PerlinNoise(p.x * .075f + seed, p.z * .075f + seed * .7f);
    static Vector3[] Centers(string name, float heightOffset = 0)
    {
        var v = GameObject.Find("Offroad Course").transform.Find(name).GetComponent<MeshFilter>().sharedMesh.vertices;
        var p = new Vector3[v.Length / 2];
        for (int i = 0; i < p.Length; i++) p[i] = (v[i * 2] + v[i * 2 + 1]) * .5f + Vector3.up * heightOffset;
        return p;
    }
    static float CapToDrivingRoutes(Vector3 position)
    {
        float height = position.y;
        foreach (var route in routes)
        {
            float nearest = float.MaxValue, roadHeight = 0;
            var q = new Vector2(position.x, position.z);
            for (int i = 1; i < route.points.Length; i++)
            {
                var a = route.points[i - 1]; var b = route.points[i];
                var from = new Vector2(a.x, a.z); var delta = new Vector2(b.x - a.x, b.z - a.z);
                float t = Mathf.Clamp01(Vector2.Dot(q - from, delta) / Mathf.Max(.00001f, delta.sqrMagnitude));
                float d = (q - from - delta * t).sqrMagnitude;
                if (d >= nearest) continue;
                nearest = d; roadHeight = Mathf.Lerp(a.y, b.y, t);
            }
            if (nearest < (route.halfWidth + .5f) * (route.halfWidth + .5f)) height = Mathf.Min(height, roadHeight - .025f);
        }
        return height;
    }
    static Mesh Landscape(Vector3[] points, float halfWidth, bool closed, bool branch, string name)
    {
        const int across = 32;
        var v = new Vector3[points.Length * (across + 1)];
        var uv = new Vector2[v.Length]; var colors = new Color[v.Length];
        var triangles = new List<int>();
        for (int i = 0; i < points.Length; i++)
        {
            Vector3 tangent;
            if (closed && (i == 0 || i == points.Length - 1)) tangent = points[1] - points[points.Length - 2];
            else tangent = points[Mathf.Min(i + 1, points.Length - 1)] - points[Mathf.Max(0, i - 1)];
            var side = Vector3.Cross(Vector3.up, tangent).normalized;
            float u = i / (float)(points.Length - 1);
            float endRound = closed ? 1 : Mathf.Pow(Mathf.Max(0, Mathf.Sin(Mathf.PI * u)), .65f);
            for (int j = 0; j <= across; j++)
            {
                float t = Mathf.Lerp(-1, 1, j / (float)across);
                float sign = t < 0 ? -1 : 1;
                float edgeNoise = Noise(points[i] + side * sign * 13, branch ? 41 : 17);
                float outer = halfWidth + 1.3f + (branch ? 14 : 12) * endRound + (edgeNoise - .5f) * 4 * endRound;
                float offset = t * outer;
                var p = points[i] + side * offset;
                float inner = halfWidth + .4f;
                float blend = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(inner, outer, Mathf.Abs(offset)));
                p.y = -.055f + (points[i].y + .03f) * blend;
                p.y = CapToDrivingRoutes(p);
                float wearWidth = 1.1f + Noise(p, 63) * 2.5f;
                float soilWeight = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(halfWidth - .25f, halfWidth + wearWidth, Mathf.Abs(offset)));
                // Broken grass tongues and occasional exposed earth soften the edge without covering the road.
                float patch = Noise(p, 119);
                float bankPatch = Mathf.SmoothStep(.65f, .9f, patch) * blend * (1 - soilWeight) * .45f;
                soilWeight = Mathf.Clamp01(soilWeight + bankPatch);
                Color earth = soil;
                if (!branch)
                {
                    float curvature = Mathf.Abs(Vector3.Dot(tangent.normalized, Vector3.forward));
                    earth = Color.Lerp(soil, gravel, Mathf.SmoothStep(.15f, .65f, curvature));
                }
                var tint = Color.Lerp(grass, earth, soilWeight);
                tint *= Mathf.Lerp(.96f, 1.04f, Noise(p, 87) * soilWeight);
                tint.a = 1;
                // Vertex colors are linear shader inputs; match the existing grass material in linear projects.
                colors[i * (across + 1) + j] = QualitySettings.activeColorSpace == ColorSpace.Linear ? tint.linear : tint;
                int k = i * (across + 1) + j; v[k] = p; uv[k] = new Vector2(p.x / 4, p.z / 4);
                if (i == points.Length - 1 || j == across) continue;
                triangles.AddRange(new[] { k, k + across + 1, k + 1, k + 1, k + across + 1, k + across + 2 });
            }
        }
        var mesh = new Mesh { name = name };
        mesh.vertices = v; mesh.uv = uv; mesh.colors = colors; mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(Dir + name + ".asset");
        if (existing != null) { EditorUtility.CopySerialized(mesh, existing); EditorUtility.SetDirty(existing); Object.DestroyImmediate(mesh); AssetDatabase.SaveAssets(); AssetDatabase.ImportAsset(Dir + name + ".asset", ImportAssetOptions.ForceUpdate); return AssetDatabase.LoadAssetAtPath<Mesh>(Dir + name + ".asset"); }
        AssetDatabase.CreateAsset(mesh, Dir + name + ".asset"); return mesh;
    }
    static void CreateLand(Transform parent, string name, Vector3[] points, float width, bool closed, bool branch, Material material, float grip, Color dust)
    {
        var mesh = Landscape(points, width, closed, branch, name);
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider), typeof(TestDriveSurface));
        go.transform.SetParent(parent, false);
        go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<MeshCollider>().sharedMesh = mesh;
        go.GetComponent<Renderer>().sharedMaterial = material;
        var so = new SerializedObject(go.GetComponent<TestDriveSurface>());
        so.FindProperty("gripMultiplier").floatValue = grip; so.FindProperty("dustColor").colorValue = dust;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    public static string Main()
    {
        if (EditorApplication.isPlaying) throw new System.Exception("Edit mode required.");
        for (int i = 0; i < SceneManager.sceneCount; i++) if (SceneManager.GetSceneAt(i).isDirty) throw new System.Exception("Save pending scene changes first.");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/TestTrackScene.unity", OpenSceneMode.Single);
        var root = GameObject.Find("Offroad Course").transform;
        if (root.Find("Natural ground edges")) Object.DestroyImmediate(root.Find("Natural ground edges").gameObject);
        var main = Centers("Dirt shoulders", .025f); var branch = Centers("Truck ridge branch"); var access = Centers("Dirt access road");
        routes = new[] { new Route { points = main, halfWidth = 7 }, new Route { points = branch, halfWidth = 6 }, new Route { points = access, halfWidth = 7 } };
        grass = AssetDatabase.LoadAssetAtPath<Material>("Assets/Environments/TestTrack/Track_Grass.mat").color;
        soil = new Color(.48f, .36f, .22f); gravel = new Color(.50f, .46f, .36f);
        // URP's lit particle shader supports mesh vertex colors and the same PBR lighting as the grass.
        var shader = Shader.Find("Universal Render Pipeline/Particles/Lit");
        if (shader == null) throw new System.Exception("Vertex-color lit shader unavailable.");
        var material = AssetDatabase.LoadAssetAtPath<Material>(Dir + "Natural Ground Blend.mat");
        bool newMaterial = material == null;
        if (newMaterial) material = new Material(shader) { name = "Natural Ground Blend" };
        material.SetColor("_BaseColor", Color.white); material.SetFloat("_Surface", 0); material.SetFloat("_Smoothness", .12f);
        material.SetFloat("_Metallic", 0); material.SetFloat("_ZWrite", 1);
        if (newMaterial) AssetDatabase.CreateAsset(material, Dir + "Natural Ground Blend.mat");
        var group = new GameObject("Natural ground edges").transform; group.SetParent(root, false);
        CreateLand(group, "Rounded main loop banks", main, 7, true, false, material, .8f, new Color(.73f,.59f,.41f,.42f));
        CreateLand(group, "Rounded hill climb banks", branch, 6, false, true, material, .85f, new Color(.73f,.59f,.41f,.42f));
        CreateLand(group, "Blended access shoulders", access, 7, false, false, material, .8f, new Color(.73f,.59f,.41f,.42f));
        foreach (var name in new[] { "Dirt shoulders", "Branch shoulders", "Dirt Hill 1 earth banks", "Dirt Hill 2 earth banks", "Truck ridge branch earth banks" }) root.Find(name).gameObject.SetActive(false);
        Physics.SyncTransforms();
        foreach (Transform child in root)
        {
            if (child.name != "Branch boundary boulder" && child.name != "Trail edge marker") continue;
            float ground = float.NegativeInfinity;
            foreach (var hit in Physics.RaycastAll(child.position + Vector3.up * 20, Vector3.down, 40))
                if (hit.collider.transform.IsChildOf(group)) ground = Mathf.Max(ground, hit.point.y);
            var position = child.position;
            position.y = (float.IsNegativeInfinity(ground) ? -.05f : ground) + child.localScale.y * .5f;
            child.position = position;
        }
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = group.gameObject;
        return "Built irregular grass-blended shoulders and rounded banks for the main loop, hill climb and access road. Driving meshes and vehicle tuning retained.";
    }
}
