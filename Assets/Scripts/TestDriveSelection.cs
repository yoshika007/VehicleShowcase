using UnityEngine;

// Keeps the chosen vehicle during scene changes in the current play session.
public static class TestDriveSelection
{
    public enum DriveMode { FreeRun, Challenge }
    public enum TimeOfDay { Day, Night }
    public static TimeOfDay Lighting { get; private set; }
    public static bool IsNight => Mode == DriveMode.FreeRun && Lighting == TimeOfDay.Night;
    public static DriveMode Mode { get; private set; }
    public static int VehicleIndex { get; private set; }
    public static bool HasSelection { get; private set; }

    public static void Select(int index, DriveMode mode = DriveMode.FreeRun, TimeOfDay lighting = TimeOfDay.Day)
    {
        VehicleIndex = index;
        Mode = mode;
        Lighting = mode == DriveMode.Challenge ? TimeOfDay.Day : lighting;
        HasSelection = true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Clear()
    {
        VehicleIndex = 0;
        Mode = DriveMode.FreeRun;
        Lighting = TimeOfDay.Day;
        HasSelection = false;
    }
}
