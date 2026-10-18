using UnityEngine;

[CreateAssetMenu(menuName = "Vehicle Showcase/Test Drive Catalog")]
public class TestDriveVehicleCatalog : ScriptableObject
{
    // Same order as the showroom's vehicle list.
    public TestDriveMovement[] drivingPrefabs;
}
