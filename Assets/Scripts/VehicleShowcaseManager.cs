using UnityEngine;

public class VehicleShowcaseManager : MonoBehaviour
{

    [SerializeField] private GameObject[] vehiclePrefabs;
    [SerializeField] private Transform displaypoint;

    private int currentVehicleIndex;
    private GameObject currentVehicle;

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

    public void ShowVehicle()
    {
        if(currentVehicle != null)
        {
            Destroy(currentVehicle);
        }
        currentVehicle = Instantiate(vehiclePrefabs[currentVehicleIndex], displaypoint.position, displaypoint.rotation);
    }

}
