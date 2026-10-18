using UnityEngine;

// Keeps the chosen vehicle during scene changes in the current play session.
public static class TestDriveSelection
{
    public static int VehicleIndex { get; private set; }
    public static bool HasSelection { get; private set; }

    public static void Select(int index)
    {
        VehicleIndex = index;
        HasSelection = true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Clear()
    {
        VehicleIndex = 0;
        HasSelection = false;
    }
}
