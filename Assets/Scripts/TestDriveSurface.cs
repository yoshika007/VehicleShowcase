using UnityEngine;

// WheelColliders use their own friction curves rather than a collider's Physics Material.
public class TestDriveSurface : MonoBehaviour
{
    [SerializeField, Range(0.1f, 1f)] private float gripMultiplier = 0.8f;
    [SerializeField] private Color dustColor = new Color(0.64f, 0.48f, 0.31f, 0.28f);
    public Color DustColor => dustColor;
    public float GripMultiplier => gripMultiplier;
}
