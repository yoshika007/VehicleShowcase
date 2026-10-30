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
    public bool CanDriveHighlandClimb => currentVehicle != null && (currentVehicle.Category == "Off-Road" || currentVehicle.Category == "Military");
    private bool CourseMenuOpen => courseSelectionPanel != null && courseSelectionPanel.activeSelf;
    private bool ChallengeMenuOpen => challengePanel != null && challengePanel.activeSelf;
    private bool[] navigationAvailability;
    private bool dragWasEnabled;
    private bool TimeMenuOpen => timeOfDayPanel != null && timeOfDayPanel.activeSelf;
    private bool ModeMenuOpen => (driveModePanel != null && driveModePanel.activeSelf) || TimeMenuOpen || ChallengeMenuOpen || CourseMenuOpen;

    private Vector2 nightChallengePosition;
    private bool challengePositionsSaved;
    private bool isTransitioning;
    private int currentVehicleIndex;
    private Vehicle currentVehicle;
    private VehicleCapabilityBars capabilityBars;
    private RectTransform infoPanel, nextButton, driveButton, showroomCanvas;
    private Vector2 infoBasePosition, driveBasePosition;
    private Vector3 infoBaseScale;
    private Canvas showroomRootCanvas;
    private readonly Vector3[] showroomCorners = new Vector3[4];

    private void Awake()
    {
        var preview = GameObject.Find("Showroom Edit Preview");
        if (preview != null) preview.SetActive(false);
        var swatches = GameObject.Find("Edit Preview Swatches");
        if (swatches != null) swatches.SetActive(false);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (driveModePanel != null) driveModePanel.SetActive(false);
        if (timeOfDayPanel != null) timeOfDayPanel.SetActive(false);
        if (challengePanel != null) challengePanel.SetActive(false);
        if (courseSelectionPanel != null) courseSelectionPanel.SetActive(false);
        currentVehicleIndex = Mathf.Clamp(TestDriveSelection.VehicleIndex, 0, vehiclePrefabs.Length - 1);
        ShowVehicle();
        InitializeShowroomLayout();
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

    private void InitializeShowroomLayout()
    {
        infoPanel = descriptionText != null ? descriptionText.transform.parent as RectTransform : null;
        nextButton = GameObject.Find("NextButton")?.GetComponent<RectTransform>();
        driveButton = GameObject.Find("DriveButton")?.GetComponent<RectTransform>();
        showroomRootCanvas = infoPanel != null ? infoPanel.GetComponentInParent<Canvas>()?.rootCanvas : null;
        showroomCanvas = showroomRootCanvas != null ? showroomRootCanvas.transform as RectTransform : null;
        if (infoPanel == null || nextButton == null || driveButton == null || showroomCanvas == null) return;
        infoBasePosition = infoPanel.anchoredPosition;
        driveBasePosition = driveButton.anchoredPosition;
        infoBaseScale = infoPanel.localScale;
        UpdateShowroomLayout();
    }

    private void LateUpdate() => UpdateShowroomLayout();

    private void UpdateShowroomLayout()
    {
        if (infoPanel == null || showroomCanvas == null || nextButton == null || driveButton == null) return;
        Rect viewport = showroomRootCanvas.pixelRect;
        Rect safe = Screen.safeArea;
        if (viewport.width <= 0 || viewport.height <= 0 || safe.width <= 0 || safe.height <= 0) return;
        // The Simulator can report a viewport different from Screen.width during rotation.
        if (safe.xMin < viewport.xMin || safe.yMin < viewport.yMin || safe.xMax > viewport.xMax || safe.yMax > viewport.yMax) return;
        nextButton.GetWorldCorners(showroomCorners);
        float nextTop = showroomCanvas.InverseTransformPoint(showroomCorners[1]).y;
        ApplyShowroomLayout(viewport, safe, showroomCanvas.rect, nextTop);
    }

    private void ApplyShowroomLayout(Rect viewport, Rect safe, Rect root, float nextTop)
    {
        float safeRight = root.xMin + root.width * ((safe.xMax - viewport.xMin) / viewport.width);
        float safeTop = root.yMin + root.height * ((safe.yMax - viewport.yMin) / viewport.height);
        const float margin = 12f;
        float maxScale = Mathf.Max(.01f, (safeTop - margin - nextTop - margin) / infoPanel.rect.height);
        float scale = Mathf.Min(infoBaseScale.x, maxScale);
        float halfHeight = infoPanel.rect.height * scale * .5f;
        float baseCenter = root.center.y + infoBasePosition.y;
        float center = Mathf.Clamp(Mathf.Max(baseCenter, nextTop + margin + halfHeight),
            nextTop + margin + halfHeight, safeTop - margin - halfHeight);
        float baseRight = root.xMax + infoBasePosition.x;
        float right = Mathf.Min(baseRight, safeRight - margin);
        float baseBottom = baseCenter - infoPanel.rect.height * infoBaseScale.y * .5f;
        float bottom = center - halfHeight;
        infoPanel.localScale = new Vector3(scale, scale, infoBaseScale.z);
        infoPanel.anchoredPosition = infoBasePosition + new Vector2(right - baseRight, center - baseCenter);
        Vector2 drivePosition = driveBasePosition + new Vector2(right - baseRight, bottom - baseBottom);
        if (driveButton.TryGetComponent<TestDriveButtonFeedback>(out var feedback)) feedback.SetRestingPosition(drivePosition);
        else driveButton.anchoredPosition = drivePosition;
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
        if (freeRideCourse == TestDriveSelection.CourseKind.HighlandClimb && !CanDriveHighlandClimb)
            freeRideCourse = TestDriveSelection.CourseKind.TestCircuit;
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
        if (isTransitioning || !CourseMenuOpen || driveModePanel == null ||
            (freeRideCourse == TestDriveSelection.CourseKind.HighlandClimb && !CanDriveHighlandClimb)) return;
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
        string course = freeRideCourse == TestDriveSelection.CourseKind.HighlandClimb ? "Highland Climb"
            : freeRideCourse == TestDriveSelection.CourseKind.LakeTown ? "Lake Town"
            : freeRideCourse == TestDriveSelection.CourseKind.MountainPass ? "Mountain Pass"
            : freeRideCourse == TestDriveSelection.CourseKind.HarborStreet ? "Harbor Street"
            : freeRideCourse == TestDriveSelection.CourseKind.PineRidge ? "Pine Ridge Trail" : "Test Circuit";
        var freeRunDescription = driveModePanel.transform.Find("Dialog/FreeRunButton/Description")?.GetComponent<TMP_Text>();
        if (freeRunDescription != null) freeRunDescription.text = "Explore " + course + "\nin day or night.";
        bool hasChallenges = freeRideCourse == TestDriveSelection.CourseKind.TestCircuit || freeRideCourse == TestDriveSelection.CourseKind.LakeTown || freeRideCourse == TestDriveSelection.CourseKind.MountainPass || freeRideCourse == TestDriveSelection.CourseKind.PineRidge || freeRideCourse == TestDriveSelection.CourseKind.HarborStreet || freeRideCourse == TestDriveSelection.CourseKind.HighlandClimb;
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
    public void PineRidgeRide() => SelectFreeRideCourse(TestDriveSelection.CourseKind.PineRidge);
    public void HighlandClimbRide() { if (CanDriveHighlandClimb) SelectFreeRideCourse(TestDriveSelection.CourseKind.HighlandClimb); }
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
            (freeRideCourse != TestDriveSelection.CourseKind.TestCircuit && freeRideCourse != TestDriveSelection.CourseKind.LakeTown && freeRideCourse != TestDriveSelection.CourseKind.MountainPass && freeRideCourse != TestDriveSelection.CourseKind.PineRidge && freeRideCourse != TestDriveSelection.CourseKind.HarborStreet && freeRideCourse != TestDriveSelection.CourseKind.HighlandClimb) || challengePanel == null) return;
        PlayButtonSound(navigationSound);
        driveModePanel.SetActive(false);
        var dialog = challengePanel.transform.Find("Dialog");
        if (dialog != null) dialog.gameObject.SetActive(true);
        challengePanel.SetActive(true);
        bool lake = freeRideCourse == TestDriveSelection.CourseKind.LakeTown;
        bool highland = freeRideCourse == TestDriveSelection.CourseKind.HighlandClimb;
        bool harbor = freeRideCourse == TestDriveSelection.CourseKind.HarborStreet;
        bool pine = freeRideCourse == TestDriveSelection.CourseKind.PineRidge;
        bool mountain = freeRideCourse == TestDriveSelection.CourseKind.MountainPass || pine || harbor || highland;
        var third = dialog.Find("OffroadTrailButton");
        var night = dialog.Find("NightLapButton") as RectTransform;
        if (night != null && !challengePositionsSaved) { nightChallengePosition = night.anchoredPosition; challengePositionsSaved = true; }
        if (third != null) third.gameObject.SetActive(!mountain || harbor);
        if (night != null) night.anchoredPosition = mountain && !harbor && third != null ? ((RectTransform)third).anchoredPosition : nightChallengePosition;
        SetChallengeLabel("CleanLapButton", lake ? "DAY CHECKPOINTS" : mountain ? "DAY CLEAR LAP" : "CLEAN LAP", lake ? "Follow the lakeside route by day." : highland ? "Complete the highland loop by day." : harbor ? "Complete the harbor loop by day." : pine ? "Complete the forest loop by day." : mountain ? "Complete the mountain loop by day." : "One lap. Avoid the barriers.");
        SetChallengeLabel("OffroadTrailButton", (lake || harbor) ? "PARKING" : "OFF-ROAD TRAIL", harbor ? "Find and park within 25 seconds." : lake ? "Find the marked bay and park." : "Dirt hills. Gravel turns. Eight gates.");
        SetChallengeLabel("NightLapButton", lake ? "NIGHT CHECKPOINTS" : mountain ? "NIGHT CLEAR LAP" : "NIGHT LAP", lake ? "Follow the lakeside route after dark." : highland ? "Complete the highland loop at night." : harbor ? "Complete the harbor loop at night." : pine ? "Complete the forest loop at night." : mountain ? "Complete the mountain loop at night." : "One clean lap. After dark.");
        if (challengePanel.TryGetComponent<CanvasGroup>(out var group)) group.interactable = true;
    }
    private void SetChallengeLabel(string button, string title, string description)
    {
        var root = challengePanel.transform.Find("Dialog/" + button);
        if (root == null) return;
        var heading = root.Find("Title")?.GetComponent<TMP_Text>();
        var detail = root.Find("Description")?.GetComponent<TMP_Text>();
        if (heading != null) heading.text = title;
        if (detail != null) detail.text = description;
    }
    public void CleanLap() { if (ChallengeMenuOpen) BeginDrive(TestDriveSelection.DriveMode.Challenge,
        challenge: freeRideCourse == TestDriveSelection.CourseKind.LakeTown ? TestDriveSelection.ChallengeKind.LakeDay : freeRideCourse == TestDriveSelection.CourseKind.MountainPass ? TestDriveSelection.ChallengeKind.MountainDay : freeRideCourse == TestDriveSelection.CourseKind.PineRidge ? TestDriveSelection.ChallengeKind.PineDay : freeRideCourse == TestDriveSelection.CourseKind.HarborStreet ? TestDriveSelection.ChallengeKind.HarborDay : freeRideCourse == TestDriveSelection.CourseKind.HighlandClimb ? TestDriveSelection.ChallengeKind.HighlandDay : TestDriveSelection.ChallengeKind.CleanLap, course: freeRideCourse); }
    public void OffroadTrail() { if (ChallengeMenuOpen) BeginDrive(TestDriveSelection.DriveMode.Challenge,
        TestDriveSelection.TimeOfDay.Day, freeRideCourse == TestDriveSelection.CourseKind.LakeTown ? TestDriveSelection.ChallengeKind.LakeParking : freeRideCourse == TestDriveSelection.CourseKind.HarborStreet ? TestDriveSelection.ChallengeKind.HarborParking : TestDriveSelection.ChallengeKind.OffroadTrail, freeRideCourse); }

    public void NightLap() { if (ChallengeMenuOpen) BeginDrive(TestDriveSelection.DriveMode.Challenge,
        TestDriveSelection.TimeOfDay.Night, freeRideCourse == TestDriveSelection.CourseKind.LakeTown ? TestDriveSelection.ChallengeKind.LakeNight : freeRideCourse == TestDriveSelection.CourseKind.MountainPass ? TestDriveSelection.ChallengeKind.MountainNight : freeRideCourse == TestDriveSelection.CourseKind.PineRidge ? TestDriveSelection.ChallengeKind.PineNight : freeRideCourse == TestDriveSelection.CourseKind.HarborStreet ? TestDriveSelection.ChallengeKind.HarborNight : freeRideCourse == TestDriveSelection.CourseKind.HighlandClimb ? TestDriveSelection.ChallengeKind.HighlandNight : TestDriveSelection.ChallengeKind.NightLap, freeRideCourse); }

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
        string scene = TestDriveSelection.Course == TestDriveSelection.CourseKind.HighlandClimb ? "HighlandClimbScene"
            : TestDriveSelection.Course == TestDriveSelection.CourseKind.PineRidge ? "PineRidgeScene"
            : TestDriveSelection.Course == TestDriveSelection.CourseKind.HarborStreet ? "HarborStreetScene"
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
        if (capabilityBars == null && descriptionText != null)
        {
            var existing = descriptionText.transform.parent.Find("VehicleCapabilityBars");
            capabilityBars = existing != null ? existing.GetComponent<VehicleCapabilityBars>() : null;
            if (capabilityBars == null)
                capabilityBars = new GameObject("VehicleCapabilityBars", typeof(RectTransform)).AddComponent<VehicleCapabilityBars>();
            capabilityBars.Initialize(descriptionText);
        }
        float? topSpeed = null;
        if (testDriveCatalog != null && testDriveCatalog.drivingPrefabs != null &&
            currentVehicleIndex < testDriveCatalog.drivingPrefabs.Length &&
            testDriveCatalog.drivingPrefabs[currentVehicleIndex] != null)
            topSpeed = testDriveCatalog.drivingPrefabs[currentVehicleIndex].TargetTopSpeedKmh;
        if (capabilityBars != null) capabilityBars.Show(currentVehicle, topSpeed);
        if (layoutText != null)
            layoutText.text = $"LAYOUT: {currentVehicle.LayoutCode}";
        vehicleCounterText.text = $"{currentVehicleIndex + 1} / {vehiclePrefabs.Length}";
    }

}
