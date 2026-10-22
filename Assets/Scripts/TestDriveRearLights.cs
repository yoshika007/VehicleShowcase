using UnityEngine;

// Uses only the rear lamp lenses; headlamps, roof beacons and shared materials stay independent.
[RequireComponent(typeof(TestDriveMovement))]
public class TestDriveRearLights : MonoBehaviour
{
    [SerializeField] private Renderer rearLamps;
    [SerializeField] private Renderer[] auxiliaryBrakeLamps;
    private TestDriveMovement movement;
    private MaterialPropertyBlock properties;
    private int displayedState = -1;
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        movement = GetComponent<TestDriveMovement>();
        properties = new MaterialPropertyBlock();
    }

    private void LateUpdate()
    {
        int state = movement.IsBraking ? 1 : movement.IsReversing ? 2 : 0;
        Show(state);
    }

    private void Show(int state)
    {
        if (rearLamps == null || displayedState == state) return;
        if (properties == null) properties = new MaterialPropertyBlock();
        bool night = TestDriveSelection.IsNight;
        Color color = state == 2 ? new Color(.92f, .97f, 1f)
            : state == 1 ? new Color(1f, .015f, .005f)
            : night ? new Color(.6f, .008f, .004f) : new Color(.28f, .008f, .004f);
        // Night running lamps stay softly red; braking adds a restrained increase.
        Color emission = state == 2 ? color * 3f
            : state == 1 ? color * (night ? 1.6f : 6f)
            : night ? new Color(.8f, .012f, .006f) : color * .3f;
        rearLamps.GetPropertyBlock(properties);
        properties.SetColor(BaseColor, color);
        properties.SetColor(EmissionColor, emission);
        rearLamps.SetPropertyBlock(properties);
        // High-mounted auxiliary lamps are red brake/running lights, never reverse lights.
        if (auxiliaryBrakeLamps != null)
        {
            properties.SetColor(BaseColor, state == 1 ? new Color(1f, .015f, .005f)
                : night ? new Color(.6f, .008f, .004f) : new Color(.28f, .008f, .004f));
            properties.SetColor(EmissionColor, state == 1 ? new Color(1f, .015f, .005f) * (night ? 1.6f : 6f)
                : night ? new Color(.8f, .012f, .006f) : new Color(.084f, .0024f, .0012f));
            foreach (var lamp in auxiliaryBrakeLamps)
                if (lamp != null) lamp.SetPropertyBlock(properties);
        }
        displayedState = state;
    }

    private void OnEnable() => displayedState = -1;
    private void OnDisable() => Show(0);
}
