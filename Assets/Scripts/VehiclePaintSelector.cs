using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class VehiclePaintSelector : MonoBehaviour
{
    [SerializeField] private RectTransform panel;
    [SerializeField] private RectTransform testDriveButton;
    [SerializeField] private GameObject section;
    [SerializeField] private RectTransform swatchRow;
    [SerializeField] private UnityEngine.UI.Button swatchTemplate;
    [SerializeField] private TMP_Text selectedName;
    [SerializeField] private float extraPanelHeight = 112f;
    private readonly List<UnityEngine.UI.Button> buttons = new List<UnityEngine.UI.Button>();
    private VehiclePaint vehicle;
    private Action changed;
    private Vector2 panelSize, drivePosition;
    private bool available = true;

    private void Awake()
    {
        // Give the capability chart more room while keeping the drive button below the panel.
        const float extraWidth = 56f, extraHeight = 20f;
        panelSize = panel.sizeDelta + new Vector2(extraWidth, extraHeight);
        drivePosition = testDriveButton.anchoredPosition - Vector2.up * (extraHeight * panel.localScale.y * .5f);
        swatchTemplate.gameObject.SetActive(false);
    }
    public void Bind(VehiclePaint target, Action onChanged)
    {
        vehicle = target;
        changed = onChanged;
        available = true;
        int count = target != null && target.Palette != null ? target.Palette.Count : 0;
        section.SetActive(count > 0);
        panel.sizeDelta = panelSize + Vector2.up * (count > 0 ? extraPanelHeight : 0f);
        Vector2 position = drivePosition - Vector2.up * (count > 0 ? extraPanelHeight * panel.localScale.y * .5f : 0f);
        if (testDriveButton.TryGetComponent<TestDriveButtonFeedback>(out var feedback))
            feedback.SetRestingPosition(position);
        else testDriveButton.anchoredPosition = position;
        for (int i = 0; i < count; i++)
        {
            if (i == buttons.Count)
            {
                int index = i;
                var button = Instantiate(swatchTemplate, swatchRow);
                button.name = "PaintOption" + i;
                button.onClick.AddListener(() => Select(index));
                buttons.Add(button);
            }
            buttons[i].gameObject.SetActive(true);
            buttons[i].transform.Find("Swatch").GetComponent<UnityEngine.UI.Image>().color = target.Palette.GetOption(i).swatch;
        }
        for (int i = count; i < buttons.Count; i++) buttons[i].gameObject.SetActive(false);
        Refresh();
    }
    public void SetInteractable(bool value)
    {
        available = value;
        foreach (var button in buttons) button.interactable = value;
    }
    private void Select(int index)
    {
        if (!available || vehicle == null || index == vehicle.SelectedIndex) return;
        vehicle.SelectColor(index);
        changed?.Invoke();
        Refresh();
    }
    private void Refresh()
    {
        if (vehicle == null || vehicle.Palette == null || vehicle.Palette.Count == 0) return;
        int selected = vehicle.SelectedIndex;
        selectedName.text = "EXTERIOR  /  " + vehicle.Palette.GetOption(selected).displayName;
        for (int i = 0; i < buttons.Count; i++)
        {
            buttons[i].interactable = available;
            buttons[i].GetComponent<UnityEngine.UI.Outline>().enabled = i == selected;
            buttons[i].transform.Find("Selected").gameObject.SetActive(i == selected);
        }
    }
}
