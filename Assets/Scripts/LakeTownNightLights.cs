using UnityEngine;

// Visible lanterns stay glowing; only nearby lights illuminate the surroundings.
public class LakeTownNightLights : MonoBehaviour
{
    [SerializeField] private Light[] streetlights;
    [SerializeField] private Renderer[] windows;
    [SerializeField] private Material nightWindows;
    [SerializeField] private Renderer[] lanterns;
    [SerializeField] private Material nightLanterns;
    [SerializeField] private float fullLightDistance = 40f;
    [SerializeField] private float fadeOutDistance = 65f;
    [SerializeField] private float fadeSpeed = 2f;
    private float[] brightness;
    private Camera view;
    private bool night;

    private void Awake()
    {
        night = TestDriveSelection.IsNight;
        brightness = new float[streetlights.Length];
        view = Camera.main;
        for (int i = 0; i < streetlights.Length; i++)
        {
            var lamp = streetlights[i];
            if (lamp == null) continue;
            brightness[i] = lamp.intensity;
            lamp.enabled = false;
            lamp.intensity = 0f;
        }
        if (!night) return;
        foreach (var window in windows) if (window != null) window.sharedMaterial = nightWindows;
        foreach (var lantern in lanterns) if (lantern != null) lantern.sharedMaterial = nightLanterns;
        UpdateLights(1f);
    }

    private void LateUpdate()
    {
        if (night) UpdateLights(Time.deltaTime * fadeSpeed);
    }

    private void UpdateLights(float step)
    {
        if (view == null) view = Camera.main;
        if (view == null) return;
        var position = view.transform.position;
        for (int i = 0; i < streetlights.Length; i++)
        {
            var lamp = streetlights[i];
            if (lamp == null) continue;
            float distance = Vector3.Distance(lamp.transform.position, position);
            float target = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(fullLightDistance, fadeOutDistance, distance));
            lamp.intensity = Mathf.MoveTowards(lamp.intensity, brightness[i] * target, brightness[i] * step);
            lamp.enabled = lamp.intensity > .001f;
        }
    }

    private void OnDisable()
    {
        foreach (var lamp in streetlights) if (lamp != null) lamp.enabled = false;
    }
}
