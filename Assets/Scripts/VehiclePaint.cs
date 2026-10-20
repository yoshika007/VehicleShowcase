using System.Collections.Generic;
using UnityEngine;

// Each palette keeps an independent choice throughout the current play session.
public class VehiclePaint : MonoBehaviour
{
    [SerializeField] private VehiclePaintPalette palette;
    [SerializeField] private Renderer body;
    [SerializeField, Min(0)] private int materialSlot;
    private static readonly Dictionary<string, int> selections = new Dictionary<string, int>();
    public VehiclePaintPalette Palette => palette;
    public int SelectedIndex => palette != null && selections.TryGetValue(palette.Id, out int index)
        ? Mathf.Clamp(index, 0, Mathf.Max(0, palette.Count - 1)) : 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ClearSelections() => selections.Clear();
    private void Awake() => Apply();
    public void SelectColor(int index)
    {
        if (palette == null || index < 0 || index >= palette.Count || palette.GetOption(index).material == null) return;
        selections[palette.Id] = index;
        Apply();
    }
    private void Apply()
    {
        if (palette == null || palette.Count == 0 || body == null) return;
        var materials = body.sharedMaterials;
        if (materialSlot >= materials.Length || palette.GetOption(SelectedIndex).material == null) return;
        materials[materialSlot] = palette.GetOption(SelectedIndex).material;
        body.sharedMaterials = materials;
    }
}
