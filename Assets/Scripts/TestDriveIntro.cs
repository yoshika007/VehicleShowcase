using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Runs once for a showroom selection. Holds the vehicle and hands off before any
// challenge briefing/countdown. Uses unscaled time and restores all captured state.
public class TestDriveIntro : MonoBehaviour
{
    private const float CarSeconds = 1.8f;
    private const float FeatureSeconds = 1.2f;
    private const float HandoffSeconds = .8f;
    private const float BottomLetterboxHeight = .18f;
    private const float FeaturesEnd = CarSeconds + FeatureSeconds * 2;
    public const float Duration = FeaturesEnd + HandoffSeconds;
    public bool IsPlaying { get; private set; }
    public float Elapsed => elapsed;
    private TestDriveMovement vehicle;
    private TestDriveCameraFollow follow;
    private TestDriveTouchControls controls;
    private Camera view;
    private Action completed;
    private readonly List<Canvas> hiddenHud = new List<Canvas>();
    private bool wasStopped, followEnabled;
    private float fieldOfView, nearClip, elapsed;
    private Vector3 carCenter, carForward, chasePosition, returnPosition;
    private Quaternion chaseRotation, returnRotation;
    private float radius, carHeight, returnFov;
    private TMP_Text title, subtitle;
    private RectTransform safeArea;
    private GameObject previousSelection;
    private Light fill;
    private Shot first, second;
    private bool returning;

    private struct Shot
    {
        public string title, subtitle;
        public Vector3 focus, from, to;
        public Shot(string heading, string detail, Vector3 point, Vector3 start, Vector3 end)
        { title = heading; subtitle = detail; focus = point; from = start; to = end; }
    }

    public void Initialize(TestDriveMovement car, TestDriveCameraFollow cameraFollow,
        TestDriveTouchControls touch, Action onComplete)
    {
        vehicle = car; follow = cameraFollow; controls = touch; completed = onComplete;
        view = follow.GetComponent<Camera>();
        if (view == null) { onComplete?.Invoke(); Destroy(gameObject); return; }
        wasStopped = vehicle.IsChallengeStopped;
        vehicle.SetChallengeStopped(true);
        if (controls != null) controls.ReleaseAll();
        followEnabled = follow.enabled;
        follow.SnapToTarget();
        chasePosition = view.transform.position; chaseRotation = view.transform.rotation;
        fieldOfView = view.fieldOfView; nearClip = view.nearClipPlane;
        follow.enabled = false;
        var bounds = new Bounds(vehicle.transform.position + Vector3.up, Vector3.zero);
        foreach (var renderer in vehicle.GetComponentsInChildren<Renderer>())
            if (renderer is MeshRenderer || renderer is SkinnedMeshRenderer) bounds.Encapsulate(renderer.bounds);
        carCenter = bounds.center; carForward = vehicle.transform.forward; carForward.y = 0;
        radius = Mathf.Max(5.8f, bounds.size.magnitude * 1.1f);
        carHeight = Mathf.Max(2f, bounds.size.y * 1.1f);
        foreach (var canvas in FindObjectsByType<Canvas>())
            if (canvas.enabled && canvas.gameObject.scene == gameObject.scene)
            { hiddenHud.Add(canvas); canvas.enabled = false; }
        BuildShots();
        BuildOverlay();
        var lamp = new GameObject("Intro car fill", typeof(Light));
        lamp.transform.SetParent(transform, false); fill = lamp.GetComponent<Light>();
        fill.type = LightType.Point; fill.range = radius * 2; fill.intensity = 28;
        fill.color = new Color(.85f, .91f, 1f); fill.shadows = LightShadows.None;
        IsPlaying = true;
        Apply(0);
    }

    private void BuildShots()
    {
        string scene = SceneManager.GetActiveScene().name;
        if (scene == "HighlandClimbScene")
        {
            first = new Shot("HIGHLAND CLIMB", "Steep climbs and rolling dirt trails", Vector3.zero, new Vector3(100, 90, -120), new Vector3(10, 75, -100));
            second = new Shot("HILL COUNTRY", "Choose your line and climb carefully", new Vector3(0, 20, 100), new Vector3(90, 80, 50), new Vector3(20, 65, 80));
        }
        else if (scene == "PineRidgeScene")
        {
            first = new Shot("PINE RIDGE TRAIL", "Explore a forest dirt circuit", Vector3.zero, new Vector3(100, 80, -120), new Vector3(10, 65, -100));
            second = new Shot("FOREST AND RIDGE", "Follow the trail through the hills", new Vector3(0, 8, 120), new Vector3(90, 65, 50), new Vector3(20, 55, 80));
        }
        else if (scene == "HarborStreetScene")
        {
            first = new Shot("HARBOR STREET", "City climbs, a hotel hairpin and the marina",
                new Vector3(10, 10, -90), new Vector3(-130, 145, -320), new Vector3(-60, 120, -285));
            second = new Shot("SEASIDE TUNNEL", "Brake for the tight corners beyond the covered coast road",
                new Vector3(185, 4, 45), new Vector3(290, 65, -5), new Vector3(275, 48, 70));
        }
        else if (scene == "MountainPassScene")
        {
            first = new Shot("MOUNTAIN PASS", "Three hairpins climb toward the summit tunnel",
                new Vector3(0, 50, 130), new Vector3(280, 190, -110), new Vector3(235, 170, -70));
            second = new Shot("LINKED CORNERS", "Find your line through the technical S bends",
                new Vector3(50, 0, -365), new Vector3(135, 75, -445), new Vector3(5, 60, -440));
        }
        else if (scene == "LakeTownScene")
        {
            first = new Shot("LAKE TOWN", "Lakeside roads and the marina",
                new Vector3(-60, 0, 165), new Vector3(-130, 45, 225), new Vector3(-35, 35, 235));
            second = new Shot("OLD TOWN", "Village streets meet winding lakeside bends",
                new Vector3(30, 0, -5), new Vector3(115, 105, -145), new Vector3(10, 85, -150));
        }
        else
        {
            first = new Shot("TEST CIRCUIT", "Fast straights and sweeping turns",
                Vector3.zero, new Vector3(85, 75, -95), new Vector3(-20, 60, -100));

            var feature = GameObject.Find("Dirt Hill 1");
            Vector3 point = feature != null && feature.TryGetComponent<Collider>(out var collider)
                ? collider.bounds.center : new Vector3(30, 0, 80);
            second = new Shot("BEYOND THE ASPHALT", "Gravel, hills and mixed-surface routes",
                point, point + new Vector3(55, 35, -50), point + new Vector3(5, 30, -60));
        }
        if (TestDriveSelection.Mode != TestDriveSelection.DriveMode.Challenge) return;
        var trail = FindAnyObjectByType<OffroadTrailCourse>();
        if (trail != null)
        {
            first = RouteShot(trail.Route, .25f, "OFF-ROAD TRAIL", "Follow the dirt loop over rolling terrain");
            second = RouteShot(trail.Route, .7f, "GRAVEL CORNERS", "Eight gates test your grip and control");
        }
        else
        {
            second = new Shot(TestDriveSelection.IsNight ? "NIGHT LAP" : "CLEAN LAP",
                    "Smooth braking and clean corners make the difference", new Vector3(35, 0, 25),
                    new Vector3(80, 28, -5), new Vector3(65, 22, 65));
        }
    }

    private static Shot RouteShot(Vector3[] route, float fraction, string heading, string detail)
    {
        int i = Mathf.Clamp(Mathf.RoundToInt((route.Length - 2) * fraction), 0, route.Length - 2);
        var direction = (route[i + 1] - route[i]).normalized;
        var side = Vector3.Cross(Vector3.up, direction).normalized;
        return new Shot(heading, detail, route[i], route[i] + side * 35 - direction * 25 + Vector3.up * 30,
            route[i] + side * 22 - direction * 15 + Vector3.up * 25);
    }

    private void Update()
    {
        if (!IsPlaying) return;
        var keys = Keyboard.current;
        if ((keys != null && (keys.enterKey.wasPressedThisFrame || keys.spaceKey.wasPressedThisFrame))
            || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame))
        { Skip(); return; }
        elapsed += Time.unscaledDeltaTime;
        if (elapsed >= Duration) { Finish(true); return; }
        Apply(elapsed);
    }

    private void Apply(float time)
    {
        FitSafeArea();
        view.nearClipPlane = .1f;
        if (time < CarSeconds)
        {
            float t = Mathf.SmoothStep(0, 1, time / CarSeconds);
            Vector3 offset = Quaternion.AngleAxis(Mathf.Lerp(-55, 45, t), Vector3.up) * carForward * radius;
            Vector3 position = carCenter + offset + Vector3.up * Mathf.Lerp(carHeight, carHeight * 1.25f, t);
            Pose(position, carCenter, Mathf.Lerp(48, 43, t));
            var model = vehicle.GetComponent<Vehicle>();
            title.text = !string.IsNullOrEmpty(TestDriveSelection.VehicleName) ? TestDriveSelection.VehicleName.ToUpperInvariant()
                : model != null ? model.DisplayName.ToUpperInvariant() : "YOUR CAR";
            subtitle.text = CourseName() + (TestDriveSelection.IsNight ? "  /  NIGHT DRIVE" : "  /  DAY DRIVE");
            fill.enabled = TestDriveSelection.IsNight; fill.transform.position = position;
        }
        else if (time < FeaturesEnd)
        {
            fill.enabled = false;
            bool firstShot = time < CarSeconds + FeatureSeconds;
            var shot = firstShot ? first : second;
            float t = Mathf.SmoothStep(0, 1, (time - CarSeconds - (firstShot ? 0 : FeatureSeconds)) / FeatureSeconds);
            Pose(Vector3.Lerp(shot.from, shot.to, t), shot.focus, 53);
            title.text = shot.title; subtitle.text = shot.subtitle;
        }
        else
        {
            fill.enabled = false;
            if (!returning)
            {
                returning = true;
                // Cut back beside the car, then ease into chase view; never sweep
                // across hundreds of metres of terrain or through village buildings.
                returnPosition = chasePosition - carForward * 3f + Vector3.up * 1.5f;
                returnRotation = Quaternion.LookRotation(carCenter - returnPosition);
                returnFov = 48f;
            }
            float t = Mathf.SmoothStep(0, 1, (time - FeaturesEnd) / HandoffSeconds);
            view.transform.SetPositionAndRotation(Vector3.Lerp(returnPosition, chasePosition, t),
                Quaternion.Slerp(returnRotation, chaseRotation, t));
            view.fieldOfView = Mathf.Lerp(returnFov, fieldOfView, t);
            title.text = "READY TO DRIVE"; subtitle.text = CourseName();
        }
    }

    private static string CourseName() => SceneManager.GetActiveScene().name == "HighlandClimbScene" ? "HIGHLAND CLIMB"
        : SceneManager.GetActiveScene().name == "PineRidgeScene" ? "PINE RIDGE TRAIL"
        : SceneManager.GetActiveScene().name == "HarborStreetScene" ? "HARBOR STREET"
        : SceneManager.GetActiveScene().name == "MountainPassScene" ? "MOUNTAIN PASS"
        : SceneManager.GetActiveScene().name == "LakeTownScene" ? "LAKE TOWN" : "TEST CIRCUIT";
    private void Pose(Vector3 position, Vector3 focus, float fov)
    {
        view.transform.SetPositionAndRotation(position, Quaternion.LookRotation(focus - position));
        view.fieldOfView = fov;
    }

    public void Skip() => Finish(true);
    public void Cancel() => Finish(false);
    private void Finish(bool handoff)
    {
        if (!IsPlaying) return;
        IsPlaying = false;
        if (controls != null) controls.ReleaseAll();
        foreach (var canvas in hiddenHud) if (canvas != null) canvas.enabled = true;
        if (view != null) { view.fieldOfView = fieldOfView; view.nearClipPlane = nearClip; }
        if (follow != null) { follow.enabled = followEnabled; follow.SnapToTarget(); }
        if (vehicle != null) vehicle.SetChallengeStopped(wasStopped);
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null
            && EventSystem.current.currentSelectedGameObject.transform.IsChildOf(transform))
            EventSystem.current.SetSelectedGameObject(previousSelection != null && previousSelection.activeInHierarchy ? previousSelection : null);
        var callback = completed; completed = null;
        gameObject.SetActive(false);
        if (handoff) callback?.Invoke();
        Destroy(gameObject);
    }
    private void OnDisable() { if (IsPlaying) Finish(false); }

    private RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchorMin = min; rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero; return rect;
    }
    private TMP_Text Text(string name, Transform parent, string value, float size, Vector2 min, Vector2 max)
    {
        var rect = Rect(name, parent, min, max);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value; text.fontSize = size; text.color = Color.white;
        text.enableAutoSizing = true; text.fontSizeMin = size * .7f; text.fontSizeMax = size;
        text.alignment = TextAlignmentOptions.MidlineLeft; text.raycastTarget = false;
        return text;
    }
    private void BuildOverlay()
    {
        var canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 200;
        var scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        // Consume the completed tap here so it cannot also press a driving control.
        var tapArea = Rect("TapAnywhereToSkip", transform, Vector2.zero, Vector2.one);
        var tapImage = tapArea.gameObject.AddComponent<UnityEngine.UI.Image>();
        tapImage.color = Color.clear;
        var tapButton = tapArea.gameObject.AddComponent<UnityEngine.UI.Button>();
        tapButton.targetGraphic = tapImage;
        tapButton.transition = UnityEngine.UI.Selectable.Transition.None;
        tapButton.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
        tapButton.onClick.AddListener(Skip);
        var top = Rect("Top letterbox", transform, new Vector2(0, .90f), Vector2.one);
        var topImage = top.gameObject.AddComponent<UnityEngine.UI.Image>();
        topImage.color = new Color(0, 0, 0, .88f); topImage.raycastTarget = false;
        var bottom = Rect("Bottom letterbox", transform, Vector2.zero, new Vector2(1, BottomLetterboxHeight));
        var bottomImage = bottom.gameObject.AddComponent<UnityEngine.UI.Image>();
        bottomImage.color = new Color(0, 0, 0, .88f); bottomImage.raycastTarget = false;
        safeArea = Rect("IntroSafeArea", transform, Vector2.zero, Vector2.one);
        var hint = Text("TapHint", safeArea, "Tap anywhere to skip", 24, new Vector2(.67f, .91f), new Vector2(.96f, .98f));
        hint.alignment = TextAlignmentOptions.MidlineRight;
        title = Text("IntroTitle", safeArea, "", 48, new Vector2(.045f, .065f), new Vector2(.77f, .132f));
        title.color = new Color(.98f, .77f, .25f);
        subtitle = Text("IntroSubtitle", safeArea, "", 27, new Vector2(.045f, .007f), new Vector2(.80f, .054f));
        if (EventSystem.current != null)
        {
            previousSelection = EventSystem.current.currentSelectedGameObject;
            EventSystem.current.SetSelectedGameObject(tapArea.gameObject);
        }
        FitSafeArea();
    }
    private void FitSafeArea()
    {
        if (safeArea == null || Screen.width <= 0 || Screen.height <= 0) return;
        Rect area = Screen.safeArea;
        safeArea.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
        safeArea.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
        safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
        LayoutLetterboxText(area, new Vector2Int(Screen.width, Screen.height));
    }

    private void LayoutLetterboxText(Rect area, Vector2Int screen)
    {
        if (title == null || subtitle == null || area.height <= 0 || screen.y <= 0) return;
        // Use the part of the bottom bar inside the safe area; inset both labels
        // so the title remains below the bar edge even on devices with a home indicator.
        float available = Mathf.Clamp01((screen.y * BottomLetterboxHeight - area.yMin) / area.height);
        var titleRect = title.rectTransform;
        var subtitleRect = subtitle.rectTransform;
        titleRect.anchorMin = new Vector2(titleRect.anchorMin.x, available * .36f);
        titleRect.anchorMax = new Vector2(titleRect.anchorMax.x, available * .73f);
        subtitleRect.anchorMin = new Vector2(subtitleRect.anchorMin.x, available * .04f);
        subtitleRect.anchorMax = new Vector2(subtitleRect.anchorMax.x, available * .30f);
    }
}
