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
    [SerializeField] private TMP_Text layoutText;
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
    [SerializeField] private GameObject timeOfDayPanel;
    [SerializeField] private GameObject challengePanel;
    [SerializeField] private GameObject courseSelectionPanel;
    private TestDriveSelection.TimeOfDay freeRideLighting;
    public TestDriveSelection.TimeOfDay FreeRideLighting => freeRideLighting;
    private TestDriveSelection.CourseKind freeRideCourse;
    public TestDriveSelection.CourseKind FreeRideCourse => freeRideCourse;
    private bool CourseMenuOpen => courseSelectionPanel != null && courseSelectionPanel.activeSelf;
    private bool ChallengeMenuOpen => challengePanel != null && challengePanel.activeSelf;
    private bool[] navigationAvailability;
    private bool dragWasEnabled;
    private bool TimeMenuOpen => timeOfDayPanel != null && timeOfDayPanel.activeSelf;
    private bool ModeMenuOpen => (driveModePanel != null && driveModePanel.activeSelf) || TimeMenuOpen || ChallengeMenuOpen || CourseMenuOpen;

    private bool isTransitioning;
    private int currentVehicleIndex;
    private Vehicle currentVehicle;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (driveModePanel != null) driveModePanel.SetActive(false);
        if (timeOfDayPanel != null) timeOfDayPanel.SetActive(false);
        if (challengePanel != null) challengePanel.SetActive(false);
        if (courseSelectionPanel != null) courseSelectionPanel.SetActive(false);
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
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) NavigateBack();
    }

    public void NavigateBack()
    {
        if (!ModeMenuOpen || isTransitioning) return;
        if (CourseMenuOpen) CloseDriveMenu();
        else if (TimeMenuOpen || ChallengeMenuOpen) BackToDriveModes();
        else BackToCourses();
    }

    public void OpenDriveMenu()
    {
        if (isTransitioning || ModeMenuOpen || courseSelectionPanel == null) return;
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
        courseSelectionPanel.SetActive(true);
        if (courseSelectionPanel.TryGetComponent<CanvasGroup>(out var group)) group.interactable = true;
    }

    public void CloseDriveMenu()
    {
        if (isTransitioning || !ModeMenuOpen) return;
        PlayButtonSound(navigationSound);
        driveModePanel.SetActive(false);
        if (timeOfDayPanel != null) timeOfDayPanel.SetActive(false);
        if (challengePanel != null) challengePanel.SetActive(false);
        if (courseSelectionPanel != null) courseSelectionPanel.SetActive(false);
        for (int i = 0; i < navigationButtons.Length; i++)
            if (navigationButtons[i] != null) navigationButtons[i].interactable = navigationAvailability[i];
        if (paintSelector != null) paintSelector.SetInteractable(true);
        if (vehicleDragRotation != null) vehicleDragRotation.enabled = dragWasEnabled;
    }

    public void FreeRun()
    {
        if (isTransitioning || driveModePanel == null || !driveModePanel.activeSelf) return;
        PlayButtonSound(navigationSound);
        driveModePanel.SetActive(false);
        ShowTimeOfDay();
    }

    public void ConfirmCourse()
    {
        if (isTransitioning || !CourseMenuOpen || driveModePanel == null) return;
        PlayButtonSound(navigationSound);
        courseSelectionPanel.SetActive(false);
        ShowDriveModes();
    }

    public void BackToCourses()
    {
        if (isTransitioning || driveModePanel == null || !driveModePanel.activeSelf) return;
        PlayButtonSound(navigationSound);
        driveModePanel.SetActive(false);
        courseSelectionPanel.SetActive(true);
        if (courseSelectionPanel.TryGetComponent<CanvasGroup>(out var group)) group.interactable = true;
    }

    private void ShowTimeOfDay()
    {
        timeOfDayPanel.SetActive(true);
        if (timeOfDayPanel.TryGetComponent<CanvasGroup>(out var group)) group.interactable = true;
    }

    public void BackToDriveModes()
    {
        if (isTransitioning || (!TimeMenuOpen && !ChallengeMenuOpen)) return;
        PlayButtonSound(navigationSound);
        if (timeOfDayPanel != null) timeOfDayPanel.SetActive(false);
        if (challengePanel != null) challengePanel.SetActive(false);
        ShowDriveModes();
    }

    private void ShowDriveModes()
    {
        driveModePanel.SetActive(true);
        if (driveModePanel.TryGetComponent<CanvasGroup>(out var group)) group.interactable = true;
        string course = freeRideCourse == TestDriveSelection.CourseKind.LakeTown ? "Lake Town"
            : freeRideCourse == TestDriveSelection.CourseKind.MountainPass ? "Mountain Pass"
            : freeRideCourse == TestDriveSelection.CourseKind.HarborStreet ? "Harbor Street" : "Test Circuit";
        var freeRunDescription = driveModePanel.transform.Find("Dialog/FreeRunButton/Description")?.GetComponent<TMP_Text>();
        if (freeRunDescription != null) freeRunDescription.text = "Explore " + course + "\nin day or night.";
        bool hasChallenges = freeRideCourse == TestDriveSelection.CourseKind.TestCircuit;
        var challenge = driveModePanel.transform.Find("Dialog/ChallengeButton")?.GetComponent<UnityEngine.UI.Button>();
        if (challenge != null)
        {
            challenge.interactable = hasChallenges;
            var description = challenge.transform.Find("Description")?.GetComponent<TMP_Text>();
            if (description != null) description.text = hasChallenges
                ? "Choose a challenge and\ntest your driving skills."
                : "Challenges for this course\nare coming later.";
        }
    }

    public void TestCircuitRide() => SelectFreeRideCourse(TestDriveSelection.CourseKind.TestCircuit);
    public void LakeTownRide() => SelectFreeRideCourse(TestDriveSelection.CourseKind.LakeTown);
    public void MountainPassRide() => SelectFreeRideCourse(TestDriveSelection.CourseKind.MountainPass);
    public void HarborStreetRide() => SelectFreeRideCourse(TestDriveSelection.CourseKind.HarborStreet);
    private void SelectFreeRideCourse(TestDriveSelection.CourseKind course)
    {
        if (!CourseMenuOpen || isTransitioning) return;
        freeRideCourse = course;
        PlayButtonSound(navigationSound);
    }
    public void StartFreeRide()
    {
        FreeRun();
    }
    public void DayDrive() => SelectTimeOfDay(TestDriveSelection.TimeOfDay.Day);
    public void NightDrive() => SelectTimeOfDay(TestDriveSelection.TimeOfDay.Night);
    private void SelectTimeOfDay(TestDriveSelection.TimeOfDay lighting)
    {
        if (isTransitioning || !TimeMenuOpen) return;
        freeRideLighting = lighting;
        BeginDrive(TestDriveSelection.DriveMode.FreeRun, lighting, course: freeRideCourse);
    }
    public void Challenge()
    {
        if (isTransitioning || driveModePanel == null || !driveModePanel.activeSelf ||
            freeRideCourse != TestDriveSelection.CourseKind.TestCircuit || challengePanel == null) return;
        PlayButtonSound(navigationSound);
        driveModePanel.SetActive(false);
        var dialog = challengePanel.transform.Find("Dialog");
        if (dialog != null) dialog.gameObject.SetActive(true);
        challengePanel.SetActive(true);
        if (challengePanel.TryGetComponent<CanvasGroup>(out var group)) group.interactable = true;
    }
    public void CleanLap() { if (ChallengeMenuOpen) BeginDrive(TestDriveSelection.DriveMode.Challenge); }
    public void OffroadTrail() { if (ChallengeMenuOpen) BeginDrive(TestDriveSelection.DriveMode.Challenge,
        TestDriveSelection.TimeOfDay.Day, TestDriveSelection.ChallengeKind.OffroadTrail); }

    public void NightLap() { if (ChallengeMenuOpen) BeginDrive(TestDriveSelection.DriveMode.Challenge,
        TestDriveSelection.TimeOfDay.Night, TestDriveSelection.ChallengeKind.NightLap); }

    private void BeginDrive(TestDriveSelection.DriveMode mode, TestDriveSelection.TimeOfDay lighting = TestDriveSelection.TimeOfDay.Day, TestDriveSelection.ChallengeKind challenge = TestDriveSelection.ChallengeKind.CleanLap, TestDriveSelection.CourseKind course = TestDriveSelection.CourseKind.TestCircuit)
    {
        if (isTransitioning || !ModeMenuOpen) return;
        if (testDriveCatalog == null || currentVehicleIndex >= testDriveCatalog.drivingPrefabs.Length ||
            testDriveCatalog.drivingPrefabs[currentVehicleIndex] == null)
        {
            Debug.LogError("No driving prefab is configured for this vehicle.", this);
            return;
        }
        TestDriveSelection.Select(currentVehicleIndex, mode, lighting, challenge, course, currentVehicle.DisplayName);
        string scene = TestDriveSelection.Course == TestDriveSelection.CourseKind.HarborStreet ? "HarborStreetScene"
            : TestDriveSelection.Course == TestDriveSelection.CourseKind.MountainPass ? "MountainPassScene"
            : TestDriveSelection.Course == TestDriveSelection.CourseKind.LakeTown ? "LakeTownScene" : "TestTrackScene";
        BeginTransition(scene, driveStartSound != null ? driveStartSound : backSound);
    }

    private void BeginTransition(string sceneName, AudioClip transitionSound)
    {
        isTransitioning = true;
        if (courseSelectionPanel != null && courseSelectionPanel.TryGetComponent<CanvasGroup>(out var courseGroup)) courseGroup.interactable = false;
        if (challengePanel != null && challengePanel.TryGetComponent<CanvasGroup>(out var challengeGroup))
            challengeGroup.interactable = false;
        if (timeOfDayPanel != null && timeOfDayPanel.TryGetComponent<CanvasGroup>(out var timeGroup))
            timeGroup.interactable = false;
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
        if (testDriveCatalog != null && testDriveCatalog.drivingPrefabs != null &&
            currentVehicleIndex < testDriveCatalog.drivingPrefabs.Length &&
            testDriveCatalog.drivingPrefabs[currentVehicleIndex] != null)
        {
            float targetSpeed = testDriveCatalog.drivingPrefabs[currentVehicleIndex].TargetTopSpeedKmh;
            descriptionText.text += $"\nTOP SPEED: {Mathf.RoundToInt(targetSpeed)} km/h";
        }
        if (layoutText != null)
            layoutText.text = $"LAYOUT: {currentVehicle.LayoutCode}";
        vehicleCounterText.text = $"{currentVehicleIndex + 1} / {vehiclePrefabs.Length}";
    }

}
