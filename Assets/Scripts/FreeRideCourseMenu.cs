using UnityEngine;
using TMPro;

// Reflects course and lighting selections before GO starts the drive.
public class FreeRideCourseMenu : MonoBehaviour
{
    [SerializeField] private VehicleShowcaseManager manager;
    [SerializeField] private UnityEngine.UI.Button day;
    [SerializeField] private UnityEngine.UI.Button night;
    [SerializeField] private UnityEngine.UI.Button circuit;
    [SerializeField] private UnityEngine.UI.Button lakeTown;
    private void OnEnable() => Refresh();
    private void LateUpdate() => Refresh();
    private void Refresh()
    {
        bool isNight = manager != null && manager.FreeRideLighting == TestDriveSelection.TimeOfDay.Night;
        Tint(day, !isNight);
        Tint(night, isNight);
        bool lakeSelected = manager != null && manager.FreeRideCourse == TestDriveSelection.CourseKind.LakeTown;
        TintCourse(circuit, !lakeSelected);
        TintCourse(lakeTown, lakeSelected);
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
        button.GetComponentInChildren<TMP_Text>().color = selected ? new Color(.04f, .06f, .08f) : Color.white;
    }
}
