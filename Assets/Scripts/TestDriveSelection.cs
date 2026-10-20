using UnityEngine;

// Keeps the chosen vehicle during scene changes in the current play session.
public static class TestDriveSelection
{
    public enum DriveMode { FreeRun, Challenge }
    public static DriveMode Mode { get; private set; }
    public static int VehicleIndex { get; private set; }
    public static bool HasSelection { get; private set; }

    public static void Select(int index, DriveMode mode = DriveMode.FreeRun)
    {
        VehicleIndex = index;
        Mode = mode;
        HasSelection = true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Clear()
    {
        VehicleIndex = 0;
        Mode = DriveMode.FreeRun;
        HasSelection = false;
    }
}
