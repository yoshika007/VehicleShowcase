using TMPro;
using UnityEngine;

public class TestDriveCleanLapView : MonoBehaviour
{
    [SerializeField] private TMP_Text heading;
    [SerializeField] private TMP_Text clock;
    [SerializeField] private TMP_Text status;
    [SerializeField] private TMP_Text targets;
    [SerializeField] private TMP_Text actionLabel;
    [SerializeField] private UnityEngine.UI.Button action;
    private static readonly Color Gold = new Color(.98f, .77f, .25f);
    public static string FormatTime(float seconds)
    {
        int hundredths = Mathf.Max(0, Mathf.FloorToInt(seconds * 100f));
        return $"{hundredths / 6000:00}:{hundredths / 100 % 60:00}.{hundredths % 100:00}";
    }
    public void Refresh(TestDriveCleanLapChallenge challenge)
    {
        var phase = challenge.State;
        heading.text = phase == TestDriveCleanLapChallenge.Phase.Finished
            ? challenge.LastMedal.ToString().ToUpperInvariant() + " MEDAL" : "CLEAN LAP";
        heading.color = phase == TestDriveCleanLapChallenge.Phase.Finished
            ? challenge.LastMedal == TestDriveCleanLapChallenge.Medal.Gold ? Gold
            : challenge.LastMedal == TestDriveCleanLapChallenge.Medal.Silver ? new Color(.8f, .86f, .92f)
            : new Color(.88f, .58f, .3f) : Gold;
        bool showBest = phase == TestDriveCleanLapChallenge.Phase.Ready && challenge.BestSeconds > 0;
        clock.fontSize = showBest ? 26 : 32;
        clock.text = phase == TestDriveCleanLapChallenge.Phase.Countdown ? challenge.CountdownNumber.ToString()
            : showBest ? "BEST " + FormatTime(challenge.BestSeconds) : FormatTime(challenge.ElapsedSeconds);
        status.text = phase == TestDriveCleanLapChallenge.Phase.Running
            ? $"CHECKPOINTS {challenge.CheckpointsPassed} / 8" : challenge.Message;
        status.color = phase == TestDriveCleanLapChallenge.Phase.Failed ? new Color(1f, .66f, .58f) : Color.white;
        targets.text = $"GOLD {challenge.GoldSeconds:0}s / SILVER {challenge.SilverSeconds:0}s\nBRONZE: FINISH CLEAN";
        actionLabel.text = phase == TestDriveCleanLapChallenge.Phase.Countdown || phase == TestDriveCleanLapChallenge.Phase.Armed
            ? "CANCEL" : phase == TestDriveCleanLapChallenge.Phase.Running ? "RESTART LAP"
            : phase == TestDriveCleanLapChallenge.Phase.Ready ? "START LAP" : "TRY AGAIN";
        action.interactable = challenge.IsAvailable;
    }
}
