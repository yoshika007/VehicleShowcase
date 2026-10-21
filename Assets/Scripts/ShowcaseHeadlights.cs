using UnityEngine;

// A single welcome pulse on each displayed vehicle instance.
public class ShowcaseHeadlights : MonoBehaviour
{
    [SerializeField] private Renderer lenses;
    [SerializeField] private Light[] beams;
    [SerializeField, Min(0.01f)] private float fadeIn = .25f;
    [SerializeField, Min(0f)] private float hold = 1.1f;
    [SerializeField, Min(0.01f)] private float fadeOut = .7f;
    private MaterialPropertyBlock properties;
    private float elapsed;
    private bool finished;
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    private void OnEnable()
    {
        elapsed = 0f;
        finished = false;
        Apply(0f);
    }

    private void Update()
    {
        if (finished) return;
        elapsed += Time.unscaledDeltaTime;
        float level = elapsed < fadeIn ? Mathf.SmoothStep(0f, 1f, elapsed / Mathf.Max(.01f, fadeIn))
            : elapsed < fadeIn + hold ? 1f
            : 1f - Mathf.SmoothStep(0f, 1f, (elapsed - fadeIn - hold) / Mathf.Max(.01f, fadeOut));
        Apply(level);
        finished = elapsed >= fadeIn + hold + fadeOut;
    }

    private void Apply(float level)
    {
        if (lenses != null)
        {
            if (properties == null) properties = new MaterialPropertyBlock();
            lenses.GetPropertyBlock(properties);
            properties.SetColor(EmissionColor, new Color(1f, .94f, .78f) * (level * 6f));
            lenses.SetPropertyBlock(properties);
        }
        if (beams == null) return;
        foreach (var beam in beams)
        {
            if (beam == null) continue;
            beam.intensity = level * 12f;
            beam.enabled = level > .001f;
        }
    }

    private void OnDisable() => Apply(0f);
}
