using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(UnityEngine.UI.Button))]
public class TestDriveTouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private readonly HashSet<int> pointers = new HashSet<int>();
    private UnityEngine.UI.Button button;
    public bool IsHeld => pointers.Count > 0 && isActiveAndEnabled;
    private void Awake() => button = GetComponent<UnityEngine.UI.Button>();
    public void OnPointerDown(PointerEventData data)
    {
        if (data.button == PointerEventData.InputButton.Left && button != null && button.IsInteractable())
            pointers.Add(data.pointerId);
    }
    public void OnPointerUp(PointerEventData data) => pointers.Remove(data.pointerId);
    public void OnPointerExit(PointerEventData data) => pointers.Remove(data.pointerId);
    public void Release() => pointers.Clear();
    private void OnDisable() => Release();
}
