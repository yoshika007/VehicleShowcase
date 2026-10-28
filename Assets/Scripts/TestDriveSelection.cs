using UnityEngine;

// Keeps the chosen vehicle during scene changes in the current play session.
public static class TestDriveSelection
{
    public enum CourseKind { TestCircuit, LakeTown, MountainPass, HarborStreet, PineRidge, HighlandClimb }
    public static CourseKind Course { get; private set; }
    public enum DriveMode { FreeRun, Challenge }
    public enum ChallengeKind { CleanLap = 0, OffroadTrail = 3, NightLap = 5 }
    public static ChallengeKind SelectedChallenge { get; private set; }
    public enum TimeOfDay { Day, Night }
    public static TimeOfDay Lighting { get; private set; }
    public static bool IsNight => Lighting == TimeOfDay.Night;
    public static DriveMode Mode { get; private set; }
    public static int VehicleIndex { get; private set; }
    public static bool HasSelection { get; private set; }
    public static string VehicleName { get; private set; }
    private static bool introPending;

    public static bool ConsumeIntroRequest()
    {
        bool pending = introPending;
        introPending = false;
        return pending;
    }

    public static void Select(int index, DriveMode mode = DriveMode.FreeRun, TimeOfDay lighting = TimeOfDay.Day, ChallengeKind challenge = ChallengeKind.CleanLap, CourseKind course = CourseKind.TestCircuit, string vehicleName = null)
    {
        Course = mode == DriveMode.FreeRun ? course : CourseKind.TestCircuit;
        VehicleIndex = index;
        Mode = mode;
        SelectedChallenge = mode == DriveMode.Challenge ? challenge : ChallengeKind.CleanLap;
        Lighting = mode == DriveMode.Challenge
            ? (challenge == ChallengeKind.NightLap ? TimeOfDay.Night : TimeOfDay.Day) : lighting;
        HasSelection = true;
        VehicleName = vehicleName;
        introPending = true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Clear()
    {
        Course = CourseKind.TestCircuit;
        VehicleIndex = 0;
        Mode = DriveMode.FreeRun;
        Lighting = TimeOfDay.Day;
        SelectedChallenge = ChallengeKind.CleanLap;
        HasSelection = false;
        VehicleName = null;
        introPending = false;
    }
}
