using UnityEngine;

// WheelColliders use their own friction curves rather than a collider's Physics Material.
public class TestDriveSurface : MonoBehaviour
{
    public enum Kind { Dry, Mud, Water }

    [SerializeField] private Kind kind = Kind.Dry;
    [SerializeField, Range(0.1f, 1f)] private float gripMultiplier = 0.8f;
    [SerializeField] private Color dustColor = new Color(0.64f, 0.48f, 0.31f, 0.28f);
    [Tooltip("Constant resistance in m/s squared, applied in proportion to wheel contact. Zero preserves normal coasting.")]
    [SerializeField, Min(0f)] private float resistanceAcceleration;
    [Tooltip("Additional resistance in m/s squared per m/s of vehicle speed. Useful for water and thick mud.")]
    [SerializeField, Min(0f)] private float velocityResistance;

    public Kind SurfaceKind => kind;
    public Color DustColor => dustColor;
    public float GripMultiplier => gripMultiplier;
    public float ResistanceAcceleration => Mathf.Max(0f, resistanceAcceleration);
    public float VelocityResistance => Mathf.Max(0f, velocityResistance);
}
