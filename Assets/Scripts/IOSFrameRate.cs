using UnityEngine;

// Establish a predictable frame budget on iPhone/iPad without capping the Editor or other platforms.
public static class IOSFrameRate
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Configure()
    {
        if (Application.platform == RuntimePlatform.IPhonePlayer)
            Application.targetFrameRate = 60;
    }
}
