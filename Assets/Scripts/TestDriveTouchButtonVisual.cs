using UnityEngine;
using UnityEngine.UI;

// Mirrors the control's held state, including multiple touches and forced releases.
[RequireComponent(typeof(TestDriveTouchButton))]
public class TestDriveTouchButtonVisual : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Image background;
    [SerializeField] private UnityEngine.UI.Image border;
    [SerializeField] private UnityEngine.UI.Image icon;
    [SerializeField] private Color normalBackground;
    [SerializeField] private Color heldBackground;
    [SerializeField] private Color normalBorder;
    [SerializeField] private Color heldBorder;
    [SerializeField] private Color normalIcon = Color.white;
    [SerializeField] private Color heldIcon = Color.white;

    private TestDriveTouchButton control;
    private void Awake() => control = GetComponent<TestDriveTouchButton>();
    private void LateUpdate() => Refresh(control != null && control.IsHeld);
    private void OnDisable() => Refresh(false);

    private void Refresh(bool held)
    {
        if (background != null) background.color = held ? heldBackground : normalBackground;
        if (border != null) border.color = held ? heldBorder : normalBorder;
        if (icon != null) icon.color = held ? heldIcon : normalIcon;
    }
}
