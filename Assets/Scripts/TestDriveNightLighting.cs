using UnityEngine;
using UnityEngine.Rendering;

// Only overrides this scene's lighting for a selected night drive or Night Lap challenge.
public class TestDriveNightLighting : MonoBehaviour
{
    [SerializeField] private Light sun;
    [SerializeField] private Material nightSky;
    private void Awake()
    {
        if (!TestDriveSelection.IsNight) return;
        RenderSettings.skybox = nightSky;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.18f, .22f, .32f);
        RenderSettings.ambientEquatorColor = new Color(.12f, .15f, .20f);
        RenderSettings.ambientGroundColor = new Color(.065f, .075f, .10f);
        RenderSettings.reflectionIntensity = .15f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = new Color(.025f, .04f, .075f);
        RenderSettings.fogDensity = .002f;
        if (sun != null)
        {
            sun.color = new Color(.53f, .67f, 1f);
            sun.intensity = .24f;
            sun.transform.rotation = Quaternion.Euler(28f, -35f, 0f);
            RenderSettings.sun = sun;
        }
    }
}
