using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(UnityEngine.UI.Button))]
public class TestDriveButtonFeedback : MonoBehaviour, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler, IDeselectHandler, ISubmitHandler
{
    [SerializeField] private TestDriveButtonFace face;
    [SerializeField] private bool expandDownwards;
    [SerializeField] private RectTransform clearanceHint;
    private UnityEngine.UI.Button button;
    private UnityEngine.UI.Outline outline;
    private Canvas canvas;
    private RectTransform rect;
    private Vector3 restingScale;
    private Vector2 restingPosition;
    private Vector2 restingHintPosition;
    private float restingHeight;
    private Color restingOutline;
    private int? pointer;
    private float emphasis, submitUntil;

    private void Awake()
    {
        button = GetComponent<UnityEngine.UI.Button>();
        outline = GetComponent<UnityEngine.UI.Outline>();
        canvas = GetComponentInParent<Canvas>();
        rect = (RectTransform)transform;
        restingScale = transform.localScale;
        restingPosition = rect.anchoredPosition;
        restingHeight = rect.rect.height;
        if (clearanceHint != null) restingHintPosition = clearanceHint.anchoredPosition;
        if (outline != null) restingOutline = outline.effectColor;
    }
    private void Update()
    {
        bool available = button.IsInteractable();
        bool pressed = available && (pointer.HasValue || Time.unscaledTime < submitUntil);
        // Touch-down is immediate; release settles in one tenth of a second.
        emphasis = pressed ? 1f : Mathf.MoveTowards(emphasis, 0f, Time.unscaledDeltaTime / .1f);
        transform.localScale = restingScale * Mathf.Lerp(1f, .96f, emphasis);
        if (face != null) face.SetAppearance(emphasis, !available);
        if (outline != null)
            outline.effectColor = Color.Lerp(restingOutline, new Color(1f, .9f, .6f, .85f), emphasis);
    }
    private void LateUpdate()
    {
        // Keep a useful minimum hit height even when CanvasScaler reduces a small screen.
        if (canvas == null || canvas.scaleFactor <= 0f) return;
        float density = Screen.dpi > 0f ? Mathf.Max(1f, Screen.dpi / 160f) : 1f;
        float height = Mathf.Max(restingHeight, 50f * density / canvas.scaleFactor);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        float edge = expandDownwards ? -(1f - rect.pivot.y) : rect.pivot.y;
        rect.anchoredPosition = restingPosition + Vector2.up * ((height - restingHeight) * edge);
        if (clearanceHint != null)
            clearanceHint.anchoredPosition = restingHintPosition + Vector2.up * (height - restingHeight);
    }
    public void OnPointerDown(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Left || !button.IsInteractable() || pointer.HasValue) return;
        pointer = e.pointerId;
        emphasis = 1f;
        transform.localScale = restingScale * .96f;
    }
    public void OnPointerUp(PointerEventData e) { if (pointer == e.pointerId) pointer = null; }
    public void OnPointerExit(PointerEventData e) { if (pointer == e.pointerId) pointer = null; }
    public void OnDeselect(BaseEventData e) { pointer = null; }
    public void OnSubmit(BaseEventData e) { if (button.IsInteractable()) submitUntil = Time.unscaledTime + .1f; }
    private void OnApplicationFocus(bool focused) { if (!focused) { pointer = null; submitUntil = 0f; } }
    private void OnDisable()
    {
        pointer = null; emphasis = submitUntil = 0f;
        if (button == null) return;
        transform.localScale = restingScale;
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, restingHeight);
        rect.anchoredPosition = restingPosition;
        if (clearanceHint != null) clearanceHint.anchoredPosition = restingHintPosition;
        if (outline != null) outline.effectColor = restingOutline;
        if (face != null) face.SetAppearance(0f, false);
    }
}
