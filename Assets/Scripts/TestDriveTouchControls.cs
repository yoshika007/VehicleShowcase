using UnityEngine;
using UnityEngine.InputSystem;

public class TestDriveTouchControls : MonoBehaviour
{
    [SerializeField] private CanvasGroup controls;
    [SerializeField] private RectTransform safeArea;
    [SerializeField] private RectTransform hudSafeArea;
    [SerializeField] private RectTransform minimapPanel;
    [SerializeField] private RectTransform backButton;
    [SerializeField] private RectTransform steeringGroup;
    [SerializeField] private RectTransform pedalGroup;
    [SerializeField] private RectTransform resetButton;
    [SerializeField] private TestDriveRecoveryPrompt recoveryPrompt;
    [SerializeField] private RectTransform recoveryPanel;
    [SerializeField] private RectTransform cleanLapPanel;
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
        if (recoveryPrompt != null) recoveryPrompt.BindVehicle(target);
        if (vehicle != null) vehicle.ConfigureTouchInput(this);
        Refresh();
    }
    // Run after CanvasScaler so rotation and Simulator device changes use the new canvas size.
    private void LateUpdate() => Refresh();
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
        if (keyboardResetHint != null) keyboardResetHint.SetActive(!visible && originalHintActive);
        if (recoveryPrompt != null) recoveryPrompt.SetTouchMode(visible);
        ApplyLayout(Screen.safeArea, new Vector2Int(Screen.width, Screen.height), visible);
    }

    private void ApplyLayout(Rect area, Vector2Int screen, bool touchVisible)
    {
        RectTransform bounds = hudSafeArea != null ? hudSafeArea : safeArea;
        if (bounds == null || screen.x <= 0 || screen.y <= 0) return;
        // Clamp transient Simulator/rotation values before converting pixels to anchors.
        float xMin = Mathf.Clamp(area.xMin, 0, screen.x);
        float yMin = Mathf.Clamp(area.yMin, 0, screen.y);
        float xMax = Mathf.Clamp(area.xMax, xMin, screen.x);
        float yMax = Mathf.Clamp(area.yMax, yMin, screen.y);
        if (xMax <= xMin || yMax <= yMin) return;
        area = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        if (area != lastSafeArea || screen != lastScreen)
        {
            lastSafeArea = area; lastScreen = screen;
            bounds.anchorMin = new Vector2(area.xMin / screen.x, area.yMin / screen.y);
            bounds.anchorMax = new Vector2(area.xMax / screen.x, area.yMax / screen.y);
            bounds.offsetMin = bounds.offsetMax = Vector2.zero;
        }
        // Reserve room for the top HUD, speed readout and controls even on short screens.
        float scale = Mathf.Min(1f, bounds.rect.width / 1100f, bounds.rect.height / 760f);
        if (scale <= 0f) return;
        float margin = 36f * scale;
        Place(steeringGroup, scale, new Vector2(margin, margin));
        Place(pedalGroup, scale, new Vector2(-margin, margin));
        Place(resetButton, scale, new Vector2(0, margin));
        Place(recoveryPanel, scale, new Vector2(0, margin + 200f * scale));
        float backHeight = backButton != null ? backButton.rect.height : 68f;
        Place(minimapPanel, scale, new Vector2(margin, -margin - (backHeight + 24f) * scale));
        Place(backButton, scale, new Vector2(margin, -margin));
        Place(cleanLapPanel, scale, new Vector2(-margin, -margin));
        if (speedPanel != null)
        {
            // The readout follows the actual scaled pedal height, with a scaled gap.
            float pedalHeight = pedalGroup != null ? pedalGroup.rect.height * scale : 0f;
            float speedY = margin + (touchVisible ? pedalHeight + 24f * scale : 0f);
            Place(speedPanel, scale, new Vector2(-margin, speedY));
        }
    }

    private static void Place(RectTransform target, float scale, Vector2 position)
    {
        if (target == null) return;
        target.localScale = Vector3.one * scale;
        target.anchoredPosition = position;
    }
    public void ResetVehicle()
    {
        ReleaseAll();
        if (vehicle != null && vehicle.isActiveAndEnabled) vehicle.RecoverOrResetVehicle();
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
