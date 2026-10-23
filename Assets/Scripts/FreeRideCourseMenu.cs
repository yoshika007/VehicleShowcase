using UnityEngine;
using TMPro;

// Reflects the selected lighting without changing the course card callbacks.
public class FreeRideCourseMenu : MonoBehaviour
{
    [SerializeField] private VehicleShowcaseManager manager;
    [SerializeField] private UnityEngine.UI.Button day;
    [SerializeField] private UnityEngine.UI.Button night;
    private void LateUpdate()
    {
        bool isNight = manager != null && manager.FreeRideLighting == TestDriveSelection.TimeOfDay.Night;
        Tint(day, !isNight);
        Tint(night, isNight);
    }
    private static void Tint(UnityEngine.UI.Button button, bool selected)
    {
        if (button == null) return;
        button.GetComponent<UnityEngine.UI.Image>().color = selected ? new Color(.98f, .77f, .25f) : new Color(.10f, .22f, .26f);
        button.GetComponentInChildren<TMP_Text>().color = selected ? new Color(.04f, .06f, .08f) : Color.white;
    }
}
