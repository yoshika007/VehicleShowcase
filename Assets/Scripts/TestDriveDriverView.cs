using UnityEngine;
using UnityEngine.Rendering;

// The transform defines the driver's eye position for this vehicle.
public class TestDriveDriverView : MonoBehaviour
{
    [SerializeField] private Renderer[] hiddenInDriverView;
    private ShadowCastingMode[] originalShadows;

    public void SetDriverView(bool active)
    {
        if (hiddenInDriverView == null) return;
        if (originalShadows == null)
        {
            originalShadows = new ShadowCastingMode[hiddenInDriverView.Length];
            for (int i = 0; i < hiddenInDriverView.Length; i++)
                if (hiddenInDriverView[i] != null) originalShadows[i] = hiddenInDriverView[i].shadowCastingMode;
        }
        for (int i = 0; i < hiddenInDriverView.Length; i++)
            if (hiddenInDriverView[i] != null)
                hiddenInDriverView[i].shadowCastingMode = active ? ShadowCastingMode.ShadowsOnly : originalShadows[i];
    }

    private void OnDisable() => SetDriverView(false);
}
