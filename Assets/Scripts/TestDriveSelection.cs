using UnityEngine;

// Keeps the chosen vehicle during scene changes in the current play session.
public static class TestDriveSelection
{
    public enum DriveMode { FreeRun, Challenge }
    public enum ChallengeKind { CleanLap, CheckpointSprint, Slalom, OffroadTrail }
    public static ChallengeKind SelectedChallenge { get; private set; }
    public enum TimeOfDay { Day, Night }
    public static TimeOfDay Lighting { get; private set; }
    public static bool IsNight => Mode == DriveMode.FreeRun && Lighting == TimeOfDay.Night;
    public static DriveMode Mode { get; private set; }
    public static int VehicleIndex { get; private set; }
    public static bool HasSelection { get; private set; }

    public static void Select(int index, DriveMode mode = DriveMode.FreeRun, TimeOfDay lighting = TimeOfDay.Day, ChallengeKind challenge = ChallengeKind.CleanLap)
    {
        VehicleIndex = index;
        Mode = mode;
        SelectedChallenge = mode == DriveMode.Challenge ? challenge : ChallengeKind.CleanLap;
        Lighting = mode == DriveMode.Challenge ? TimeOfDay.Day : lighting;
        HasSelection = true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Clear()
    {
        VehicleIndex = 0;
        Mode = DriveMode.FreeRun;
        Lighting = TimeOfDay.Day;
        SelectedChallenge = ChallengeKind.CleanLap;
        HasSelection = false;
    }
}
