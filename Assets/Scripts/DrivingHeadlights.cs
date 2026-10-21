using UnityEngine;

// Persistent beams belong to the driving prefab, independent of the showroom pulse.
public class DrivingHeadlights : MonoBehaviour
{
    [SerializeField] private Renderer lenses;
    [SerializeField] private Light[] beams;
    private void Awake()
    {
        bool night = TestDriveSelection.IsNight;
        if (lenses != null)
        {
            var block = new MaterialPropertyBlock();
            lenses.GetPropertyBlock(block);
            block.SetColor("_EmissionColor", night ? new Color(6f, 5.64f, 4.68f) : Color.black);
            lenses.SetPropertyBlock(block);
        }
        if (beams != null) foreach (var beam in beams)
            if (beam != null) beam.enabled = night;
    }
}
