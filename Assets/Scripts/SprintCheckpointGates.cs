using UnityEngine;
using TMPro;

// Visual gates only: crossing is checked against an ordered plane, never a physics trigger.
public class SprintCheckpointGates : MonoBehaviour
{
    private Vector3[] points, directions;
    private float halfWidth;
    private Renderer[][] frames;
    private TextMeshPro[] labels;
    private Material frameMaterial;
    public void Initialize(Vector3[] positions, Vector3[] forwards, float width)
    {
        points = positions; directions = forwards; halfWidth = width;
        frames = new Renderer[points.Length][];
        labels = new TextMeshPro[points.Length];
        frameMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        frameMaterial.SetColor("_BaseColor", Color.white);
        frameMaterial.SetFloat("_Smoothness", .1f);
        for (int i = 0; i < points.Length; i++)
        {
            var gate = new GameObject("Sprint Gate " + (i + 1)).transform;
            gate.SetParent(transform, false); gate.position = points[i];
            gate.rotation = Quaternion.LookRotation(directions[i], Vector3.up);
            frames[i] = new Renderer[3];
            frames[i][0] = Bar(gate, new Vector3(-halfWidth - .4f, 2.4f, 0), new Vector3(.25f, 4.8f, .25f));
            frames[i][1] = Bar(gate, new Vector3(halfWidth + .4f, 2.4f, 0), new Vector3(.25f, 4.8f, .25f));
            frames[i][2] = Bar(gate, new Vector3(0, 4.8f, 0), new Vector3(halfWidth * 2 + 1f, .22f, .25f));
            var sign = new GameObject("Checkpoint Number").transform; sign.SetParent(gate, false); sign.localPosition = new Vector3(0, 5.8f, 0);
            labels[i] = sign.gameObject.AddComponent<TextMeshPro>();
            labels[i].text = i == 3 ? "FINISH  4 / 4" : "CHECKPOINT  " + (i + 1) + " / 4";
            labels[i].fontSize = 9; labels[i].alignment = TextAlignmentOptions.Center;
            labels[i].rectTransform.sizeDelta = new Vector2(18, 2);
            labels[i].GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        ShowNext(0);
    }
    private Renderer Bar(Transform parent, Vector3 position, Vector3 scale)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var collider = go.GetComponent<Collider>();
        collider.enabled = false;
        Destroy(collider);
        go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = scale;
        var renderer = go.GetComponent<Renderer>(); renderer.sharedMaterial = frameMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return renderer;
    }
    public bool Crossed(int gate, Vector3 before, Vector3 after)
    {
        var forward = directions[gate]; forward.y = 0; forward.Normalize();
        var right = Vector3.Cross(Vector3.up, forward);
        float a = Vector3.Dot(before - points[gate], forward), b = Vector3.Dot(after - points[gate], forward);
        if (a >= 0 || b < 0) return false;
        Vector3 crossing = Vector3.Lerp(before, after, -a / (b - a));
        return Mathf.Abs(Vector3.Dot(crossing - points[gate], right)) <= halfWidth
            && Mathf.Abs(crossing.y - points[gate].y) < 3f;
    }
    public void ShowNext(int next)
    {
        if (frames == null) return;
        for (int i = 0; i < frames.Length; i++)
        {
            Color color = i < next ? new Color(.2f, .5f, .35f) : i == next ? new Color(1f, .74f, .12f) : new Color(.16f, .56f, .65f);
            var block = new MaterialPropertyBlock(); block.SetColor("_BaseColor", color);
            foreach (var renderer in frames[i]) renderer.SetPropertyBlock(block);
            labels[i].color = color;
        }
    }
    private void LateUpdate()
    {
        var camera = Camera.main;
        if (camera == null || labels == null) return;
        foreach (var label in labels) label.transform.rotation = Quaternion.LookRotation(label.transform.position - camera.transform.position);
    }
    private void OnDestroy() { if (frameMaterial != null) Destroy(frameMaterial); }
}
