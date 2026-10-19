using UnityEngine;

// Audio classification only; this never changes collision or tire grip.
public class TestDriveAudioSurface : MonoBehaviour
{
    public enum Kind { Asphalt, Dirt, Gravel, Grass }
    [SerializeField] private Kind kind;
    public Kind SurfaceKind => kind;
}
