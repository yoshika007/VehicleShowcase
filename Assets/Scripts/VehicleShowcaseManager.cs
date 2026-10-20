using System.Collections;
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

    [SerializeField] private AudioSource buttonAudio;
    [SerializeField] private AudioClip navigationSound;
    [SerializeField] private AudioClip backSound;
    [SerializeField] private UnityEngine.UI.Button[] navigationButtons = new UnityEngine.UI.Button[0];
    [SerializeField, Min(0f)] private float backTransitionDelay = 0.22f;

    [SerializeField] private TestDriveVehicleCatalog testDriveCatalog;

    [SerializeField] private VehiclePaintSelector paintSelector;

    private bool isTransitioning;
    private int currentVehicleIndex;
    private Vehicle currentVehicle;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentVehicleIndex = Mathf.Clamp(TestDriveSelection.VehicleIndex, 0, vehiclePrefabs.Length - 1);
        ShowVehicle();
    }

    public void ShowNextVehicle()
    {
        if (isTransitioning) return;
        PlayButtonSound(navigationSound);
        currentVehicleIndex++;
        if(currentVehicleIndex >= vehiclePrefabs.Length)
        {
            currentVehicleIndex = 0;
        }
        ShowVehicle();
    }

    public void ShowPreviousVehicle()
    {
        if (isTransitioning) return;
        PlayButtonSound(navigationSound);
        currentVehicleIndex--;
        if(currentVehicleIndex < 0)
        {
            currentVehicleIndex = vehiclePrefabs.Length -1;
        }
        ShowVehicle();
    }

    public void BackToTitle()
    {
        if (isTransitioning) return;
        TestDriveSelection.Clear();
        BeginTransition("TitleScene");
    }

    public void TestDrive()
    {
        if (isTransitioning) return;
        if (testDriveCatalog == null || currentVehicleIndex >= testDriveCatalog.drivingPrefabs.Length ||
            testDriveCatalog.drivingPrefabs[currentVehicleIndex] == null)
        {
            Debug.LogError("No driving prefab is configured for this vehicle.", this);
            return;
        }
        TestDriveSelection.Select(currentVehicleIndex);
        BeginTransition("TestTrackScene");
    }

    private void BeginTransition(string sceneName)
    {
        isTransitioning = true;
        if (paintSelector != null) paintSelector.SetInteractable(false);
        foreach (var button in navigationButtons)
            if (button != null) button.interactable = false;
        StartCoroutine(LoadAfterSound(sceneName));
    }

    private void PlayButtonSound(AudioClip clip)
    {
        if (buttonAudio == null || clip == null) return;
        // Replace the previous click instead of stacking sounds on rapid presses.
        buttonAudio.Stop();
        buttonAudio.clip = clip;
        buttonAudio.Play();
    }

    private IEnumerator LoadAfterSound(string sceneName)
    {
        PlayButtonSound(backSound);
        float delay = backTransitionDelay;
        if (buttonAudio != null && backSound != null)
            delay = Mathf.Max(delay, backSound.length / Mathf.Max(0.01f, Mathf.Abs(buttonAudio.pitch)));
        yield return new WaitForSecondsRealtime(delay);
        SceneManager.LoadScene(sceneName);
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

        if (paintSelector != null)
            paintSelector.Bind(currentVehicle.GetComponent<VehiclePaint>(), () => PlayButtonSound(navigationSound));

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
