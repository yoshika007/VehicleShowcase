using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public static class UpgradeTruckHill
{
    const int Samples = 240;
    const string Dir = "Assets/Environments/OffroadCourse/";
    public static Vector3[] Points()
    {
        var points = new Vector3[Samples + 1];
        var distances = new float[points.Length];
        for (int i = 0; i <= Samples; i++)
        {
            float u = i / (float)Samples;
            points[i] = new Vector3(235 - 80 * u, 0.04f, 30 + 48 * Mathf.Pow(Mathf.Sin(Mathf.PI * u), 2));
            if (i > 0) distances[i] = distances[i - 1] + Vector3.Distance(points[i], points[i - 1]);
        }
        float descentLength = distances[Samples] - 18 - 57;
        for (int i = 0; i <= Samples; i++)
        {
            float d = distances[i];
            float height = d < 20 ? 0.3f * Mathf.SmoothStep(0, 1, d / 20) :
                d < 47 ? 0.3f + 5.7f * Ramp(d - 20, 27, 8) :
                d < 57 ? 6 : 6 * (1 - Ramp(d - 57, descentLength, 8));
            points[i].y += height;
        }
        return points;
    }
    // Integrates an eased slope: rounded transitions, constant grade through the middle.
    static float Ramp(float distance, float length, float ease)
    {
        float d = Mathf.Clamp(distance, 0, length);
        float area = d < ease ? d * d / (2 * ease) :
            d > length - ease ? length - ease - (length - d) * (length - d) / (2 * ease) : d - ease / 2;
        return area / (length - ease);
    }
    static void UpdateRibbon(Transform go, Vector3[] points, float width, float yOffset)
    {
        var v = new Vector3[points.Length * 2];
        var uv = new Vector2[v.Length];
        var triangles = new List<int>();
        float distance = 0;
        for (int i = 0; i < points.Length; i++)
        {
            var tangent = points[Mathf.Min(i + 1, Samples)] - points[Mathf.Max(i - 1, 0)];
            var side = Vector3.Cross(Vector3.up, tangent).normalized * width / 2;
            v[i * 2] = points[i] + side + Vector3.up * yOffset;
            v[i * 2 + 1] = points[i] - side + Vector3.up * yOffset;
            if (i > 0) distance += Vector3.Distance(points[i - 1], points[i]);
            uv[i * 2] = new Vector2(0, distance / 4);
            uv[i * 2 + 1] = new Vector2(width / 4, distance / 4);
            if (i == Samples) continue;
            int j = i * 2;
            triangles.AddRange(new[] { j, j + 1, j + 2, j + 1, j + 3, j + 2 });
        }
        UpdateMesh(go, v, triangles.ToArray(), uv);
    }
    static void UpdateMesh(Transform go, Vector3[] vertices, int[] triangles, Vector2[] uv)
    {
        var mesh = go.GetComponent<MeshFilter>().sharedMesh;
        mesh.Clear(); mesh.vertices = vertices; mesh.triangles = triangles; mesh.uv = uv;
        mesh.RecalculateNormals(); mesh.RecalculateBounds(); EditorUtility.SetDirty(mesh);
        if (go.TryGetComponent<MeshCollider>(out var collider)) { collider.sharedMesh = null; collider.sharedMesh = mesh; }
    }
    static void UpdateBanks(Transform go, Vector3[] points)
    {
        const int across = 24;
        var v = new Vector3[points.Length * (across + 1)];
        var uv = new Vector2[v.Length];
        var triangles = new List<int>();
        for (int i = 0; i < points.Length; i++)
        {
            var tangent = points[Mathf.Min(i + 1, Samples)] - points[Mathf.Max(i - 1, 0)];
            var side = Vector3.Cross(Vector3.up, tangent).normalized;
            for (int j = 0; j <= across; j++)
            {
                float offset = Mathf.Lerp(-18, 18, j / (float)across);
                float blend = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(7.5f, 18, Mathf.Abs(offset)));
                var p = points[i] + side * offset;
                p.y = -0.045f + (points[i].y - 0.04f) * blend;
                // Keep the shared dirt straight clear where the earth bank approaches the merge.
                if (p.z < 39 && p.x >= 148 && p.x <= 242)
                {
                    float mainHeight = 1.4f * Mathf.Pow(Mathf.Sin(Mathf.Clamp01((235 - p.x) / 80) * Mathf.PI), 4);
                    p.y = Mathf.Min(p.y, mainHeight - 0.045f);
                }
                int k = i * (across + 1) + j; v[k] = p; uv[k] = new Vector2(p.x / 4, p.z / 4);
                if (i == Samples || j == across) continue;
                triangles.AddRange(new[] { k, k + across + 1, k + 1, k + 1, k + across + 1, k + across + 2 });
            }
        }
        UpdateMesh(go, v, triangles.ToArray(), uv);
    }
    public static string Main()
    {
        if (EditorApplication.isPlaying) throw new System.Exception("Edit mode required.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new System.Exception("Save pending scene edits before updating the course.");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/TestTrackScene.unity", OpenSceneMode.Single);
        var root = GameObject.Find("Offroad Course").transform;
        var points = Points();
        UpdateRibbon(root.Find("Truck ridge branch"), points, 12, 0);
        var climb = root.Find("Truck ridge branch");
        var surface = new SerializedObject(climb.GetComponent<TestDriveSurface>());
        surface.FindProperty("gripMultiplier").floatValue = 0.85f;
        surface.FindProperty("dustColor").colorValue = new Color(.73f, .59f, .41f, .42f);
        surface.ApplyModifiedPropertiesWithoutUndo();
        climb.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Dir + "Packed Dirt.mat");
        UpdateRibbon(root.Find("Branch shoulders"), points, 15, -0.025f);
        UpdateBanks(root.Find("Truck ridge branch earth banks"), points);
        int index = 0;
        foreach (Transform child in root)
        {
            if (child.name != "Branch boundary boulder") continue;
            float u = 0.16f + (index + 0.5f) / 16 * 0.68f;
            int i = Mathf.RoundToInt(u * Samples);
            var tangent = points[i + 1] - points[i - 1];
            child.position = points[i] + Vector3.Cross(Vector3.up, tangent).normalized * 8.5f;
            index++;
        }
        var sign = root.GetComponentsInChildren<TextMesh>().First(t => t.text.StartsWith("TRUCK RIDGE"));
        sign.text = "HILL CLIMB\n17 DEG | TRUCK ROUTE";
        var roadNames = new[] { "Dirt Hill 1", "Dirt Hill 2", "Gravel Corner 1", "Gravel Corner 2", "Dirt access road", "Truck ridge branch" };
        var filters = roadNames.Select(n => root.Find(n).GetComponent<MeshFilter>()).ToList();
        filters.Add(GameObject.Find("Asphalt — 12m wide").GetComponent<MeshFilter>());
        var map = AssetDatabase.LoadAssetAtPath<Mesh>(Dir + "CourseMap.asset");
        map.Clear(); map.CombineMeshes(filters.Select(f => new CombineInstance { mesh = f.sharedMesh, transform = f.transform.localToWorldMatrix }).ToArray());
        EditorUtility.SetDirty(map);
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = root.Find("Truck ridge branch").gameObject;
        if (SceneView.lastActiveSceneView) SceneView.lastActiveSceneView.LookAt(new Vector3(195, 3, 60), Quaternion.Euler(35, 0, 0), 70);
        float maxUp = 0, maxDown = 0;
        for (int i = 1; i < points.Length; i++)
        {
            var delta = points[i] - points[i - 1];
            float grade = Mathf.Atan2(delta.y, new Vector2(delta.x, delta.z).magnitude) * Mathf.Rad2Deg;
            maxUp = Mathf.Max(maxUp, grade); maxDown = Mathf.Min(maxDown, grade);
        }
        return $"Truck branch: 12m wide, 6m summit, 10m plateau, ascent {maxUp:F1} degrees, descent {-maxDown:F1} degrees. Vehicle tuning unchanged.";
    }
}
