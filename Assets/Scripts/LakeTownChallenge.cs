using TMPro;
using UnityEngine;
using UnityEngine.UI;

// First-pass Lake Town challenges. Positions follow the authored lakeside road.
public class LakeTownChallenge : MonoBehaviour
{
    [System.Serializable]
    private class Gate
    {
        public Vector3 position;
        [Tooltip("Direction of travel through the gate, in degrees around Y.")]
        public float yaw;
        [Min(1)] public float openingWidth = 16f;
        [Min(2)] public float openingHeight = 6f;
        public Gate(Vector3 position, float yaw) { this.position = position; this.yaw = yaw; }
    }
    [Header("Checkpoint gates")]
    [SerializeField] private Gate[] gates = {
        new Gate(new Vector3(-160, 0, 65), 83.7f), new Gate(new Vector3(20, 0, 115), 81.9f),
        new Gate(new Vector3(180, 0, 95), 80.2f), new Gate(new Vector3(250, 0, -40), -137.5f),
        new Gate(new Vector3(25, 0, -135), -110.6f), new Gate(new Vector3(-255, 0, -100), -65.4f),
        new Gate(new Vector3(-290, 0, -35), 1.7f), new Gate(new Vector3(-250, 0, 70), 52.4f)
    };
    [SerializeField, Min(0)] private float gateGroundTolerance = 2f;
    [SerializeField] private AudioSource gateAudio;
    [SerializeField] private AudioClip gatePassSound;
    [System.Serializable]
    private class ParkingSpot
    {
        public Vector3 position;
        [Tooltip("Direction of the parking bay along the road, in degrees around Y.")]
        public float yaw;
        public ParkingSpot(Vector3 position, float yaw) { this.position = position; this.yaw = yaw; }
    }
    [Header("Parking tuning")]
    [SerializeField] private ParkingSpot[] parkingSpots = {
        new ParkingSpot(new Vector3(-160, 0, 65), 83.7f),
        new ParkingSpot(new Vector3(20, 0, 115), 81.9f),
        new ParkingSpot(new Vector3(250, 0, -40), -137.5f),
        new ParkingSpot(new Vector3(25, 0, -135), -110.6f),
        new ParkingSpot(new Vector3(-255, 0, -100), -65.4f),
        new ParkingSpot(new Vector3(-90, 0, 40), 0f),
        new ParkingSpot(new Vector3(85, 0, 45), 0f),
        new ParkingSpot(new Vector3(0, 0, -5), 90f)
    };
    [Tooltip("Fallback bay used when no parking spots are configured.")]
    [SerializeField] private Vector3 parkingCenter = new Vector3(-160, 0, 65);
    [SerializeField] private Vector2 parkingSize = new Vector2(4.5f, 8f);
    [SerializeField] private float parkingYaw = 74f;
    [SerializeField, Min(0)] private float parkingSpeed = 0.7f;
    [SerializeField, Min(0)] private float parkingHoldSeconds = 2f;
    [SerializeField, Range(0, 90)] private float parkingAngleTolerance = 35f;
    [Header("Parking beacon")]
    [SerializeField] private Color beaconColor = new Color(1f, .74f, .12f);
    [SerializeField, Min(1f)] private float beaconHeight = 30f;
    [SerializeField, Min(3f)] private float beaconBaseHeight = 8f;
    [SerializeField, Min(.1f)] private float beaconWidth = 1.8f;
    [SerializeField, Range(.1f, 1f)] private float beaconIntensity = 1f;
    [SerializeField, Min(0f)] private float beaconPulseSpeed = 1.2f;

    private TestDriveMovement vehicle;
    private Rigidbody body;
    private TestDriveSceneManager manager;
    private GameObject canvasObject, overlay, marker;
    private TMP_Text heading, detail, hud;
    private RectTransform challengePanel;
    private Button start, retry, back;
    private bool running, parking;
    private int next;
    private float elapsed, held;
    private int resetCount;
    private Material markerMaterial, activeGateMaterial, inactiveGateMaterial, signMaterial, beaconMaterial;
    private Renderer[] beaconRenderers;
    private MaterialPropertyBlock beaconProperties;
    private GameObject gateRoot;
    private Renderer[][] gateRenderers;
    private TMP_Text[][] gateLabels;
    private Vector3 previousPosition;
    private Vector3 selectedParkingCenter;
    private float selectedParkingYaw;
    private static int previousParkingIndex = -1;

    public void Initialize(TestDriveMovement car, TestDriveSceneManager sceneManager)
    {
        vehicle = car; body = car.GetComponent<Rigidbody>(); manager = sceneManager;
        parking = TestDriveSelection.SelectedChallenge == TestDriveSelection.ChallengeKind.LakeParking;
        vehicle.SetChallengeStopped(true);
        vehicle.SetChallengeInputLocked(true);
        BuildUI();
        canvasObject.SetActive(false);
    }
    public void ShowBriefing()
    {
        canvasObject.SetActive(true); overlay.SetActive(true);
        heading.color = new Color(.98f, .77f, .25f);
        heading.text = parking ? "PARKING CHALLENGE" : TestDriveSelection.IsNight ? "NIGHT CHECKPOINTS" : "DAY CHECKPOINTS";
        detail.text = parking ? $"Explore Lake Town and find the gold parking bay.\nStop inside, aligned with the road, for {parkingHoldSeconds:0.#} seconds. Resetting ends the attempt."
            : "Drive through the numbered gates in order, following the gold gate.\nCross each gate in the direction of travel. Resetting ends the attempt.";
        start.gameObject.SetActive(true); retry.gameObject.SetActive(false);
        back.gameObject.SetActive(true); hud.text = "";
    }
    private void Begin()
    {
        overlay.SetActive(false); next = 0; elapsed = held = 0;
        vehicle.SetChallengeStopped(false); vehicle.ResetVehicle(); resetCount = vehicle.ResetCount;
        vehicle.SetChallengeInputLocked(false); running = true;
        previousPosition = body.position;
        if (parking) { SelectParkingSpot(); ShowMarker(); } else BuildGates();
    }
    private void Update()
    {
        if (!running || vehicle == null) return;
        elapsed += Time.deltaTime;
        if (vehicle.ResetCount != resetCount) { End(false, "Reset used. Try again."); return; }
        Vector3 pos = body.position;
        if (parking)
        {
            UpdateBeacon();
            Vector3 local = Quaternion.Euler(0, -selectedParkingYaw, 0) * (pos - selectedParkingCenter);
            float angle = Vector3.Angle(Vector3.ProjectOnPlane(vehicle.transform.forward, Vector3.up), Quaternion.Euler(0, selectedParkingYaw, 0) * Vector3.forward);
            bool inside = Mathf.Abs(local.x) <= parkingSize.x * .5f && Mathf.Abs(local.z) <= parkingSize.y * .5f;
            bool valid = inside && Mathf.Min(angle, 180f - angle) <= parkingAngleTolerance && body.linearVelocity.magnitude <= parkingSpeed;
            held = valid ? held + Time.deltaTime : 0;
            hud.text = valid ? $"HOLD POSITION  {held:0.0} / {parkingHoldSeconds:0.0}s" : "FIND AND PARK IN THE GOLD BAY";
            if (held >= parkingHoldSeconds && valid) End(true, "Parking complete!");
        }
        else hud.text = $"GATES PASSED {next} / {gates.Length}    •    NEXT {next + 1}    {elapsed:0.0}s";
    }
    private void FixedUpdate()
    {
        if (!running || parking || vehicle == null || body == null) return;
        if (vehicle.ResetCount != resetCount) { End(false, "Reset used. Try again."); return; }
        Vector3 position = body.position;
        if (next < gates.Length && CrossedGate(gates[next], previousPosition, position, gateGroundTolerance))
        {
            if (gateAudio != null && gatePassSound != null) gateAudio.PlayOneShot(gatePassSound);
            next++;
            if (next == gates.Length) End(true, "All gates passed!");
            else RefreshGates();
        }
        previousPosition = position;
    }
    private static bool CrossedGate(Gate gate, Vector3 previous, Vector3 current, float groundTolerance)
    {
        Quaternion inverse = Quaternion.Inverse(Quaternion.Euler(0, gate.yaw, 0));
        Vector3 a = inverse * (previous - gate.position), b = inverse * (current - gate.position);
        // A directed, swept segment catches a crossing even when a physics step skips the plane.
        if (a.z >= 0f || b.z < 0f) return false;
        float fraction = -a.z / (b.z - a.z);
        Vector3 crossing = Vector3.LerpUnclamped(a, b, fraction);
        return Mathf.Abs(crossing.x) <= gate.openingWidth * .5f
            && crossing.y >= -groundTolerance && crossing.y <= gate.openingHeight;
    }
    private void BuildGates()
    {
        if (gateRoot != null) { gateRoot.SetActive(false); Destroy(gateRoot); }
        gateRoot = new GameObject("Lake Town numbered gates");
        gateRenderers = new Renderer[gates.Length][]; gateLabels = new TMP_Text[gates.Length][];
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (activeGateMaterial == null)
        {
            activeGateMaterial = new Material(shader); activeGateMaterial.color = new Color(1f, .68f, .04f);
            activeGateMaterial.EnableKeyword("_EMISSION"); activeGateMaterial.SetColor("_EmissionColor", new Color(.8f, .32f, .01f));
            inactiveGateMaterial = new Material(shader); inactiveGateMaterial.color = new Color(.12f, .38f, .42f);
            inactiveGateMaterial.EnableKeyword("_EMISSION"); inactiveGateMaterial.SetColor("_EmissionColor", new Color(.02f, .12f, .14f));
            signMaterial = new Material(shader); signMaterial.color = new Color(.025f, .07f, .08f);
        }
        for (int i = 0; i < gates.Length; i++)
        {
            Gate gate = gates[i];
            var root = new GameObject("Gate " + (i + 1)); root.transform.SetParent(gateRoot.transform, false);
            root.transform.position = gate.position; root.transform.rotation = Quaternion.Euler(0, gate.yaw, 0);
            float half = gate.openingWidth * .5f, height = gate.openingHeight;
            gateRenderers[i] = new[] {
                GatePart(root.transform, new Vector3(-half-.3f, height*.5f, 0), new Vector3(.6f, height, .6f)),
                GatePart(root.transform, new Vector3(half+.3f, height*.5f, 0), new Vector3(.6f, height, .6f)),
                GatePart(root.transform, new Vector3(0, height+.35f, 0), new Vector3(gate.openingWidth+1.2f, .7f, .7f))
            };
            var sign = GatePart(root.transform, new Vector3(0, height + 1.75f, 0), new Vector3(7f, 2f, .7f));
            sign.sharedMaterial = signMaterial;
            gateLabels[i] = new[] { GateLabel(root.transform, i + 1, height, false), GateLabel(root.transform, i + 1, height, true) };
        }
        RefreshGates();
    }
    private Renderer GatePart(Transform parent, Vector3 position, Vector3 scale)
    {
        var part = GameObject.CreatePrimitive(PrimitiveType.Cube); part.transform.SetParent(parent, false);
        part.transform.localPosition = position; part.transform.localScale = scale;
        Destroy(part.GetComponent<Collider>()); return part.GetComponent<Renderer>();
    }
    private TMP_Text GateLabel(Transform parent, int number, float height, bool reverse)
    {
        var go = new GameObject("Gate number " + number, typeof(TextMeshPro)); go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0, height + 1.75f, reverse ? .42f : -.42f);
        go.transform.localRotation = Quaternion.Euler(0, reverse ? 180 : 0, 0);
        var text = go.GetComponent<TextMeshPro>(); text.text = "GATE " + number; text.fontSize = 7f;
        text.alignment = TextAlignmentOptions.Center; text.rectTransform.sizeDelta = new Vector2(6.5f, 1.8f);
        text.color = Color.white; text.fontStyle = FontStyles.Bold; return text;
    }
    private void RefreshGates()
    {
        for (int i = 0; i < gates.Length; i++)
        {
            // Show only the gate being approached, so distant gates cannot visually overlap.
            bool active = i == next, visible = active;
            gateRenderers[i][0].transform.parent.gameObject.SetActive(visible);
            foreach (var renderer in gateRenderers[i]) { renderer.gameObject.SetActive(visible); renderer.sharedMaterial = active ? activeGateMaterial : inactiveGateMaterial; }
            foreach (var label in gateLabels[i]) { label.gameObject.SetActive(visible); label.color = active ? Color.white : new Color(.65f, .85f, .85f); }
        }
    }
    private void SelectParkingSpot()
    {
        int count = parkingSpots != null ? parkingSpots.Length : 0;
        if (count == 0)
        {
            selectedParkingCenter = parkingCenter; selectedParkingYaw = parkingYaw;
            previousParkingIndex = -1;
            return;
        }
        int index;
        if (count > 1 && previousParkingIndex >= 0 && previousParkingIndex < count)
        {
            index = Random.Range(0, count - 1);
            if (index >= previousParkingIndex) index++;
        }
        else index = Random.Range(0, count);
        previousParkingIndex = index;
        selectedParkingCenter = parkingSpots[index].position;
        selectedParkingYaw = parkingSpots[index].yaw;
    }
    private void ShowMarker()
    {
        if (marker != null) { marker.SetActive(false); Destroy(marker); }
        marker = new GameObject("Lake Town challenge marker");
        marker.transform.position = selectedParkingCenter;
        marker.transform.rotation = Quaternion.Euler(0, selectedParkingYaw, 0);
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (markerMaterial == null) { markerMaterial = new Material(shader); markerMaterial.color = new Color(1f, .7f, .06f); }
        if (parking)
        {
            Line(new Vector3(-parkingSize.x/2, .12f, 0), new Vector3(.18f, .1f, parkingSize.y));
            Line(new Vector3(parkingSize.x/2, .12f, 0), new Vector3(.18f, .1f, parkingSize.y));
            Line(new Vector3(0, .12f, -parkingSize.y/2), new Vector3(parkingSize.x, .1f, .18f));
            Line(new Vector3(0, .12f, parkingSize.y/2), new Vector3(parkingSize.x, .1f, .18f));
            BuildBeacon();
        }

    }
    private void BuildBeacon()
    {
        if (beaconMaterial == null) beaconMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        var beacon = new GameObject("Parking beacon"); beacon.transform.SetParent(marker.transform, false);
        float middle = beaconBaseHeight + beaconHeight * .5f;
        beaconRenderers = new[] {
            BeaconPart(beacon.transform, new Vector3(0, middle, 0), new Vector3(beaconWidth, beaconHeight, .12f), 0f),
            BeaconPart(beacon.transform, new Vector3(0, middle, 0), new Vector3(beaconWidth, beaconHeight, .12f), 90f),
            BeaconPart(beacon.transform, new Vector3(0, beaconBaseHeight + beaconHeight, 0), new Vector3(beaconWidth * 1.6f, beaconWidth * 1.6f, beaconWidth * 1.6f), 45f)
        };
        UpdateBeacon();
    }
    private Renderer BeaconPart(Transform parent, Vector3 position, Vector3 scale, float yaw)
    {
        var part = GameObject.CreatePrimitive(PrimitiveType.Cube); part.transform.SetParent(parent, false);
        part.transform.localPosition = position; part.transform.localRotation = Quaternion.Euler(0, yaw, 0); part.transform.localScale = scale;
        Destroy(part.GetComponent<Collider>()); var renderer = part.GetComponent<Renderer>(); renderer.sharedMaterial = beaconMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false; return renderer;
    }
    private void UpdateBeacon()
    {
        if (beaconRenderers == null) return;
        if (beaconProperties == null) beaconProperties = new MaterialPropertyBlock();
        float pulse = .8f + .2f * Mathf.Sin(Time.time * beaconPulseSpeed);
        Color color = beaconColor * (beaconIntensity * pulse); color.a = 1f;
        beaconProperties.SetColor("_BaseColor", color);
        foreach (var part in beaconRenderers) if (part != null) part.SetPropertyBlock(beaconProperties);
    }
    private void Line(Vector3 position, Vector3 scale)
    {
        var part = GameObject.CreatePrimitive(PrimitiveType.Cube); part.transform.SetParent(marker.transform, false);
        part.transform.localPosition = position; part.transform.localScale = scale;
        Destroy(part.GetComponent<Collider>()); part.GetComponent<Renderer>().material = markerMaterial;
    }
    private void End(bool success, string message)
    {
        running = false; if (marker != null) { marker.SetActive(false); Destroy(marker); } if (gateRoot != null) { gateRoot.SetActive(false); Destroy(gateRoot); }
        vehicle.SetChallengeStopped(true); vehicle.SetChallengeInputLocked(true);
        overlay.SetActive(true); heading.text = success ? "CHALLENGE COMPLETE" : "CHALLENGE ENDED";
        heading.color = success ? new Color(.98f, .77f, .25f) : new Color(1f, .35f, .4f);
        detail.text = message + $"\nTime: {elapsed:0.0} seconds";
        start.gameObject.SetActive(false); retry.gameObject.SetActive(true); back.gameObject.SetActive(true); hud.text = "";
    }
    public void StopChallenge()
    {
        running = false; if (marker != null) { marker.SetActive(false); Destroy(marker); } if (gateRoot != null) { gateRoot.SetActive(false); Destroy(gateRoot); }
        if (canvasObject != null) canvasObject.SetActive(false);
    }
    private void OnDestroy()
    {
        if (markerMaterial != null) Destroy(markerMaterial);
        if (beaconMaterial != null) Destroy(beaconMaterial);
        if (activeGateMaterial != null) Destroy(activeGateMaterial);
        if (inactiveGateMaterial != null) Destroy(inactiveGateMaterial);
        if (signMaterial != null) Destroy(signMaterial);
        if (gateRoot != null) Destroy(gateRoot);
    }
    private void BuildUI()
    {
        canvasObject = new GameObject("Lake Challenge UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
        var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        hud = Text("Status", canvasObject.transform, 34, new Vector2(900, 65));
        hud.rectTransform.anchorMin = hud.rectTransform.anchorMax = new Vector2(.5f, 1); hud.rectTransform.anchoredPosition = new Vector2(0, -70);
        overlay = new GameObject("Briefing and result", typeof(RectTransform), typeof(Image)); overlay.transform.SetParent(canvasObject.transform, false);
        var cover = (RectTransform)overlay.transform; cover.anchorMin = Vector2.zero; cover.anchorMax = Vector2.one; cover.offsetMin = cover.offsetMax = Vector2.zero; overlay.GetComponent<Image>().color = new Color(0, 0, 0, .76f);
        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image)); panel.transform.SetParent(overlay.transform, false);
        var pr = (RectTransform)panel.transform; challengePanel = pr; pr.sizeDelta = new Vector2(1050, 650); panel.GetComponent<Image>().color = new Color(.045f, .08f, .11f, .99f);
        heading = Text("Heading", panel.transform, 52, new Vector2(970, 100)); heading.rectTransform.anchoredPosition = new Vector2(0, 235);
        detail = Text("Details", panel.transform, 44, new Vector2(970, 250)); detail.color = new Color(.8f, .88f, .9f); detail.rectTransform.anchoredPosition = new Vector2(0, 75);
        start = MakeButton("START CHALLENGE", panel.transform, new Vector2(0, -115), Begin);
        retry = MakeButton("TRY AGAIN", panel.transform, new Vector2(0, -115), Begin);
        back = MakeButton("BACK TO SHOWROOM", panel.transform, new Vector2(0, -205), manager.ReturnToShowroom);
    }
    private void LateUpdate()
    {
        if (challengePanel == null || Screen.width <= 0 || Screen.height <= 0) return;
        var root = (RectTransform)canvasObject.transform;
        var safe = Screen.safeArea;
        Vector2 min = root.rect.min + Vector2.Scale(root.rect.size, new Vector2(Mathf.Clamp01(safe.xMin / Screen.width), Mathf.Clamp01(safe.yMin / Screen.height)));
        Vector2 max = root.rect.min + Vector2.Scale(root.rect.size, new Vector2(Mathf.Clamp01(safe.xMax / Screen.width), Mathf.Clamp01(safe.yMax / Screen.height)));
        challengePanel.localScale = Vector3.one * Mathf.Min(1f, (max.x - min.x - 40f) / 1050f, (max.y - min.y - 40f) / 650f);
        challengePanel.anchoredPosition = (min + max) * .5f;
    }
    private TMP_Text Text(string name, Transform parent, float size, Vector2 dimensions)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
        var text = go.AddComponent<TextMeshProUGUI>(); text.fontSize = size; text.color = Color.white; text.alignment = TextAlignmentOptions.Center; text.rectTransform.sizeDelta = dimensions; return text;
    }
    private Button MakeButton(string label, Transform parent, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform; rect.sizeDelta = new Vector2(480, 70); rect.anchoredPosition = position;
        go.GetComponent<Image>().color = new Color(.08f, .25f, .29f); var button = go.GetComponent<Button>();
        var colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = colors.selectedColor = new Color(.961f, .961f, .961f);
        colors.pressedColor = new Color(.784f, .784f, .784f); colors.disabledColor = new Color(.784f, .784f, .784f, .502f); colors.fadeDuration = .1f; button.colors = colors;
        button.onClick.AddListener(action);
        var text = Text("Label", go.transform, 30, new Vector2(460, 65)); text.text = label; return button;
    }
}
