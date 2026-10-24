using UnityEngine;
using TMPro;

// Mud remains a hint; confirmed lake entry stays locked until recovery.
public class TestDriveRecoveryPrompt : MonoBehaviour
{
    [SerializeField] private CanvasGroup panel;
    [SerializeField] private TMP_Text message;
    [SerializeField] private UnityEngine.UI.Image resetFace;
    [SerializeField] private TMP_Text resetLabel;
    [SerializeField] private UnityEngine.UI.Outline resetOutline;
    [SerializeField, Min(0f)] private float lakeWarningDelay = .5f;
    private float lakeStruggleTime;
    private bool lakeRecoveryRequired;
    private int lastResetCount;
    private TestDriveMovement vehicle;
    private LakeTownWaterHazard lake;
    private TMP_Text heading, advice;
    private string originalHeading, originalAdvice;
    private RectTransform resetRect;
    private Color faceColor, labelColor, outlineColor;
    private Vector2 resetSize;
    private string resetText;
    private float labelSize;
    private bool initialized, highlighted, touchMode;

    private void Initialize()
    {
        if (initialized) return;
        initialized = true;
        heading = transform.Find("Heading")?.GetComponent<TMP_Text>();
        advice = transform.Find("VehicleAdvice")?.GetComponent<TMP_Text>();
        if (heading != null) originalHeading = heading.text;
        if (advice != null) originalAdvice = advice.text;
        if (resetFace != null)
        {
            resetRect = resetFace.rectTransform;
            resetSize = resetRect.sizeDelta;
            faceColor = resetFace.color;
        }
        if (resetLabel != null)
        {
            resetText = resetLabel.text;
            labelColor = resetLabel.color;
            labelSize = resetLabel.fontSize;
        }
        if (resetOutline != null) outlineColor = resetOutline.effectColor;
    }
    public void BindVehicle(TestDriveMovement target)
    {
        Initialize();
        Restore();
        vehicle = target;
        lakeStruggleTime = 0f;
        lakeRecoveryRequired = false;
        lastResetCount = target != null ? target.ResetCount : 0;
        lake = FindAnyObjectByType<LakeTownWaterHazard>();
        if (lake != null) lake.BindVehicle(target);
    }
    public void SetTouchMode(bool visible) => touchMode = visible;
    private void Update()
    {
        Initialize();
        bool validVehicle = vehicle != null && vehicle.isActiveAndEnabled;
        if (!validVehicle || vehicle.ResetCount != lastResetCount)
        {
            lakeStruggleTime = 0f;
            lakeRecoveryRequired = false;
            lastResetCount = vehicle != null ? vehicle.ResetCount : 0;
        }
        var submersion = validVehicle && lake != null
            ? lake.GetSubmersion() : LakeTownWaterHazard.Submersion.Dry;
        bool inLake = submersion != LakeTownWaterHazard.Submersion.Dry;
        lakeStruggleTime = inLake ? lakeStruggleTime + Time.deltaTime : 0f;
        if (submersion == LakeTownWaterHazard.Submersion.Deep ||
            (inLake && lakeStruggleTime >= lakeWarningDelay)) lakeRecoveryRequired = true;
        bool lakeWarning = lakeRecoveryRequired;
        bool lakeMessage = inLake || lakeWarning;
        if (vehicle != null) vehicle.SetRecoveryInputLocked(lakeWarning);
        bool show = vehicle != null && vehicle.isActiveAndEnabled && (vehicle.IsBoggedDown || lakeWarning);
        if (panel != null)
        {
            panel.alpha = show ? 1f : 0f;
            panel.interactable = panel.blocksRaycasts = false;
        }
        if (!show)
        {
            if (highlighted) Restore();
            return;
        }
        highlighted = true;
        if (heading != null) heading.text = lakeMessage ? "NOT A SUBMARINE!" : originalHeading;
        if (advice != null) advice.text = lakeMessage ? "The fish are judging you." : originalAdvice;
        if (message != null)
            message.text = lakeMessage
                ? (touchMode ? "Your car can't swim!\nTap RECOVER to return safely." : "Your car can't swim!\nPress R to return safely.")
                : touchMode ? "Try reversing to escape.\nOr tap RECOVER for dry ground."
                : "Try reversing to escape.\nOr press R for dry ground.";
        float pulse = .92f + .08f * Mathf.Sin(Time.unscaledTime * 4f);
        if (resetFace != null)
        {
            resetFace.color = new Color(.98f * pulse, .77f * pulse, .25f * pulse, 1f);
            resetRect.sizeDelta = new Vector2(240, 128);
        }
        if (resetLabel != null)
        {
            resetLabel.text = "RECOVER";
            resetLabel.color = new Color(.04f, .05f, .06f);
            resetLabel.fontSize = 32;
        }
        if (resetOutline != null) resetOutline.effectColor = new Color(1f, .88f, .5f, .9f);
    }
    private void Restore()
    {
        if (vehicle != null) vehicle.SetRecoveryInputLocked(false);
        highlighted = false;
        if (heading != null) heading.text = originalHeading;
        if (advice != null) advice.text = originalAdvice;
        if (panel != null) panel.alpha = 0f;
        if (!initialized) return;
        if (resetFace != null) { resetFace.color = faceColor; resetRect.sizeDelta = resetSize; }
        if (resetLabel != null) { resetLabel.text = resetText; resetLabel.color = labelColor; resetLabel.fontSize = labelSize; }
        if (resetOutline != null) resetOutline.effectColor = outlineColor;
    }
    private void OnDisable()
    {
        lakeStruggleTime = 0f;
        lakeRecoveryRequired = false;
        Restore();
    }
}
