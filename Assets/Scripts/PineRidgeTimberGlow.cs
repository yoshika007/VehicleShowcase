using UnityEngine;

// The existing batched timber remains unchanged by day and glows softly at night.
[RequireComponent(typeof(Renderer))]
public sealed class PineRidgeTimberGlow : MonoBehaviour
{
    [SerializeField, ColorUsage(false, true)] private Color nightEmission = new Color(.55f, .22f, .055f);
    private Material nightMaterial;

    private void Awake()
    {
        if (!TestDriveSelection.IsNight) return;
        var target = GetComponent<Renderer>();
        if (target.sharedMaterial == null || !target.sharedMaterial.HasProperty("_EmissionColor")) return;
        nightMaterial = new Material(target.sharedMaterial);
        nightMaterial.EnableKeyword("_EMISSION");
        nightMaterial.SetColor("_EmissionColor", nightEmission);
        target.sharedMaterial = nightMaterial;
    }

    private void OnDestroy()
    {
        if (nightMaterial != null) Destroy(nightMaterial);
    }
}
