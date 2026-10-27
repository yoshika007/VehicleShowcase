using UnityEngine;

[CreateAssetMenu(menuName = "Vehicle Showcase/Paint Palette")]
public class VehiclePaintPalette : ScriptableObject
{
    [System.Serializable]
    public class Option
    {
        public string displayName;
        public Color swatch = Color.white;
        public Material material;
    }
    [SerializeField] private string paletteId;
    [SerializeField, Min(0)] private int defaultIndex;
    [SerializeField] private Option[] options = new Option[0];
    public string Id => paletteId;
    public int Count => options.Length;
    public int DefaultIndex => Mathf.Clamp(defaultIndex, 0, Mathf.Max(0, Count - 1));
    public Option GetOption(int index) => options[index];
}
