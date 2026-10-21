using UnityEngine;
using TMPro;
using System.Collections.Generic;

// Spawned only for Slalom; cone triggers add penalties without blocking the vehicle.
public class SlalomCourse : MonoBehaviour
{
    public const int GateCount = 6;
    public const float ConePenaltySeconds = 2f;
    private const float HalfWidth = 3f;
    private Vector3[] points;
    private Vector3 forward, right, finish;
    private float finishHalfWidth;
    private TestDriveCleanLapChallenge challenge;
    private TestDriveMovement vehicle;
    private readonly HashSet<SlalomCone> hitCones = new HashSet<SlalomCone>();
    private readonly List<SlalomCone> cones = new List<SlalomCone>();
    private Renderer[] lines;
    private TextMeshPro[] labels;
    private Material material;
    private Mesh coneMesh;
    public int ConesHit => hitCones.Count;
    public float PenaltySeconds => ConesHit * ConePenaltySeconds;

    public void Initialize(TestDriveCleanLapChallenge owner, TestDriveMovement target, Transform start, float width)
    {
        challenge = owner; vehicle = target;
        forward = start.forward; forward.y = 0; forward.Normalize();
        right = Vector3.Cross(Vector3.up, forward); finishHalfWidth = width;
        points = new Vector3[GateCount]; lines = new Renderer[GateCount + 1]; labels = new TextMeshPro[GateCount + 1];
        material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.SetColor("_BaseColor", Color.white); material.SetFloat("_Smoothness", .1f);
        coneMesh = CreateConeMesh();
        for (int i = 0; i < GateCount; i++)
        {
            points[i] = start.position + forward * ((i + 1) * 9f) + right * (i % 2 == 0 ? -2.1f : 2.1f);
            var gate = new GameObject("Slalom Gate " + (i + 1)).transform;
            gate.SetParent(transform, false); gate.position = points[i]; gate.rotation = Quaternion.LookRotation(forward);
            foreach (float side in new[] { -1f, 1f })
            {
                var cone = new GameObject("Gate " + (i + 1) + " Cone", typeof(MeshFilter), typeof(MeshRenderer), typeof(BoxCollider));
                cone.transform.SetParent(gate, false); cone.transform.localPosition = new Vector3(side * HalfWidth, .02f, 0);
                cone.GetComponent<MeshFilter>().sharedMesh = coneMesh;
                cone.GetComponent<MeshRenderer>().sharedMaterial = material;
                var trigger = cone.GetComponent<BoxCollider>(); trigger.center = new Vector3(0, .43f, 0); trigger.size = new Vector3(.64f, .86f, .64f); trigger.isTrigger = true;
                var hit = cone.AddComponent<SlalomCone>(); hit.Initialize(this); cones.Add(hit);
            }
            lines[i] = Line(gate, HalfWidth * 2);
            labels[i] = Label(gate, (i + 1) + " / 6");
        }
        finish = start.position + forward * 60f;
        var finishGate = new GameObject("Slalom Finish").transform; finishGate.SetParent(transform, false);
        finishGate.position = finish; finishGate.rotation = Quaternion.LookRotation(forward);
        lines[GateCount] = Line(finishGate, width * 2);
        labels[GateCount] = Label(finishGate, "FINISH");
        ResetAttempt();
    }
    private Renderer Line(Transform parent, float width)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = "Gate Line";
        var collider = go.GetComponent<Collider>(); collider.enabled = false; Destroy(collider);
        go.transform.SetParent(parent, false); go.transform.localPosition = new Vector3(0, .03f, 0);
        go.transform.localScale = new Vector3(width, .025f, .18f);
        var renderer = go.GetComponent<Renderer>(); renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return renderer;
    }
    private TextMeshPro Label(Transform parent, string value)
    {
        var go = new GameObject("Gate Number"); go.transform.SetParent(parent, false); go.transform.localPosition = Vector3.up * 2.7f;
        var text = go.AddComponent<TextMeshPro>(); text.text = value; text.fontSize = 5f;
        text.alignment = TextAlignmentOptions.Center; text.rectTransform.sizeDelta = new Vector2(8f, 1.5f);
        return text;
    }
    private Mesh CreateConeMesh()
    {
        const int sides = 12;
        var vertices = new List<Vector3>(); var triangles = new List<int>();
        float[] heights = { 0, .35f, .52f, .85f }, radii = { .32f, .21f, .16f, .035f };
        for (int ring = 0; ring < 3; ring++) for (int i = 0; i < sides; i++)
        {
            float a = i * Mathf.PI * 2 / sides, b = (i + 1) * Mathf.PI * 2 / sides;
            int index = vertices.Count;
            vertices.Add(new Vector3(Mathf.Cos(a) * radii[ring], heights[ring], Mathf.Sin(a) * radii[ring]));
            vertices.Add(new Vector3(Mathf.Cos(b) * radii[ring], heights[ring], Mathf.Sin(b) * radii[ring]));
            vertices.Add(new Vector3(Mathf.Cos(b) * radii[ring+1], heights[ring+1], Mathf.Sin(b) * radii[ring+1]));
            vertices.Add(new Vector3(Mathf.Cos(a) * radii[ring+1], heights[ring+1], Mathf.Sin(a) * radii[ring+1]));
            triangles.AddRange(new[] { index, index+2, index+1, index, index+3, index+2 });
        }
        var mesh = new Mesh { name = "Slalom Cone" }; mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
    }
    public void ReportCone(SlalomCone cone, Collider other)
    {
        if (challenge == null || challenge.State != TestDriveCleanLapChallenge.Phase.Running
            || other.GetComponentInParent<TestDriveMovement>() != vehicle || !hitCones.Add(cone)) return;
        cone.SetHit(true); challenge.ReportSlalomPenalty();
    }
    public bool Crossed(int gate, Vector3 before, Vector3 after)
        => CrossPlane(gate < GateCount ? points[gate] : finish, gate < GateCount ? HalfWidth : finishHalfWidth, before, after);
    private bool CrossPlane(Vector3 center, float width, Vector3 before, Vector3 after)
    {
        float a = Vector3.Dot(before-center, forward), b = Vector3.Dot(after-center, forward);
        if (a >= 0 || b < 0) return false;
        var point = Vector3.Lerp(before, after, -a / (b-a));
        return Mathf.Abs(Vector3.Dot(point-center, right)) <= width && Mathf.Abs(point.y-center.y) < 3f;
    }
    public void ResetAttempt() { hitCones.Clear(); foreach (var cone in cones) cone.SetHit(false); ShowNext(0); }
    public void ShowNext(int next)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            Color color = i < next ? new Color(.15f, .5f, .3f) : i == next ? new Color(1f, .74f, .12f) : new Color(.12f, .5f, .6f);
            var block = new MaterialPropertyBlock(); block.SetColor("_BaseColor", color); lines[i].SetPropertyBlock(block); labels[i].color = color;
            labels[i].gameObject.SetActive(i >= next);
        }
    }
    private void LateUpdate()
    {
        var camera = Camera.main; if (camera == null || labels == null) return;
        foreach (var label in labels) label.transform.rotation = Quaternion.LookRotation(label.transform.position-camera.transform.position);
    }
    private void OnDestroy() { if (material != null) Destroy(material); if (coneMesh != null) Destroy(coneMesh); }
}
