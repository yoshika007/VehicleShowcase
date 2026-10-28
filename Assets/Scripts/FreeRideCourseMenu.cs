using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

// Course selection before choosing Free Run or Challenge.
public class FreeRideCourseMenu : MonoBehaviour
{
    [SerializeField] private VehicleShowcaseManager manager;
    [SerializeField] private UnityEngine.UI.Button circuit;
    [SerializeField] private UnityEngine.UI.Button lakeTown;
    [SerializeField] private UnityEngine.UI.Button mountainPass;
    [SerializeField] private UnityEngine.UI.Button harborStreet;
    [SerializeField] private UnityEngine.UI.Button pineRidge;
    private UnityEngine.UI.Button highlandClimb;
    private const int VisibleCourses = 3;
    private UnityEngine.UI.Button previousPage, nextPage;
    private int firstVisible;
    private UnityEngine.UI.Button[] CourseButtons => new[] { circuit, lakeTown, mountainPass, pineRidge, harborStreet, highlandClimb };

    private void OnEnable()
    {
        EnsureHighlandCourse();
        ArrangeWithoutSubtitle();
        firstVisible = manager != null && manager.FreeRideCourse == TestDriveSelection.CourseKind.HighlandClimb ? 3
            : manager != null && manager.FreeRideCourse == TestDriveSelection.CourseKind.HarborStreet ? 2
            : manager != null && manager.FreeRideCourse == TestDriveSelection.CourseKind.PineRidge ? 1 : 0;
        ArrangeCourses();
        EnsurePagingControls();
        MatchButtonSizes();
        AddCoursePhotos();
        UpdateCourseDescriptions();
        AddToonButtonIcons();
        Refresh();
    }
    private void EnsureHighlandCourse()
    {
        if (highlandClimb == null && pineRidge != null)
        {
            highlandClimb = Instantiate(pineRidge, pineRidge.transform.parent);
            highlandClimb.name = "HighlandClimbCourse";
            highlandClimb.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            highlandClimb.onClick.AddListener(() => manager.HighlandClimbRide());
            var label = highlandClimb.transform.Find("Label")?.GetComponent<TMP_Text>();
            if (label != null) label.text = "HIGHLAND CLIMB";
        }
        if (highlandClimb != null) highlandClimb.interactable = manager != null && manager.CanDriveHighlandClimb;
    }
    private void ArrangeWithoutSubtitle()
    {
        var subtitle = transform.Find("Dialog/Subtitle");
        if (subtitle == null || subtitle.gameObject.activeSelf) return;
        var dialog = transform.Find("Dialog") as RectTransform;
        if (dialog != null) dialog.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 690f);
        SetRowPosition("Heading", 290f);
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
        var dialog = transform.Find("Dialog") as RectTransform;
        if (dialog != null) dialog.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1150f);
        var buttons = CourseButtons;
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;
            var rect = (RectTransform)buttons[i].transform;
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 300f);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 380f);
            foreach (var label in buttons[i].GetComponentsInChildren<TMP_Text>(true))
            {
                label.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 280f);
                if (label.name == "Label")
                {
                    label.enableAutoSizing = true;
                    label.fontSizeMin = 24f;
                    label.fontSizeMax = 34f;
                    label.textWrappingMode = TextWrappingModes.NoWrap;
                    label.rectTransform.anchoredPosition = new Vector2(0f, 150f);
                }
            }
            var swipe = buttons[i].GetComponent<CourseCardSwipe>();
            if (swipe == null) swipe = buttons[i].gameObject.AddComponent<CourseCardSwipe>();
            swipe.Initialize(this);
        }
        UpdatePage();
    }

    private void EnsurePagingControls()
    {
        var dialog = transform.Find("Dialog");
        if (dialog == null) return;
        if (previousPage == null) previousPage = CreatePageButton(dialog, "PreviousCourses", "ArrowLeft", -520f, -1);
        if (nextPage == null) nextPage = CreatePageButton(dialog, "NextCourses", "ArrowRight", 520f, 1);
        UpdatePage();
    }

    private UnityEngine.UI.Button CreatePageButton(Transform parent, string name, string iconName, float x, int direction)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
        var rect = (RectTransform)obj.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = new Vector2(66f, 80f);
        rect.anchoredPosition = new Vector2(x, 50f);
        var image = obj.GetComponent<UnityEngine.UI.Image>();
        image.color = new Color(.10f, .34f, .38f, 1f);
        var button = obj.GetComponent<UnityEngine.UI.Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => ChangePage(direction));
        var icon = new GameObject("Icon", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        var iconRect = (RectTransform)icon.transform;
        iconRect.SetParent(rect, false);
        iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(.5f, .5f);
        iconRect.sizeDelta = new Vector2(40f, 40f);
        var iconImage = icon.GetComponent<UnityEngine.UI.Image>();
        iconImage.sprite = Resources.Load<Sprite>("FreeRideButtonToons/" + iconName);
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
        return button;
    }

    public void ChangePage(int direction)
    {
        int count = 0;
        foreach (var button in CourseButtons) if (button != null) count++;
        firstVisible = Mathf.Clamp(firstVisible + direction, 0, Mathf.Max(0, count - VisibleCourses));
        UpdatePage();
    }

    private void UpdatePage()
    {
        int index = 0;
        foreach (var button in CourseButtons)
        {
            if (button == null) continue;
            bool visible = index >= firstVisible && index < firstVisible + VisibleCourses;
            button.gameObject.SetActive(visible);
            if (visible) ((RectTransform)button.transform).anchoredPosition = new Vector2((index - firstVisible - 1) * 320f, 50f);
            index++;
        }
        if (previousPage != null) previousPage.interactable = firstVisible > 0;
        if (nextPage != null) nextPage.interactable = firstVisible + VisibleCourses < index;
    }
    private void AddCoursePhotos()
    {
        SetCoursePhoto(circuit, "TestCircuit");
        SetCoursePhoto(lakeTown, "LakeTown");
        SetCoursePhoto(mountainPass, "MountainPass");
        SetCoursePhoto(harborStreet, "HarborStreet");
        SetCoursePhoto(pineRidge, "PineRidge");
        SetCoursePhoto(highlandClimb, "HighlandClimb");
    }
    private static void SetCoursePhoto(UnityEngine.UI.Button button, string asset)
    {
        if (button == null) return;
        var sprite = Resources.Load<Sprite>("CoursePreviews/" + asset);
        if (sprite == null) return;
        var oldPhoto = button.transform.Find("CoursePhoto");
        if (oldPhoto != null) oldPhoto.gameObject.SetActive(false);

        // Fill a consistent photo window without stretching images with different aspect ratios.
        var frame = button.transform.Find("CoursePhotoFrame") as RectTransform;
        if (frame == null)
        {
            frame = new GameObject("CoursePhotoFrame", typeof(RectTransform), typeof(UnityEngine.UI.RectMask2D)).GetComponent<RectTransform>();
            frame.SetParent(button.transform, false);
        }
        frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(.5f, .5f);
        frame.anchoredPosition = new Vector2(0f, 35f);
        var frameSize = new Vector2(((RectTransform)button.transform).rect.width - 20f, 160f);
        frame.sizeDelta = frameSize;

        var photo = frame.Find("CoursePhoto") as RectTransform;
        if (photo == null)
        {
            photo = new GameObject("CoursePhoto", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<RectTransform>();
            photo.SetParent(frame, false);
        }
        photo.anchorMin = photo.anchorMax = photo.pivot = new Vector2(.5f, .5f);
        photo.anchoredPosition = Vector2.zero;
        var spriteSize = sprite.rect.size;
        float scale = Mathf.Max(frameSize.x / spriteSize.x, frameSize.y / spriteSize.y);
        photo.sizeDelta = spriteSize * scale;
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
        SetCourseDescription(pineRidge, "Forest dirt trails\nRolling hills\nShallow crossing");
        SetCourseDescription(highlandClimb, manager != null && manager.CanDriveHighlandClimb
            ? "Steep climbs\nRocky hills\nOff-road vehicles only"
            : "Off-road vehicles only\nChoose Trailmaster\nor Sentinel");
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
        TintCourse(pineRidge, manager != null && manager.FreeRideCourse == TestDriveSelection.CourseKind.PineRidge);
        TintCourse(highlandClimb, manager != null && manager.FreeRideCourse == TestDriveSelection.CourseKind.HighlandClimb);
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

// A drag on a card changes the visible set; an ordinary tap still selects the card.
public class CourseCardSwipe : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    private FreeRideCourseMenu menu;
    private Vector2 start;
    public void Initialize(FreeRideCourseMenu target) => menu = target;
    public void OnBeginDrag(PointerEventData eventData) => start = eventData.position;
    public void OnEndDrag(PointerEventData eventData)
    {
        Vector2 delta = eventData.position - start;
        if (menu != null && Mathf.Abs(delta.x) >= 60f && Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            menu.ChangePage(delta.x < 0f ? 1 : -1);
    }
}
