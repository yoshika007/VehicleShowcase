using UnityEngine;

public abstract class Vehicle : MonoBehaviour
{
    [SerializeField] private string displayName;
    [SerializeField] private string category;

    public enum VehicleLayout { FF, FR, MR, RR, FourWheelDrive }
    [SerializeField] private VehicleLayout layout;

    public VehicleLayout Layout => layout;
    public string LayoutCode => layout == VehicleLayout.FourWheelDrive ? "4WD" : layout.ToString();

    // ENCAPSULATION
    public string DisplayName
    {
        get => displayName;
        protected set => displayName = value;
    }

    public string Category
    {
        get => category;
        protected set => category = value;
    }

    public abstract string GetDiscription();
}
