using UnityEngine;
using UnityEngine.Rendering;

// The transform defines the driver's eye position for this vehicle.
public class TestDriveDriverView : MonoBehaviour
{
    [SerializeField] private Renderer[] hiddenInDriverView;
    private ShadowCastingMode[] originalShadows;
    private Renderer[] vehicleMeshes;
    private ShadowCastingMode[] vehicleMeshShadows;
    private bool meshesHidden;

    public void SetView(bool driverView, bool hideVehicle)
    {
        RestoreVehicleMeshes();
        SetDriverView(driverView);
        if (!hideVehicle) return;

        // Only hide geometry, leaving particle renderers and light objects alone.
        var renderers = transform.parent.GetComponentsInChildren<Renderer>(true);
        var meshes = new System.Collections.Generic.List<Renderer>();
        foreach (var renderer in renderers)
            if (renderer is MeshRenderer || renderer is SkinnedMeshRenderer) meshes.Add(renderer);
        vehicleMeshes = meshes.ToArray();
        vehicleMeshShadows = new ShadowCastingMode[vehicleMeshes.Length];
        for (int i = 0; i < vehicleMeshes.Length; i++)
        {
            vehicleMeshShadows[i] = vehicleMeshes[i].shadowCastingMode;
            vehicleMeshes[i].shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        }
        meshesHidden = true;
    }

    private void RestoreVehicleMeshes()
    {
        if (!meshesHidden) return;
        for (int i = 0; i < vehicleMeshes.Length; i++)
            if (vehicleMeshes[i] != null) vehicleMeshes[i].shadowCastingMode = vehicleMeshShadows[i];
        meshesHidden = false;
        vehicleMeshes = null;
        vehicleMeshShadows = null;
    }

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

    private void OnDisable() => SetView(false, false);
}
