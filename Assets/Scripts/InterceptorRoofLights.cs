using UnityEngine;

// Synchronized rapid flashes are confined to the Interceptor's roof lenses.
public class InterceptorRoofLights : MonoBehaviour
{
    [SerializeField] private Renderer lenses;
    [SerializeField] private Light redSpill;
    [SerializeField] private Light blueSpill;
    private MaterialPropertyBlock properties;
    private float startedAt;
    private int displayedState = -1;
    private bool allowSpill;
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    private static readonly Color Red = new Color(1f, .012f, .025f);
    private static readonly Color Blue = new Color(.015f, .12f, 1f);

    private void OnEnable()
    {
        // Keep the lenses flashing while preventing their lights from causing
        // nearby scenery to pulse in any driving scene. The showroom keeps its spill.
        allowSpill = GetComponentInParent<TestDriveMovement>() == null;
        startedAt = Time.time;
        displayedState = -1;
        Update();
    }

    private void Update()
    {
        bool flashing = Mathf.Repeat(Time.time - startedAt, .3f) < .15f;
        int state = flashing ? 1 : 0;
        if (state == displayedState) return;
        displayedState = state;
        ShowLens(0, Red, state == 1);
        ShowLens(1, Blue, flashing);
        if (redSpill != null) redSpill.enabled = allowSpill && state == 1;
        if (blueSpill != null) blueSpill.enabled = allowSpill && flashing;
    }

    private void ShowLens(int slot, Color color, bool lit)
    {
        if (lenses == null) return;
        if (properties == null) properties = new MaterialPropertyBlock();
        properties.Clear();
        lenses.GetPropertyBlock(properties, slot);
        properties.SetColor(BaseColor, color * (lit ? 1f : .28f));
        properties.SetColor(EmissionColor, color * (lit ? 8f : .08f));
        lenses.SetPropertyBlock(properties, slot);
    }

    private void OnDisable()
    {
        ShowLens(0, Red, false);
        ShowLens(1, Blue, false);
        if (redSpill != null) redSpill.enabled = false;
        if (blueSpill != null) blueSpill.enabled = false;
        displayedState = -1;
    }
}
