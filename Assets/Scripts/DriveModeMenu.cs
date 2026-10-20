using UnityEngine;

// Keeps the modal within the device safe area without changing the showroom layout.
public class DriveModeMenu : MonoBehaviour
{
    [SerializeField] private RectTransform dialog;
    private void OnEnable() => Fit();
    private void LateUpdate() => Fit();
    private void Fit() => ApplyLayout(Screen.safeArea, new Vector2Int(Screen.width, Screen.height));
    private void ApplyLayout(Rect area, Vector2Int screen)
    {
        if (dialog == null || screen.x <= 0 || screen.y <= 0) return;
        var root = (RectTransform)transform;
        Vector2 min = root.rect.min + Vector2.Scale(root.rect.size,
            new Vector2(Mathf.Clamp01(area.xMin / screen.x), Mathf.Clamp01(area.yMin / screen.y)));
        Vector2 max = root.rect.min + Vector2.Scale(root.rect.size,
            new Vector2(Mathf.Clamp01(area.xMax / screen.x), Mathf.Clamp01(area.yMax / screen.y)));
        float scale = Mathf.Min(1f, (max.x - min.x) / (dialog.rect.width + 48f),
            (max.y - min.y) / (dialog.rect.height + 48f));
        if (scale <= 0f) return;
        dialog.localScale = Vector3.one * scale;
        dialog.anchoredPosition = (min + max) * .5f - root.rect.center;
    }
}
