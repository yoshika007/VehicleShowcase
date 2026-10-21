using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Challenge-only briefing. Help is available between attempts so the lap clock is never paused.
public class CleanLapBriefing : MonoBehaviour
{
    private TestDriveCleanLapChallenge challenge;
    private TestDriveMovement vehicle;
    private GameObject overlay;
    private Button help;
    private TMP_Text startLabel;
    private RectTransform panel, helpRect;
    private bool firstEntry = true;
    private GameObject resultOverlay;
    private RectTransform resultPanel;
    private TMP_Text resultHeading, resultSummary, resultTime, resultReason;

    public void Initialize(TestDriveCleanLapChallenge target, TestDriveMovement car, TestDriveSceneManager manager)
    {
        challenge = target;
        vehicle = car;
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        gameObject.AddComponent<GraphicRaycaster>();
        var font = Object.FindAnyObjectByType<TestDriveCleanLapView>().GetComponentInChildren<TMP_Text>(true).font;
        help = MakeButton("ChallengeHelp", transform, "?", new Vector2(72, 64), font, Open);
        helpRect = (RectTransform)help.transform;
        overlay = Rect("ChallengeBriefing", transform, Vector2.zero).gameObject;
        var cover = (RectTransform)overlay.transform;
        cover.anchorMin = Vector2.zero; cover.anchorMax = Vector2.one;
        cover.offsetMin = cover.offsetMax = Vector2.zero;
        overlay.AddComponent<Image>().color = new Color(0, 0, 0, .8f);
        panel = Rect("BriefingPanel", cover, new Vector2(920, 780));
        panel.gameObject.AddComponent<Image>().color = new Color(.045f, .12f, .14f, 1);
        Text("Heading", panel, challenge.IsSprint ? "CHECKPOINT SPRINT" : "CLEAN LAP CHALLENGE", 48, 28, 72, font);
        Text("Objective", panel, challenge.IsSprint ? "Beat the clock through all four gates." : "Finish one lap without hitting a barrier.", 34, 110, 60, font);
        Text("Rules", panel,
            challenge.IsSprint ? $"Cross START / FINISH after the countdown.\nPass the four gates in order.\nStart with {challenge.SprintStartSeconds:0}s; gain {challenge.SprintBonusSeconds:0}s at gates 1–3.\nTime running out or resetting ends the attempt." : "After the countdown, cross START / FINISH.\nFollow the circuit in the correct direction.\nCurbs and runoff are OK.\nBarrier contact or resetting ends the attempt.", 36, 180, 320, font);
        Text("Medals", panel, challenge.IsSprint ? "The next gate is gold.\nFinish before the clock reaches zero." : $"GOLD  ≤ {challenge.GoldSeconds:0}s     SILVER  ≤ {challenge.SilverSeconds:0}s\nBRONZE  ≤ {challenge.BronzeSeconds:0}s", 34, 500, 100, font);
        var start = MakeButton("StartChallenge", panel, "START CHALLENGE", new Vector2(460, 72), font, Continue);
        ((RectTransform)start.transform).anchoredPosition = new Vector2(0, -270);
        startLabel = start.GetComponentInChildren<TMP_Text>();
        var back = MakeButton("BriefingBack", panel, "BACK TO SHOWROOM", new Vector2(460, 56), font, manager.ReturnToShowroom);
        ((RectTransform)back.transform).anchoredPosition = new Vector2(0, -346);
        BuildResult(font, manager);
        challenge.AttemptEnded += ShowResult;
        Open();
    }

    private void BuildResult(TMP_FontAsset font, TestDriveSceneManager manager)
    {
        resultOverlay = Rect("ChallengeResult", transform, Vector2.zero).gameObject;
        var cover = (RectTransform)resultOverlay.transform;
        cover.anchorMin = Vector2.zero; cover.anchorMax = Vector2.one;
        cover.offsetMin = cover.offsetMax = Vector2.zero;
        cover.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .76f);
        resultPanel = Rect("ResultPanel", cover, new Vector2(760, 560));
        resultPanel.gameObject.AddComponent<Image>().color = new Color(.045f, .12f, .14f, 1);
        Text("ResultHeading", resultPanel, "LAP COMPLETE!", 52, 24, 76, font);
        Text("ResultSummary", resultPanel, "", 28, 112, 44, font);
        Text("ResultTime", resultPanel, "00:00.00", 64, 166, 96, font);
        Text("ResultReason", resultPanel, "", 26, 280, 64, font);
        resultHeading = resultPanel.Find("ResultHeading").GetComponent<TMP_Text>();
        resultSummary = resultPanel.Find("ResultSummary").GetComponent<TMP_Text>();
        resultTime = resultPanel.Find("ResultTime").GetComponent<TMP_Text>();
        resultReason = resultPanel.Find("ResultReason").GetComponent<TMP_Text>();
        foreach (var t in resultPanel.GetComponentsInChildren<TMP_Text>())
            t.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 700);
        var retry = MakeButton("RetryChallenge", resultPanel, "TRY AGAIN", new Vector2(460, 64), font, Retry);
        ((RectTransform)retry.transform).anchoredPosition = new Vector2(0, -126);
        var back = MakeButton("ResultBack", resultPanel, "BACK TO SHOWROOM", new Vector2(460, 56), font, manager.ReturnToShowroom);
        ((RectTransform)back.transform).anchoredPosition = new Vector2(0, -212);
        resultOverlay.SetActive(false);
    }
    private void ShowResult(TestDriveCleanLapChallenge finished)
    {
        overlay.SetActive(false);
        challenge.SetBriefingOpen(false);
        bool success = finished.State == TestDriveCleanLapChallenge.Phase.Finished;
        resultHeading.text = success ? (finished.IsSprint ? "SPRINT COMPLETE!" : "LAP COMPLETE!") : "CHALLENGE FAILED";
        resultHeading.color = success ? new Color(.98f, .77f, .25f) : new Color(1f, .35f, .4f);
        resultSummary.text = success ? finished.IsSprint ? "4 / 4 CHECKPOINTS" : finished.LastMedal == TestDriveCleanLapChallenge.Medal.None ? "NO MEDAL" : finished.LastMedal.ToString().ToUpperInvariant() + " MEDAL" : "ATTEMPT ENDED";
        resultTime.text = TestDriveCleanLapView.FormatTime(finished.ElapsedSeconds);
        resultTime.fontStyle = FontStyles.Bold;
        resultReason.text = success ? finished.Message.Contains("New best") ? "New personal best!" : finished.IsSprint ? "All checkpoints reached." : "Clean lap complete." : finished.Message;
        resultOverlay.SetActive(true);
        Fit();
    }
    private void Retry()
    {
        if (!resultOverlay.activeSelf || !challenge.IsAvailable) return;
        resultOverlay.SetActive(false);
        challenge.StartAttempt();
    }
    private void OnDestroy()
    {
        if (challenge != null) challenge.AttemptEnded -= ShowResult;
    }

    private void Open()
    {
        if (challenge == null || !challenge.IsAvailable || !CanShowHelp() || resultOverlay.activeSelf) return;
        overlay.SetActive(true);
        vehicle.SetChallengeInputLocked(true);
        startLabel.text = firstEntry ? "START CHALLENGE" : "CLOSE GUIDE";
        challenge.SetBriefingOpen(true);
        Fit();
    }
    private void Continue()
    {
        if (!overlay.activeSelf) return;
        overlay.SetActive(false);
        challenge.SetBriefingOpen(false);
        // Ready remains physically held; START CHALLENGE releases it for the countdown.
        if (firstEntry) { firstEntry = false; challenge.StartAttempt(); }
    }
    private bool CanShowHelp() => challenge.State == TestDriveCleanLapChallenge.Phase.Ready;
    private void LateUpdate()
    {
        if (challenge == null) return;
        help.interactable = CanShowHelp() && !overlay.activeSelf;
        Fit();
    }
    private void Fit()
    {
        if (panel == null || Screen.width <= 0 || Screen.height <= 0) return;
        var root = (RectTransform)transform;
        var safe = Screen.safeArea;
        Vector2 min = root.rect.min + Vector2.Scale(root.rect.size, new Vector2(Mathf.Clamp01(safe.xMin / Screen.width), Mathf.Clamp01(safe.yMin / Screen.height)));
        Vector2 max = root.rect.min + Vector2.Scale(root.rect.size, new Vector2(Mathf.Clamp01(safe.xMax / Screen.width), Mathf.Clamp01(safe.yMax / Screen.height)));
        panel.localScale = Vector3.one * Mathf.Min(1, (max.x-min.x-40)/920, (max.y-min.y-40)/780);
        panel.anchoredPosition = (min+max)*.5f;
        if (resultPanel != null)
        {
            resultPanel.localScale = Vector3.one * Mathf.Min(1, (max.x-min.x-40)/760, (max.y-min.y-40)/560);
            resultPanel.anchoredPosition = (min+max)*.5f;
        }
        helpRect.anchoredPosition = new Vector2((min.x+max.x)*.5f, max.y-48);
    }
    private static RectTransform Rect(string name, Transform parent, Vector2 size)
    {
        var r = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        r.SetParent(parent, false); r.sizeDelta = size; return r;
    }
    private static void Text(string name, Transform parent, string value, float size, float top, float height, TMP_FontAsset font)
    {
        var r = Rect(name, parent, new Vector2(860, height));
        r.anchorMin = r.anchorMax = new Vector2(.5f, 1); r.pivot = new Vector2(.5f, 1); r.anchoredPosition = new Vector2(0,-top);
        var t = r.gameObject.AddComponent<TextMeshProUGUI>(); t.font = font; t.text = value; t.fontSize = size;
        t.color = Color.white; t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false;
    }
    private static Button MakeButton(string name, Transform parent, string label, Vector2 size, TMP_FontAsset font, UnityEngine.Events.UnityAction callback)
    {
        var r = Rect(name, parent, size);
        var image = r.gameObject.AddComponent<Image>(); image.color = new Color(.10f,.34f,.38f);
        var b = r.gameObject.AddComponent<Button>(); b.targetGraphic = image;
        var colors = b.colors; colors.highlightedColor = colors.selectedColor = Color.white; colors.pressedColor = new Color(.7f,.7f,.7f); b.colors = colors;
        b.onClick.AddListener(callback);
        Text("Label", r, label, 28, 4, size.y-8, font);
        var t = r.GetComponentInChildren<TMP_Text>(); t.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x-16);
        return b;
    }
}
