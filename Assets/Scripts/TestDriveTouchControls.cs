using UnityEngine;
using UnityEngine.InputSystem;

public class TestDriveTouchControls : MonoBehaviour
{
    [SerializeField] private CanvasGroup controls;
    [SerializeField] private RectTransform safeArea;
    [SerializeField] private RectTransform steeringGroup;
    [SerializeField] private RectTransform pedalGroup;
    [SerializeField] private RectTransform resetButton;
    [SerializeField] private TestDriveTouchButton left;
    [SerializeField] private TestDriveTouchButton right;
    [SerializeField] private TestDriveTouchButton gas;
    [SerializeField] private TestDriveTouchButton reverse;
    [SerializeField] private RectTransform speedPanel;
    [SerializeField] private GameObject keyboardResetHint;
    [SerializeField] private bool showInEditor = true;

    private bool initialized;
    private TestDriveMovement vehicle;
    private Vector2 originalSpeedPosition;
    private bool originalHintActive;
    private bool showing;
    private Rect lastSafeArea;
    private Vector2Int lastScreen;
    public bool Forward => showing && gas != null && gas.IsHeld;
    public bool Reverse => showing && reverse != null && reverse.IsHeld;
    public float Steering => showing ? (right != null && right.IsHeld ? 1f : 0f) - (left != null && left.IsHeld ? 1f : 0f) : 0f;
    public bool IsVisible => showing;

    private void Awake() => Initialize();
    private void Initialize()
    {
        if (initialized) return;
        initialized = true;
        if (speedPanel != null) originalSpeedPosition = speedPanel.anchoredPosition;
        if (keyboardResetHint != null) originalHintActive = keyboardResetHint.activeSelf;
    }
    public void BindVehicle(TestDriveMovement target)
    {
        Initialize();
        ReleaseAll();
        vehicle = target;
        if (vehicle != null) vehicle.ConfigureTouchInput(this);
        Refresh();
    }
    private void Update() => Refresh();
    private void Refresh()
    {
        Initialize();
        bool touchDevice = Application.isMobilePlatform || Touchscreen.current != null;
#if UNITY_EDITOR
        touchDevice |= showInEditor;
#endif
        bool visible = touchDevice && vehicle != null && vehicle.isActiveAndEnabled;
        if (visible != showing)
        {
            ReleaseAll();
            showing = visible;
        }
        if (controls != null)
        {
            controls.alpha = visible ? 1f : 0f;
            controls.interactable = controls.blocksRaycasts = visible;
        }
        if (speedPanel != null) speedPanel.anchoredPosition = originalSpeedPosition + (visible ? new Vector2(0, 220) : Vector2.zero);
        if (keyboardResetHint != null) keyboardResetHint.SetActive(!visible && originalHintActive);
        Rect area = Screen.safeArea;
        var size = new Vector2Int(Screen.width, Screen.height);
        if (safeArea == null || size.x <= 0 || size.y <= 0) return;
        if (area != lastSafeArea || size != lastScreen)
        {
            lastSafeArea = area; lastScreen = size;
            safeArea.anchorMin = new Vector2(area.xMin / size.x, area.yMin / size.y);
            safeArea.anchorMax = new Vector2(area.xMax / size.x, area.yMax / size.y);
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
        }
        // CanvasScaler settles after the screen change; use its current width each frame.
        float scale = Mathf.Min(1f, safeArea.rect.width / 1100f);
        if (steeringGroup != null) steeringGroup.localScale = Vector3.one * scale;
        if (pedalGroup != null) pedalGroup.localScale = Vector3.one * scale;
        if (resetButton != null) resetButton.localScale = Vector3.one * scale;
    }
    public void ResetVehicle()
    {
        ReleaseAll();
        if (vehicle != null && vehicle.isActiveAndEnabled) vehicle.ResetVehicle();
    }
    public void ReleaseAll()
    {
        if (left != null) left.Release();
        if (right != null) right.Release();
        if (gas != null) gas.Release();
        if (reverse != null) reverse.Release();
    }
    private void OnApplicationFocus(bool focused) { if (!focused) ReleaseAll(); }
    private void OnApplicationPause(bool paused) { if (paused) ReleaseAll(); }
    private void OnDisable()
    {
        ReleaseAll(); showing = false;
        if (initialized && speedPanel != null) speedPanel.anchoredPosition = originalSpeedPosition;
        if (initialized && keyboardResetHint != null) keyboardResetHint.SetActive(originalHintActive);
        if (controls != null) { controls.alpha = 0; controls.interactable = controls.blocksRaycasts = false; }
    }
}
