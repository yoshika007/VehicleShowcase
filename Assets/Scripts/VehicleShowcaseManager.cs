using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.VisualScripting;

public class VehicleShowcaseManager : MonoBehaviour
{

    [SerializeField] private Vehicle[] vehiclePrefabs;
    [SerializeField] private Transform displaypoint;

    [SerializeField] private TMP_Text vehicleNameText;
    [SerializeField] private TMP_Text categoryText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text vehicleCounterText;

    [SerializeField] private VehicleDragRotation vehicleDragRotation;

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

    public void BackToTitle()
    {
        SceneManager.LoadScene("TitleScene");
    }

    // ABSTRUCTION
    public void ShowVehicle()
    {
        if(currentVehicle != null)
        {
            Destroy(currentVehicle.gameObject);
        }
        currentVehicle = Instantiate(vehiclePrefabs[currentVehicleIndex], displaypoint.position, displaypoint.rotation);

        if (vehicleDragRotation != null)
            vehicleDragRotation.SetVehicle(currentVehicle.transform);

        UpdateVehicleInfo();
    }

    private void UpdateVehicleInfo()
    {
        vehicleNameText.text = currentVehicle.DisplayName;
        categoryText.text = currentVehicle.Category;
        descriptionText.text = currentVehicle.GetDiscription();
        vehicleCounterText.text = $"{currentVehicleIndex + 1} / {vehiclePrefabs.Length}";
    }

}
