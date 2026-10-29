using TMPro;
using UnityEngine;
using UnityEngine.UI;

// First-pass Lake Town challenges. Positions follow the authored lakeside road.
public class LakeTownChallenge : MonoBehaviour
{
    [Header("Checkpoint tuning")]
    [SerializeField] private Vector3[] checkpoints = {
        new Vector3(-160, 0, 65), new Vector3(20, 0, 115),
        new Vector3(180, 0, 95), new Vector3(250, 0, -40),
        new Vector3(25, 0, -135), new Vector3(-255, 0, -100),
        new Vector3(-290, 0, -35), new Vector3(-250, 0, 70)
    };
    [SerializeField, Min(1)] private float checkpointRadius = 9f;
    [Header("Parking tuning")]
    [SerializeField] private Vector3 parkingCenter = new Vector3(-160, 0, 65);
    [SerializeField] private Vector2 parkingSize = new Vector2(8, 14);
    [SerializeField] private float parkingYaw = 74f;
    [SerializeField, Min(0)] private float parkingSpeed = 0.7f;
    [SerializeField, Min(0)] private float parkingHoldSeconds = 2f;
    [SerializeField, Range(0, 90)] private float parkingAngleTolerance = 35f;

    private TestDriveMovement vehicle;
    private Rigidbody body;
    private TestDriveSceneManager manager;
    private GameObject canvasObject, overlay, marker;
    private TMP_Text heading, detail, hud;
    private Button start, retry, back;
    private bool running, parking;
    private int next;
    private float elapsed, held;
    private int resetCount;
    private Material markerMaterial;

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
        heading.text = parking ? "PARKING CHALLENGE" : TestDriveSelection.IsNight ? "NIGHT CHECKPOINTS" : "DAY CHECKPOINTS";
        detail.text = parking ? $"Drive to the gold bay. Stop inside it, aligned with the road, and hold for {parkingHoldSeconds:0.#} seconds.\nResetting ends the attempt."
            : "Follow the gold markers in order around Lake Town.\nResetting ends the attempt. No time limit.";
        start.gameObject.SetActive(true); retry.gameObject.SetActive(false);
        back.gameObject.SetActive(true); hud.text = "";
    }
    private void Begin()
    {
        overlay.SetActive(false); next = 0; elapsed = held = 0;
        vehicle.SetChallengeStopped(false); vehicle.ResetVehicle(); resetCount = vehicle.ResetCount;
        vehicle.SetChallengeInputLocked(false); running = true;
        ShowMarker();
    }
    private void Update()
    {
        if (!running || vehicle == null) return;
        elapsed += Time.deltaTime;
        if (vehicle.ResetCount != resetCount) { End(false, "Reset used. Try again."); return; }
        Vector3 pos = body.position;
        if (parking)
        {
            Vector3 local = Quaternion.Euler(0, -parkingYaw, 0) * (pos - parkingCenter);
            float angle = Vector3.Angle(Vector3.ProjectOnPlane(vehicle.transform.forward, Vector3.up), Quaternion.Euler(0, parkingYaw, 0) * Vector3.forward);
            bool inside = Mathf.Abs(local.x) <= parkingSize.x * .5f && Mathf.Abs(local.z) <= parkingSize.y * .5f;
            bool valid = inside && Mathf.Min(angle, 180f - angle) <= parkingAngleTolerance && body.linearVelocity.magnitude <= parkingSpeed;
            held = valid ? held + Time.deltaTime : 0;
            hud.text = valid ? $"HOLD POSITION  {held:0.0} / {parkingHoldSeconds:0.0}s" : "PARK IN THE GOLD BAY";
            if (held >= parkingHoldSeconds && valid) End(true, "Parking complete!");
        }
        else
        {
            Vector3 delta = pos - checkpoints[next]; delta.y = 0;
            hud.text = $"CHECKPOINT {next + 1} / {checkpoints.Length}    {elapsed:0.0}s";
            if (delta.sqrMagnitude <= checkpointRadius * checkpointRadius)
            {
                next++;
                if (next >= checkpoints.Length) End(true, "All checkpoints reached!");
                else ShowMarker();
            }
        }
    }
    private void ShowMarker()
    {
        if (marker != null) Destroy(marker);
        marker = new GameObject("Lake Town challenge marker");
        marker.transform.position = parking ? parkingCenter : checkpoints[next];
        marker.transform.rotation = parking ? Quaternion.Euler(0, parkingYaw, 0) : Quaternion.identity;
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (markerMaterial == null) { markerMaterial = new Material(shader); markerMaterial.color = new Color(1f, .7f, .06f); }
        if (parking)
        {
            Line(new Vector3(-parkingSize.x/2, .12f, 0), new Vector3(.18f, .1f, parkingSize.y));
            Line(new Vector3(parkingSize.x/2, .12f, 0), new Vector3(.18f, .1f, parkingSize.y));
            Line(new Vector3(0, .12f, -parkingSize.y/2), new Vector3(parkingSize.x, .1f, .18f));
            Line(new Vector3(0, .12f, parkingSize.y/2), new Vector3(parkingSize.x, .1f, .18f));
        }
        else
        {
            Line(new Vector3(-checkpointRadius * .75f, 2, 0), new Vector3(.35f, 4, .35f));
            Line(new Vector3(checkpointRadius * .75f, 2, 0), new Vector3(.35f, 4, .35f));
            Line(new Vector3(0, 4, 0), new Vector3(checkpointRadius * 1.5f, .35f, .35f));
        }
    }
    private void Line(Vector3 position, Vector3 scale)
    {
        var part = GameObject.CreatePrimitive(PrimitiveType.Cube); part.transform.SetParent(marker.transform, false);
        part.transform.localPosition = position; part.transform.localScale = scale;
        Destroy(part.GetComponent<Collider>()); part.GetComponent<Renderer>().material = markerMaterial;
    }
    private void End(bool success, string message)
    {
        running = false; if (marker != null) Destroy(marker);
        vehicle.SetChallengeStopped(true); vehicle.SetChallengeInputLocked(true);
        overlay.SetActive(true); heading.text = success ? "CHALLENGE COMPLETE" : "CHALLENGE ENDED";
        detail.text = message + $"\nTime: {elapsed:0.0} seconds";
        start.gameObject.SetActive(false); retry.gameObject.SetActive(true); back.gameObject.SetActive(true); hud.text = "";
    }
    public void StopChallenge()
    {
        running = false; if (marker != null) Destroy(marker);
        if (canvasObject != null) canvasObject.SetActive(false);
    }
    private void OnDestroy() { if (markerMaterial != null) Destroy(markerMaterial); }
    private void BuildUI()
    {
        canvasObject = new GameObject("Lake Challenge UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
        var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        hud = Text("Status", canvasObject.transform, 34, new Vector2(900, 65));
        hud.rectTransform.anchorMin = hud.rectTransform.anchorMax = new Vector2(.5f, 1); hud.rectTransform.anchoredPosition = new Vector2(0, -70);
        overlay = new GameObject("Briefing and result", typeof(RectTransform), typeof(Image)); overlay.transform.SetParent(canvasObject.transform, false);
        var cover = (RectTransform)overlay.transform; cover.anchorMin = Vector2.zero; cover.anchorMax = Vector2.one; cover.offsetMin = cover.offsetMax = Vector2.zero; overlay.GetComponent<Image>().color = new Color(0, 0, 0, .8f);
        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image)); panel.transform.SetParent(overlay.transform, false);
        var pr = (RectTransform)panel.transform; pr.sizeDelta = new Vector2(900, 650); panel.GetComponent<Image>().color = new Color(.045f, .12f, .14f, 1);
        heading = Text("Heading", panel.transform, 46, new Vector2(820, 100)); heading.rectTransform.anchoredPosition = new Vector2(0, 235);
        detail = Text("Details", panel.transform, 32, new Vector2(800, 220)); detail.rectTransform.anchoredPosition = new Vector2(0, 75);
        start = MakeButton("START CHALLENGE", panel.transform, new Vector2(0, -100), Begin);
        retry = MakeButton("TRY AGAIN", panel.transform, new Vector2(0, -100), Begin);
        back = MakeButton("BACK TO SHOWROOM", panel.transform, new Vector2(0, -200), manager.ReturnToShowroom);
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
        go.GetComponent<Image>().color = new Color(.08f, .42f, .42f); var button = go.GetComponent<Button>(); button.onClick.AddListener(action);
        var text = Text("Label", go.transform, 30, new Vector2(460, 65)); text.text = label; return button;
    }
}
