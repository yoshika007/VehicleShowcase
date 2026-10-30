using UnityEngine;
using UnityEngine.UI;

// One non-interactive UI mesh, drawn behind the result panel and removed on close.
public sealed class ChallengeConfetti : MaskableGraphic
{
    private const int PieceCount = 96;
    private const float Duration = 3.8f;
    private float elapsed;
    private static readonly Color32[] Colors = {
        new Color32(255, 196, 67, 235), new Color32(110, 220, 201, 235),
        new Color32(250, 139, 115, 235), new Color32(244, 236, 195, 235)
    };

    public static void Play(Transform overlay)
    {
        var existing = overlay.Find("Success Confetti");
        if (existing != null) { existing.gameObject.SetActive(false); Destroy(existing.gameObject); }
        var go = new GameObject("Success Confetti", typeof(RectTransform), typeof(CanvasRenderer), typeof(ChallengeConfetti));
        var rect = (RectTransform)go.transform;
        rect.SetParent(overlay, false);
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.SetAsFirstSibling();
        go.GetComponent<ChallengeConfetti>().raycastTarget = false;
    }

    protected override void OnEnable() { base.OnEnable(); elapsed = 0f; }
    protected override void OnDisable()
    {
        base.OnDisable();
        if (Application.isPlaying) { gameObject.SetActive(false); Destroy(gameObject); }
    }
    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        if (elapsed >= Duration) { Destroy(gameObject); return; }
        SetVerticesDirty();
    }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        var r = rectTransform.rect;
        for (int i = 0; i < PieceCount; i++)
        {
            // Each side remains clear of the center result panel and its controls.
            float side = i % 2 == 0 ? -1f : 1f;
            float lane = .29f + .19f * ((i * 37 % 101) / 100f);
            float stagger = (i * 53 % 101) / 100f;
            float x = side * r.width * lane + Mathf.Sin(elapsed * 2f + i) * 20f;
            float y = r.height * (.55f + stagger * .60f) - elapsed * r.height * (.32f + .13f * stagger);
            float size = 10f + (i % 4) * 2.5f;
            float angle = elapsed * (1.6f + (i % 3) * .5f) + i;
            Vector2 dx = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * size;
            Vector2 dy = new Vector2(-dx.y, dx.x) * .55f;
            int start = vh.currentVertCount;
            var color = Colors[i % Colors.Length];
            UIVertex v = UIVertex.simpleVert; v.color = color;
            v.position = new Vector2(x, y) - dx - dy; vh.AddVert(v);
            v.position = new Vector2(x, y) - dx + dy; vh.AddVert(v);
            v.position = new Vector2(x, y) + dx + dy; vh.AddVert(v);
            v.position = new Vector2(x, y) + dx - dy; vh.AddVert(v);
            vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
