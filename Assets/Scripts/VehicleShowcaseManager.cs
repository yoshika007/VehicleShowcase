using UnityEngine;

public class VehicleShowcaseManager : MonoBehaviour
{

    [SerializeField] private Vehicle[] vehiclePrefabs;
    [SerializeField] private Transform displaypoint;

    private int currentVehicleIndex;
    private Vehicle currentVehicle;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ShowVehicle();
    }

    public void ShowNextVehicle()
    {
        currentVehicleIndex++;
        if(currentVehicleIndex >= vehiclePrefabs.Length)
        {
            currentVehicleIndex = 0;
        }
        ShowVehicle();
    }

    public void ShowPreviousVehicle()
    {
        currentVehicleIndex--;
        if(currentVehicleIndex < 0)
        {
            currentVehicleIndex = vehiclePrefabs.Length -1;
        }
        ShowVehicle();
    }

    // ABSTRUCTION
    public void ShowVehicle()
    {
        if(currentVehicle != null)
        {
            Destroy(currentVehicle.gameObject);
        }
        currentVehicle = Instantiate(vehiclePrefabs[currentVehicleIndex], displaypoint.position, displaypoint.rotation);

        Debug.Log(currentVehicle.DisplayName);
        Debug.Log(currentVehicle.Category);
        Debug.Log(currentVehicle.GetDiscription());
    }

}
