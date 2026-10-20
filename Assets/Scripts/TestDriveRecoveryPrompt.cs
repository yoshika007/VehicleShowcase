using UnityEngine;
using TMPro;

// A non-blocking HUD hint; it never pauses the car or changes steering/pedal input.
public class TestDriveRecoveryPrompt : MonoBehaviour
{
    [SerializeField] private CanvasGroup panel;
    [SerializeField] private TMP_Text message;
    [SerializeField] private UnityEngine.UI.Image resetFace;
    [SerializeField] private TMP_Text resetLabel;
    [SerializeField] private UnityEngine.UI.Outline resetOutline;
    private TestDriveMovement vehicle;
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
    }
    public void SetTouchMode(bool visible) => touchMode = visible;
    private void Update()
    {
        Initialize();
        bool show = vehicle != null && vehicle.isActiveAndEnabled && vehicle.IsBoggedDown;
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
        if (message != null)
            message.text = touchMode ? "Try reversing to escape.\nOr tap RECOVER for dry ground."
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
        highlighted = false;
        if (panel != null) panel.alpha = 0f;
        if (!initialized) return;
        if (resetFace != null) { resetFace.color = faceColor; resetRect.sizeDelta = resetSize; }
        if (resetLabel != null) { resetLabel.text = resetText; resetLabel.color = labelColor; resetLabel.fontSize = labelSize; }
        if (resetOutline != null) resetOutline.effectColor = outlineColor;
    }
    private void OnDisable() => Restore();
}
