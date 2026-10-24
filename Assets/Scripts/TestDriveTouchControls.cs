using UnityEngine;
using UnityEngine.InputSystem;

public class TestDriveTouchControls : MonoBehaviour
{
    [SerializeField] private CanvasGroup controls;
    [SerializeField] private RectTransform safeArea;
    [SerializeField] private RectTransform hudSafeArea;
    [SerializeField] private RectTransform minimapPanel;
    [SerializeField] private RectTransform backButton;
    [SerializeField] private RectTransform cameraButton;
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

    private RectTransform challengeHelpButton;
    private bool initialized;
    private TestDriveMovement vehicle;
    private Vector2 originalSpeedPosition;
    private Vector2 originalSpeedAnchorMin, originalSpeedAnchorMax, originalSpeedPivot;
    private TestDriveCameraFollow followCamera;
    private Vector2 originalSpeedSize;
    private RectTransform[] speedChildren;
    private Vector2[] originalSpeedChildSizes, originalSpeedChildPositions;
    private bool compactSpeedPanel;
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
        followCamera = FindAnyObjectByType<TestDriveCameraFollow>();
        if (speedPanel != null)
        {
            originalSpeedPosition = speedPanel.anchoredPosition;
            originalSpeedAnchorMin = speedPanel.anchorMin;
            originalSpeedAnchorMax = speedPanel.anchorMax;
            originalSpeedPivot = speedPanel.pivot;
            originalSpeedSize = speedPanel.sizeDelta;
            // Cache the authored layout so chase view and disabled controls restore exactly.
            string[] names = { "Accent", "SpeedHeading", "SpeedValue", "SpeedUnit" };
            speedChildren = new RectTransform[names.Length];
            originalSpeedChildSizes = new Vector2[names.Length];
            originalSpeedChildPositions = new Vector2[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                speedChildren[i] = speedPanel.Find(names[i]) as RectTransform;
                if (speedChildren[i] == null) continue;
                originalSpeedChildSizes[i] = speedChildren[i].sizeDelta;
                originalSpeedChildPositions[i] = speedChildren[i].anchoredPosition;
            }
        }
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
    public bool BindChallengeHelp(RectTransform button)
    {
        if (cameraButton == null || button == null) return false;
        challengeHelpButton = button;
        Refresh();
        return true;
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
        float steeringWidth = steeringGroup != null ? steeringGroup.rect.width : 0f;
        float pedalWidth = pedalGroup != null ? pedalGroup.rect.width : 0f;
        float resetWidth = resetButton != null ? resetButton.rect.width : 0f;
        float requiredWidth = Mathf.Max(1100f, steeringWidth + pedalWidth + resetWidth + 120f);
        float scale = Mathf.Min(1f, bounds.rect.width / requiredWidth, bounds.rect.height / 760f);
        if (scale <= 0f) return;
        float margin = 36f * scale;
        // Move the enlarged pairs inward, but reserve a clear gap around Reset.
        float maximumInset = (bounds.rect.width - (steeringWidth + pedalWidth + resetWidth + 48f) * scale) * .5f;
        float controlsInset = Mathf.Clamp(100f * scale, margin, Mathf.Max(margin, maximumInset));
        Place(steeringGroup, scale, new Vector2(controlsInset, margin));
        Place(pedalGroup, scale, new Vector2(-controlsInset, margin));
        Place(resetButton, scale, new Vector2(0, margin));
        Place(recoveryPanel, scale, new Vector2(0, margin + 200f * scale));
        float backHeight = backButton != null ? backButton.rect.height : 68f;
        Place(minimapPanel, scale, new Vector2(margin, -margin - (backHeight + 24f) * scale));
        Place(backButton, scale, new Vector2(margin, -margin));
        if (cameraButton != null && challengeHelpButton != null)
        {
            cameraButton.anchorMin = cameraButton.anchorMax = new Vector2(.5f, 1f);
            cameraButton.pivot = new Vector2(.5f, 1f);
            // Keep challenge help in its existing HUD position before moving the camera control.
            float sideWidth = Mathf.Max(backButton != null ? backButton.rect.width : 0f,
                cleanLapPanel != null ? cleanLapPanel.rect.width : 0f);
            float helpWidth = challengeHelpButton != null ? challengeHelpButton.rect.width + 24f : 0f;
            float groupWidth = cameraButton.rect.width + helpWidth;
            bool fitsTopRow = bounds.rect.width >= 2f * (margin + (sideWidth + 24f) * scale)
                + groupWidth * scale;
            float groupScale = scale;
            float cameraY = -margin;
            if (!fitsTopRow)
            {
                cameraY -= (backHeight + 24f) * scale;
                float rowSideWidth = Mathf.Max(minimapPanel != null ? minimapPanel.rect.width : 0f,
                    cleanLapPanel != null ? cleanLapPanel.rect.width : 0f);
                float available = bounds.rect.width - 2f * (margin + (rowSideWidth + 24f) * scale);
                float compactScale = Mathf.Min(scale, Mathf.Max(0f, available) / groupWidth);
                if (cameraButton.rect.height * compactScale >= 44f) groupScale = compactScale;
                else
                {
                    float mapBottom = backHeight + 24f + (minimapPanel != null ? minimapPanel.rect.height : 0f);
                    float hudBottom = Mathf.Max(mapBottom, cleanLapPanel != null ? cleanLapPanel.rect.height : 0f);
                    cameraY = -margin - (hudBottom + 24f) * scale;
                }
            }
            Place(cameraButton, groupScale, new Vector2(-helpWidth * .5f * groupScale, cameraY));
            PlaceChallengeHelp();
        }
        if (cameraButton != null && steeringGroup != null)
        {
            cameraButton.anchorMin = cameraButton.anchorMax = Vector2.zero;
            cameraButton.pivot = new Vector2(.5f, 0f);
            Place(cameraButton, scale, new Vector2(
                controlsInset + steeringGroup.rect.width * scale * .5f,
                margin + (steeringGroup.rect.height + 24f) * scale));
        }
        Place(cleanLapPanel, scale, new Vector2(-margin, -margin));
        if (speedPanel != null)
        {
            if (followCamera != null && followCamera.IsDriverView && resetButton != null)
            {
                SetCompactSpeedPanel(true);
                speedPanel.anchorMin = speedPanel.anchorMax = new Vector2(.5f, 0f);
                speedPanel.pivot = new Vector2(.5f, 0f);
                Place(speedPanel, scale, Vector2.zero);
                PlaceAbove(speedPanel, resetButton, 24f * scale);
                var speedRect = ScreenRect(speedPanel);
                bool overlapsSteering = steeringGroup != null && speedRect.Overlaps(ScreenRect(steeringGroup));
                bool overlapsPedals = pedalGroup != null && speedRect.Overlaps(ScreenRect(pedalGroup));
                if (overlapsSteering || overlapsPedals)
                {
                    var higher = steeringGroup == null ? pedalGroup : pedalGroup == null ? steeringGroup
                        : ScreenRect(steeringGroup).yMax > ScreenRect(pedalGroup).yMax ? steeringGroup : pedalGroup;
                    PlaceAbove(speedPanel, higher, 24f * scale, false);
                }
                // Keep the recovery message clear of the centered readout, including
                // when RESET grows into the larger RECOVER button.
                PlaceAbove(recoveryPanel, speedPanel, 24f * scale);
            }
            else
            {
                RestoreSpeedAnchors();
                // Chase view retains the readout above the pedals.
                float pedalHeight = pedalGroup != null ? pedalGroup.rect.height * scale : 0f;
                float speedY = margin + (touchVisible ? pedalHeight + 24f * scale : 0f);
                Place(speedPanel, scale, new Vector2(-controlsInset, speedY));
            }
        }
    }

    private void PlaceChallengeHelp()
    {
        if (challengeHelpButton == null || cameraButton == null) return;
        var parent = challengeHelpButton.parent as RectTransform;
        if (parent == null) return;
        // Both canvases keep their own scaler and sorting order. Convert between them
        // so the help button follows the camera row on rotation and safe-area changes.
        Vector3 point = cameraButton.TransformPoint(new Vector3(
            cameraButton.rect.xMax + 24f + challengeHelpButton.rect.width * .5f,
            cameraButton.rect.yMax - challengeHelpButton.rect.height * .5f, 0f));
        var cameraCanvas = cameraButton.GetComponentInParent<Canvas>().rootCanvas;
        var helpCanvas = challengeHelpButton.GetComponentInParent<Canvas>().rootCanvas;
        Camera fromCamera = cameraCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cameraCanvas.worldCamera;
        Camera toCamera = helpCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : helpCanvas.worldCamera;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(fromCamera, point);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, toCamera, out var local))
            challengeHelpButton.localPosition = new Vector3(local.x, local.y, 0f);
        float cameraUnit = Vector2.Distance(
            RectTransformUtility.WorldToScreenPoint(fromCamera, cameraButton.TransformPoint(Vector3.zero)),
            RectTransformUtility.WorldToScreenPoint(fromCamera, cameraButton.TransformPoint(Vector3.up)));
        float helpUnit = Vector2.Distance(
            RectTransformUtility.WorldToScreenPoint(toCamera, parent.TransformPoint(Vector3.zero)),
            RectTransformUtility.WorldToScreenPoint(toCamera, parent.TransformPoint(Vector3.up)));
        if (helpUnit > .0001f) challengeHelpButton.localScale = Vector3.one * (cameraUnit / helpUnit);
    }

    private void RestoreSpeedAnchors()
    {
        SetCompactSpeedPanel(false);
        speedPanel.anchorMin = originalSpeedAnchorMin;
        speedPanel.anchorMax = originalSpeedAnchorMax;
        speedPanel.pivot = originalSpeedPivot;
    }

    private void SetCompactSpeedPanel(bool compact)
    {
        if (compactSpeedPanel == compact) return;
        compactSpeedPanel = compact;
        speedPanel.sizeDelta = compact ? new Vector2(260f, 150f) : originalSpeedSize;
        for (int i = 0; i < speedChildren.Length; i++)
        {
            var child = speedChildren[i];
            if (child == null) continue;
            var size = originalSpeedChildSizes[i];
            var position = originalSpeedChildPositions[i];
            if (compact)
            {
                // Tighten padding and text boxes, never the font or transform scale.
                switch (child.name)
                {
                    case "Accent": size.x = speedPanel.rect.width; break;
                    case "SpeedHeading": position = new Vector2(12f, -8f); break;
                    case "SpeedValue": size.x = 160f; position.x = 12f; break;
                    case "SpeedUnit": position.x = -10f; break;
                }
            }
            child.sizeDelta = size;
            child.anchoredPosition = position;
        }
    }

    private static Rect ScreenRect(RectTransform rect)
    {
        var canvas = rect.GetComponentInParent<Canvas>().rootCanvas;
        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        var min = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.min));
        var max = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.max));
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private static void PlaceAbove(RectTransform panel, RectTransform below, float gap, bool center = true)
    {
        if (panel == null || below == null || !(panel.parent is RectTransform parent)) return;
        var fromCanvas = below.GetComponentInParent<Canvas>().rootCanvas;
        var toCanvas = panel.GetComponentInParent<Canvas>().rootCanvas;
        Camera fromCamera = fromCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : fromCanvas.worldCamera;
        Camera toCamera = toCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : toCanvas.worldCamera;
        var top = below.TransformPoint(new Vector3(below.rect.center.x, below.rect.yMax, 0f));
        var screenPoint = RectTransformUtility.WorldToScreenPoint(fromCamera, top);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, toCamera, out var local))
            panel.localPosition = new Vector3(center ? local.x : panel.localPosition.x, local.y + gap, 0f);
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
        if (initialized && speedPanel != null)
        {
            RestoreSpeedAnchors();
            speedPanel.anchoredPosition = originalSpeedPosition;
        }
        if (initialized && keyboardResetHint != null) keyboardResetHint.SetActive(originalHintActive);
        if (controls != null) { controls.alpha = 0; controls.interactable = controls.blocksRaycasts = false; }
    }
}
