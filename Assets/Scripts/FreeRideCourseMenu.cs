using UnityEngine;
using TMPro;

// Course selection before choosing Free Run or Challenge.
public class FreeRideCourseMenu : MonoBehaviour
{
    [SerializeField] private VehicleShowcaseManager manager;
    [SerializeField] private UnityEngine.UI.Button circuit;
    [SerializeField] private UnityEngine.UI.Button lakeTown;
    [SerializeField] private UnityEngine.UI.Button mountainPass;
    [SerializeField] private UnityEngine.UI.Button harborStreet;
    private void OnEnable()
    {
        ArrangeWithoutSubtitle();
        ArrangeCourses();
        MatchButtonSizes();
        AddCoursePhotos();
        UpdateCourseDescriptions();
        AddToonButtonIcons();
        Refresh();
    }
    private void ArrangeWithoutSubtitle()
    {
        var subtitle = transform.Find("Dialog/Subtitle");
        if (subtitle == null || subtitle.gameObject.activeSelf) return;
        var dialog = transform.Find("Dialog") as RectTransform;
        if (dialog != null) dialog.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 650f);
        SetRowPosition("Heading", 270f);
        SetRowPosition("TestCircuitCourse", 50f);
        SetRowPosition("LakeTownCourse", 50f);
        SetRowPosition("Back", -240f);
        SetRowPosition("Go", -240f);
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
        if (dialog != null) dialog.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1150f);
        var buttons = harborStreet != null ? new[] { circuit, lakeTown, mountainPass, harborStreet }
            : new[] { circuit, lakeTown, mountainPass };
        bool four = harborStreet != null;
        for (int i = 0; i < buttons.Length; i++)
        {
            var rect = (RectTransform)buttons[i].transform;
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, four ? 255f : 280f);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 380f);
            rect.anchoredPosition = new Vector2((i - (buttons.Length - 1) * .5f) * (four ? 275f : 310f), 50f);
            foreach (var label in buttons[i].GetComponentsInChildren<TMP_Text>(true))
            {
                label.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, four ? 235f : 260f);
                if (label.name == "Label")
                {
                    label.enableAutoSizing = true;
                    label.fontSizeMin = four ? 20f : 24f;
                    label.fontSizeMax = four ? 30f : 34f;
                    label.textWrappingMode = TextWrappingModes.NoWrap;
                    label.rectTransform.anchoredPosition = new Vector2(0f, 150f);
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
    private void AddCoursePhotos()
    {
        SetCoursePhoto(circuit, "TestCircuit");
        SetCoursePhoto(lakeTown, "LakeTown");
        SetCoursePhoto(mountainPass, "MountainPass");
        SetCoursePhoto(harborStreet, "HarborStreet");
    }
    private static void SetCoursePhoto(UnityEngine.UI.Button button, string asset)
    {
        if (button == null) return;
        var sprite = Resources.Load<Sprite>("CoursePreviews/" + asset);
        if (sprite == null) return;
        var photo = button.transform.Find("CoursePhoto") as RectTransform;
        if (photo == null)
        {
            photo = new GameObject("CoursePhoto", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<RectTransform>();
            photo.SetParent(button.transform, false);
        }
        photo.anchorMin = photo.anchorMax = photo.pivot = new Vector2(.5f, .5f);
        photo.anchoredPosition = new Vector2(0f, 35f);
        photo.sizeDelta = new Vector2(((RectTransform)button.transform).rect.width - 20f, 160f);
        var image = photo.GetComponent<UnityEngine.UI.Image>();
        image.sprite = sprite;
        image.color = Color.white;
        image.preserveAspect = true;
        // The whole course card remains the selection target, including the photo.
        image.raycastTarget = false;
    }
    private void UpdateCourseDescriptions()
    {
        SetCourseDescription(circuit, "Fast oval\nGravel turns\nOptional hill routes");
        SetCourseDescription(lakeTown, "Lakeside streets\nTown intersections\nWinding roads");
        SetCourseDescription(mountainPass, "Mountain hairpins\nSummit tunnel\nSweeping descents");
        SetCourseDescription(harborStreet, "Tight city corners\nHotel hairpin\nSeaside tunnel");
        var trial = lakeTown != null ? lakeTown.transform.Find("Trial") : null;
        if (trial != null) trial.gameObject.SetActive(false);
    }
    private static void SetCourseDescription(UnityEngine.UI.Button button, string description)
    {
        if (button == null) return;
        var details = button.transform.Find("Details")?.GetComponent<TMP_Text>();
        if (details == null) return;
        details.gameObject.SetActive(true);
        details.text = description;
        details.fontSize = 26f;
        details.enableAutoSizing = false;
        details.textWrappingMode = TextWrappingModes.Normal;
        details.alignment = TextAlignmentOptions.Center;
        details.color = new Color(.8f, .88f, .9f);
        details.raycastTarget = false;
        var rect = details.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = new Vector2(0f, -115f);
        rect.sizeDelta = new Vector2(((RectTransform)button.transform).rect.width - 20f, 112f);
    }
    private void MatchButtonSizes()
    {
        var back = transform.Find("Dialog/Back") as RectTransform;
        if (back == null) return;
        back.sizeDelta = new Vector2(510f, 90f);
        back.anchoredPosition = new Vector2(-275f, -240f);
        var go = transform.Find("Dialog/Go") as RectTransform;
        if (go != null)
        {
            go.sizeDelta = new Vector2(510f, 90f);
            go.anchoredPosition = new Vector2(275f, -240f);
        }
    }
    private void AddToonButtonIcons()
    {
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
}
