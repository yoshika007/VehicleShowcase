using TMPro;
using UnityEngine;

public class TestDriveCleanLapView : MonoBehaviour
{
    [SerializeField] private TMP_Text heading;
    [SerializeField] private TMP_Text clock;
    [SerializeField] private TMP_Text status;
    [SerializeField] private TMP_Text actionLabel;
    [SerializeField] private UnityEngine.UI.Button action;
    private TMP_Text centerCountdown;
    private static readonly Color Gold = new Color(.98f, .77f, .25f);
    public static string FormatTime(float seconds)
    {
        int hundredths = Mathf.Max(0, Mathf.FloorToInt(seconds * 100f));
        return $"{hundredths / 6000:00}:{hundredths / 100 % 60:00}.{hundredths % 100:00}";
    }
    public void Refresh(TestDriveCleanLapChallenge challenge)
    {
        var phase = challenge.State;
        bool showBest = phase == TestDriveCleanLapChallenge.Phase.Ready && challenge.BestSeconds > 0;
        heading.text = phase == TestDriveCleanLapChallenge.Phase.Finished
            ? challenge.LastMedal == TestDriveCleanLapChallenge.Medal.None ? "NO MEDAL" : challenge.LastMedal.ToString().ToUpperInvariant() + " MEDAL"
            : showBest ? "CLEAN LAP / BEST" : "CLEAN LAP";
        heading.color = phase == TestDriveCleanLapChallenge.Phase.Finished
            ? challenge.LastMedal == TestDriveCleanLapChallenge.Medal.Gold ? Gold
            : challenge.LastMedal == TestDriveCleanLapChallenge.Medal.Silver ? new Color(.8f, .86f, .92f)
            : new Color(.88f, .58f, .3f) : Gold;
        clock.fontSize = 64;
        clock.fontStyle = FontStyles.Bold;
        clock.color = phase == TestDriveCleanLapChallenge.Phase.Finished ? Gold
            : phase == TestDriveCleanLapChallenge.Phase.Running ? new Color(.64f, 1f, .88f) : Color.white;
        // Fixed cells and left alignment prevent changing glyph bounds from shifting the row.
        string time = FormatTime(showBest ? challenge.BestSeconds : challenge.ElapsedSeconds);
        clock.fontFeatures.Clear();
        clock.alignment = TextAlignmentOptions.Left;
        clock.margin = new Vector4(Mathf.Max(0, (clock.rectTransform.rect.width - 64f * .58f * time.Length) * .5f), 0, 0, 0);
        clock.text = "<mspace=0.58em>" + time + "</mspace>";
        status.text = phase == TestDriveCleanLapChallenge.Phase.Ready ? "Ready to start"
            : phase == TestDriveCleanLapChallenge.Phase.Countdown ? "Get ready"
            : phase == TestDriveCleanLapChallenge.Phase.Armed ? "Cross the start line"
            : phase == TestDriveCleanLapChallenge.Phase.Running ? "Lap in progress"
            : phase == TestDriveCleanLapChallenge.Phase.Finished
                ? challenge.Message.Contains("New best") ? "New best!" : "Lap complete"
            : challenge.Message;
        status.color = phase == TestDriveCleanLapChallenge.Phase.Failed ? new Color(1f, .66f, .58f) : Color.white;
        actionLabel.text = phase == TestDriveCleanLapChallenge.Phase.Countdown || phase == TestDriveCleanLapChallenge.Phase.Armed
            ? "CANCEL" : phase == TestDriveCleanLapChallenge.Phase.Running ? "RESTART LAP"
            : phase == TestDriveCleanLapChallenge.Phase.Ready ? "START LAP" : "TRY AGAIN";
        action.interactable = challenge.IsAvailable
            && phase != TestDriveCleanLapChallenge.Phase.Finished && phase != TestDriveCleanLapChallenge.Phase.Failed;
        RefreshCountdown(challenge);
    }
    private void RefreshCountdown(TestDriveCleanLapChallenge challenge)
    {
        bool counting = challenge.State == TestDriveCleanLapChallenge.Phase.Countdown && challenge.IsAvailable;
        if (!counting)
        {
            if (centerCountdown != null) centerCountdown.gameObject.SetActive(false);
            return;
        }
        if (centerCountdown == null)
        {
            var canvas = GetComponentInParent<Canvas>().rootCanvas;
            var rect = (RectTransform)new GameObject("CleanLapCountdown", typeof(RectTransform)).transform;
            rect.SetParent(canvas.transform, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(420, 280);
            centerCountdown = rect.gameObject.AddComponent<TextMeshProUGUI>();
            centerCountdown.font = clock.font;
            centerCountdown.fontSize = 180;
            centerCountdown.fontStyle = FontStyles.Bold;
            centerCountdown.alignment = TextAlignmentOptions.Center;
            centerCountdown.color = new Color(1f, .23f, .32f, 1f);
            centerCountdown.outlineColor = new Color(.025f, .08f, .10f, 1);
            centerCountdown.outlineWidth = .2f;
            centerCountdown.raycastTarget = false;
        }
        centerCountdown.text = challenge.CountdownNumber.ToString();
        centerCountdown.gameObject.SetActive(true);
        centerCountdown.transform.SetAsLastSibling();
        // Match the visible safe area, including Simulator values outside the current view.
        var parent = (RectTransform)centerCountdown.transform.parent;
        Rect safe = Screen.safeArea;
        if (Screen.width > 0 && Screen.height > 0)
        {
            Vector2 min = new Vector2(Mathf.Clamp01(safe.xMin / Screen.width), Mathf.Clamp01(safe.yMin / Screen.height));
            Vector2 max = new Vector2(Mathf.Clamp01(safe.xMax / Screen.width), Mathf.Clamp01(safe.yMax / Screen.height));
            centerCountdown.rectTransform.anchoredPosition = Vector2.Scale(parent.rect.size, (min + max) * .5f - Vector2.one * .5f);
        }
    }
    private void OnDisable()
    {
        if (centerCountdown != null) centerCountdown.gameObject.SetActive(false);
    }
    private void OnDestroy()
    {
        if (centerCountdown != null) Destroy(centerCountdown.gameObject);
    }
}
