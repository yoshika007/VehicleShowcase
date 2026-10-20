using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.InputSystem;
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
    [SerializeField] private AudioClip driveStartSound;
    [SerializeField] private UnityEngine.UI.Button[] navigationButtons = new UnityEngine.UI.Button[0];
    [SerializeField, Min(0f)] private float backTransitionDelay = 0.22f;

    [SerializeField] private TestDriveVehicleCatalog testDriveCatalog;

    [SerializeField] private VehiclePaintSelector paintSelector;

    [SerializeField] private GameObject driveModePanel;
    private bool[] navigationAvailability;
    private bool dragWasEnabled;
    private bool ModeMenuOpen => driveModePanel != null && driveModePanel.activeSelf;

    private bool isTransitioning;
    private int currentVehicleIndex;
    private Vehicle currentVehicle;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (driveModePanel != null) driveModePanel.SetActive(false);
        currentVehicleIndex = Mathf.Clamp(TestDriveSelection.VehicleIndex, 0, vehiclePrefabs.Length - 1);
        ShowVehicle();
    }

    public void ShowNextVehicle()
    {
        if (isTransitioning || ModeMenuOpen) return;
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
        if (isTransitioning || ModeMenuOpen) return;
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
        if (isTransitioning || ModeMenuOpen) return;
        TestDriveSelection.Clear();
        BeginTransition("TitleScene", backSound);
    }

    // Retains the original callback for existing scene-authoring tools.
    public void TestDrive() => OpenDriveMenu();

    private void Update()
    {
        if (ModeMenuOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            CloseDriveMenu();
    }

    public void OpenDriveMenu()
    {
        if (isTransitioning || ModeMenuOpen || driveModePanel == null) return;
        PlayButtonSound(navigationSound);
        navigationAvailability = new bool[navigationButtons.Length];
        for (int i = 0; i < navigationButtons.Length; i++)
        {
            if (navigationButtons[i] == null) continue;
            navigationAvailability[i] = navigationButtons[i].interactable;
            navigationButtons[i].interactable = false;
        }
        if (paintSelector != null) paintSelector.SetInteractable(false);
        if (vehicleDragRotation != null)
        {
            dragWasEnabled = vehicleDragRotation.enabled;
            vehicleDragRotation.enabled = false;
        }
        if (driveModePanel.TryGetComponent<CanvasGroup>(out var group)) group.interactable = true;
        driveModePanel.SetActive(true);
    }

    public void CloseDriveMenu()
    {
        if (isTransitioning || !ModeMenuOpen) return;
        PlayButtonSound(navigationSound);
        driveModePanel.SetActive(false);
        for (int i = 0; i < navigationButtons.Length; i++)
            if (navigationButtons[i] != null) navigationButtons[i].interactable = navigationAvailability[i];
        if (paintSelector != null) paintSelector.SetInteractable(true);
        if (vehicleDragRotation != null) vehicleDragRotation.enabled = dragWasEnabled;
    }

    public void FreeRun() => BeginDrive(TestDriveSelection.DriveMode.FreeRun);
    public void Challenge() => BeginDrive(TestDriveSelection.DriveMode.Challenge);

    private void BeginDrive(TestDriveSelection.DriveMode mode)
    {
        if (isTransitioning || !ModeMenuOpen) return;
        if (testDriveCatalog == null || currentVehicleIndex >= testDriveCatalog.drivingPrefabs.Length ||
            testDriveCatalog.drivingPrefabs[currentVehicleIndex] == null)
        {
            Debug.LogError("No driving prefab is configured for this vehicle.", this);
            return;
        }
        TestDriveSelection.Select(currentVehicleIndex, mode);
        BeginTransition("TestTrackScene", driveStartSound != null ? driveStartSound : backSound);
    }

    private void BeginTransition(string sceneName, AudioClip transitionSound)
    {
        isTransitioning = true;
        if (driveModePanel != null && driveModePanel.TryGetComponent<CanvasGroup>(out var group))
            group.interactable = false;
        if (paintSelector != null) paintSelector.SetInteractable(false);
        foreach (var button in navigationButtons)
            if (button != null) button.interactable = false;
        StartCoroutine(LoadAfterSound(sceneName, transitionSound));
    }

    private void PlayButtonSound(AudioClip clip)
    {
        if (buttonAudio == null || clip == null) return;
        // Replace the previous click instead of stacking sounds on rapid presses.
        buttonAudio.Stop();
        buttonAudio.clip = clip;
        buttonAudio.Play();
    }

    private IEnumerator LoadAfterSound(string sceneName, AudioClip transitionSound)
    {
        PlayButtonSound(transitionSound);
        float delay = backTransitionDelay;
        if (buttonAudio != null && transitionSound != null)
            delay = Mathf.Max(delay, transitionSound.length / Mathf.Max(0.01f, Mathf.Abs(buttonAudio.pitch)));
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
