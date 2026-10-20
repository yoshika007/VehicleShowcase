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
    [SerializeField] private Option[] options = new Option[0];
    public string Id => paletteId;
    public int Count => options.Length;
    public Option GetOption(int index) => options[index];
}
