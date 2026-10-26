using UnityEngine;
using TMPro;

// Reflects course and lighting selections before DRIVE starts the drive.
public class FreeRideCourseMenu : MonoBehaviour
{
    [SerializeField] private VehicleShowcaseManager manager;
    [SerializeField] private UnityEngine.UI.Button day;
    [SerializeField] private UnityEngine.UI.Button night;
    [SerializeField] private UnityEngine.UI.Button circuit;
    [SerializeField] private UnityEngine.UI.Button lakeTown;
    [SerializeField] private UnityEngine.UI.Button mountainPass;
    [SerializeField] private UnityEngine.UI.Button harborStreet;
    private void OnEnable()
    {
        ArrangeWithoutSubtitle();
        ArrangeCourses();
        MatchButtonSizes();
        UpdateActionLabels();
        UpdateLakeTownDescription();
        MatchActionButtonColors();
        AddToonButtonIcons();
        Refresh();
    }
    private void ArrangeWithoutSubtitle()
    {
        var subtitle = transform.Find("Dialog/Subtitle");
        if (subtitle == null || subtitle.gameObject.activeSelf) return;
        var dialog = transform.Find("Dialog") as RectTransform;
        if (dialog != null) dialog.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 620f);
        SetRowPosition("Heading", 245f);
        SetRowPosition("TestCircuitCourse", 65f);
        SetRowPosition("LakeTownCourse", 65f);
        SetRowPosition("Day", -125f);
        SetRowPosition("Night", -125f);
        SetRowPosition("Back", -225f);
        SetRowPosition("Go", -225f);
    }
    private void SetRowPosition(string name, float y)
    {
        var rect = transform.Find("Dialog/" + name) as RectTransform;
        if (rect != null) rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
    }
    private void ArrangeCourses()
    {
        if (mountainPass == null) return;
        var dialog = transform.Find("Dialog") as RectTransform;
        if (dialog != null) dialog.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1000f);
        var buttons = harborStreet != null ? new[] { circuit, lakeTown, mountainPass, harborStreet }
            : new[] { circuit, lakeTown, mountainPass };
        bool four = harborStreet != null;
        for (int i = 0; i < buttons.Length; i++)
        {
            var rect = (RectTransform)buttons[i].transform;
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, four ? 220f : 280f);
            rect.anchoredPosition = new Vector2((i - (buttons.Length - 1) * .5f) * (four ? 235f : 310f), 65f);
            foreach (var label in buttons[i].GetComponentsInChildren<TMP_Text>(true))
            {
                label.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, four ? 200f : 260f);
                if (label.name == "Label")
                {
                    label.enableAutoSizing = true;
                    label.fontSizeMin = four ? 20f : 24f;
                    label.fontSizeMax = four ? 28f : 34f;
                    label.textWrappingMode = TextWrappingModes.NoWrap;
                    label.rectTransform.anchoredPosition = new Vector2(0f, 65f);
                }
                else if (label.name == "Details")
                {
                    label.fontSize = four ? 21f : 23f;
                    label.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 120f);
                    label.rectTransform.anchoredPosition = new Vector2(0f, -30f);
                }
            }
        }
    }
    private void MatchButtonSizes()
    {
        if (circuit == null || lakeTown == null) return;
        if (mountainPass != null)
        {
            foreach (string name in new[] { "Day", "Night", "Back", "Go" })
            {
                var rect = transform.Find("Dialog/" + name) as RectTransform;
                if (rect == null) continue;
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 440f);
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 90f);
                rect.anchoredPosition = new Vector2(name == "Day" || name == "Back" ? -235f : 235f,
                    name == "Day" || name == "Night" ? -125f : -240f);
            }
            return;
        }
        var leftColumn = (RectTransform)circuit.transform;
        var rightColumn = (RectTransform)lakeTown.transform;
        MatchButtonWidth(day, leftColumn);
        MatchButtonWidth(night, rightColumn);
        MatchButtonWidth(transform.Find("Dialog/Back")?.GetComponent<UnityEngine.UI.Button>(), leftColumn);
        MatchButtonWidth(transform.Find("Dialog/Go")?.GetComponent<UnityEngine.UI.Button>(), rightColumn);
    }
    private static void MatchButtonWidth(UnityEngine.UI.Button button, RectTransform column)
    {
        if (button == null) return;
        var rect = (RectTransform)button.transform;
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, column.rect.width);
        rect.anchoredPosition = new Vector2(column.anchoredPosition.x, rect.anchoredPosition.y);
    }
    private void UpdateActionLabels()
    {
        var driveLabel = transform.Find("Dialog/Go/Label")?.GetComponent<TMP_Text>();
        if (driveLabel != null) driveLabel.text = "DRIVE";
        var subtitle = transform.Find("Dialog/Subtitle")?.GetComponent<TMP_Text>();
        if (subtitle != null) subtitle.text = "FREE RUN  /  Choose course and lighting, then DRIVE";
    }
    private void UpdateLakeTownDescription()
    {
        if (lakeTown == null) return;
        var details = lakeTown.transform.Find("Details")?.GetComponent<TMP_Text>();
        if (details != null) details.text = "Lakeside town + winding roads";
        var trial = lakeTown.transform.Find("Trial");
        if (trial != null) trial.gameObject.SetActive(false);
    }
    private void MatchActionButtonColors()
    {
        var back = transform.Find("Dialog/Back")?.GetComponent<UnityEngine.UI.Button>();
        var go = transform.Find("Dialog/Go")?.GetComponent<UnityEngine.UI.Button>();
        if (back == null || go == null) return;
        if (back.targetGraphic != null && go.targetGraphic != null)
            go.targetGraphic.color = back.targetGraphic.color;
        var backLabel = back.GetComponentInChildren<TMP_Text>(true);
        var goLabel = go.GetComponentInChildren<TMP_Text>(true);
        if (backLabel != null && goLabel != null) goLabel.color = backLabel.color;
    }
    private void AddToonButtonIcons()
    {
        AddToonIcon(day, "Sun");
        AddToonIcon(night, "MoonStar");
        AddToonIcon(transform.Find("Dialog/Back")?.GetComponent<UnityEngine.UI.Button>(), "ArrowLeft");
        AddToonIcon(transform.Find("Dialog/Go")?.GetComponent<UnityEngine.UI.Button>(), "ArrowRight");
    }
    private static void AddToonIcon(UnityEngine.UI.Button button, string asset)
    {
        if (button == null) return;
        var sprite = Resources.Load<Sprite>("FreeRideButtonToons/" + asset);
        var label = button.transform.Find("Label")?.GetComponent<TMP_Text>();
        if (sprite == null || label == null) return;
        const float iconSize = 44f, gap = 12f, padding = 12f;
        var buttonRect = (RectTransform)button.transform;
        float textWidth = Mathf.Min(label.GetPreferredValues(label.text).x,
            buttonRect.rect.width - iconSize - gap - padding * 2f);
        var iconRect = button.transform.Find("ToonIcon") as RectTransform;
        if (iconRect == null)
        {
            var icon = new GameObject("ToonIcon", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            icon.transform.SetParent(button.transform, false);
            iconRect = (RectTransform)icon.transform;
        }
        iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(.5f, .5f);
        iconRect.sizeDelta = Vector2.one * iconSize;
        iconRect.anchoredPosition = new Vector2(-(textWidth + gap) * .5f, 0f);
        var image = iconRect.GetComponent<UnityEngine.UI.Image>();
        image.sprite = sprite;
        image.color = label.color;
        image.preserveAspect = true;
        image.raycastTarget = false;
        var textRect = label.rectTransform;
        textRect.anchorMin = textRect.anchorMax = textRect.pivot = new Vector2(.5f, .5f);
        textRect.sizeDelta = new Vector2(buttonRect.rect.width - iconSize - gap - padding * 2f, textRect.sizeDelta.y);
        textRect.anchoredPosition = new Vector2((iconSize + gap) * .5f, textRect.anchoredPosition.y);
    }
    private void LateUpdate() => Refresh();
    private void Refresh()
    {
        bool isNight = manager != null && manager.FreeRideLighting == TestDriveSelection.TimeOfDay.Night;
        Tint(day, !isNight);
        Tint(night, isNight);
        bool lakeSelected = manager != null && manager.FreeRideCourse == TestDriveSelection.CourseKind.LakeTown;
        TintCourse(circuit, manager == null || manager.FreeRideCourse == TestDriveSelection.CourseKind.TestCircuit);
        TintCourse(mountainPass, manager != null && manager.FreeRideCourse == TestDriveSelection.CourseKind.MountainPass);
        TintCourse(lakeTown, lakeSelected);
        TintCourse(harborStreet, manager != null && manager.FreeRideCourse == TestDriveSelection.CourseKind.HarborStreet);
    }
    private static void TintCourse(UnityEngine.UI.Button button, bool selected)
    {
        if (button == null) return;
        var outline = button.GetComponent<UnityEngine.UI.Outline>();
        if (outline != null) outline.enabled = selected;
        var title = button.transform.Find("Label")?.GetComponent<TMP_Text>();
        if (title != null) title.color = selected ? new Color(.98f, .77f, .25f) : Color.white;
    }
    private static void Tint(UnityEngine.UI.Button button, bool selected)
    {
        if (button == null) return;
        button.GetComponent<UnityEngine.UI.Image>().color = selected ? new Color(.98f, .77f, .25f) : new Color(.10f, .22f, .26f);
        var foreground = selected ? new Color(.04f, .06f, .08f) : Color.white;
        button.GetComponentInChildren<TMP_Text>().color = foreground;
        var icon = button.transform.Find("ToonIcon")?.GetComponent<UnityEngine.UI.Image>();
        if (icon != null) icon.color = foreground;
    }
}
