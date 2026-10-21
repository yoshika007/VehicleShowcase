using UnityEngine;

public class NightRoadsideLights : MonoBehaviour
{
    [SerializeField] private Light[] lamps;
    [SerializeField] private Renderer[] lenses;
    private void Awake()
    {
        bool night = TestDriveSelection.IsNight;
        if (lamps != null) foreach (var lamp in lamps)
            if (lamp != null) lamp.enabled = night;
        if (lenses == null) return;
        var block = new MaterialPropertyBlock();
        foreach (var lens in lenses)
        {
            if (lens == null) continue;
            lens.GetPropertyBlock(block);
            block.SetColor("_EmissionColor", night ? new Color(4f, 3.3f, 2.2f) : Color.black);
            lens.SetPropertyBlock(block);
        }
    }
}
