using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class TestDriveSceneManager : MonoBehaviour
{
    [SerializeField] private TestDriveVehicleCatalog catalog;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private TestDriveCameraFollow followCamera;
    [SerializeField] private TestDriveSpeedDisplay speedDisplay;
    [SerializeField] private TestDriveMinimap minimap;
    [SerializeField] private UnityEngine.UI.Button returnButton;
    [SerializeField] private AudioSource buttonAudio;
    [SerializeField] private AudioClip returnSound;

    [SerializeField] private TestDriveTouchControls touchControls;

    [SerializeField] private TestDriveCleanLapChallenge cleanLapChallenge;

    private LakeTownChallenge lakeChallenge;
    private TestDriveMovement vehicle;
    private bool isReturning;
    private TestDriveIntro intro;

    private void Awake()
    {
        if (catalog == null || catalog.drivingPrefabs == null || catalog.drivingPrefabs.Length == 0 || spawnPoint == null)
        {
            Debug.LogError("The test-drive vehicle catalog or spawn point is missing.", this);
            return;
        }

        int index = Mathf.Clamp(TestDriveSelection.VehicleIndex, 0, catalog.drivingPrefabs.Length - 1);
        if (catalog.drivingPrefabs[index] == null)
        {
            Debug.LogError("The selected driving prefab is missing.", this);
            return;
        }
        Transform selectedSpawn = spawnPoint;
        if (TestDriveSelection.Mode == TestDriveSelection.DriveMode.Challenge
            && TestDriveSelection.SelectedChallenge == TestDriveSelection.ChallengeKind.OffroadTrail)
        {
            var trail = new GameObject("Off-road Trail Course").AddComponent<OffroadTrailCourse>();
            trail.transform.SetParent(transform, false);
            trail.Initialize();
            selectedSpawn = trail.Spawn;
        }
        vehicle = Instantiate(catalog.drivingPrefabs[index], selectedSpawn.position, selectedSpawn.rotation);
        vehicle.ConfigureReset(selectedSpawn, followCamera);
        if (touchControls != null) touchControls.BindVehicle(vehicle);
        float framingScale = Mathf.Max(1f, vehicle.GetComponent<BoxCollider>().size.z / 4.74f);
        followCamera.SetTarget(vehicle.transform, framingScale);
        speedDisplay.SetVehicle(vehicle.GetComponent<Rigidbody>());
        minimap.SetVehicle(vehicle.transform);
        // Cache challenge geometry in Awake, before Unity combines static meshes.
        // The briefing and countdown remain behind the cinematic handoff.
        if (cleanLapChallenge != null)
            cleanLapChallenge.ConfigureMode(TestDriveSelection.Mode == TestDriveSelection.DriveMode.Challenge, vehicle);
        if (TestDriveSelection.Mode == TestDriveSelection.DriveMode.Challenge && (TestDriveSelection.Course == TestDriveSelection.CourseKind.LakeTown || TestDriveSelection.Course == TestDriveSelection.CourseKind.MountainPass || TestDriveSelection.Course == TestDriveSelection.CourseKind.PineRidge || TestDriveSelection.Course == TestDriveSelection.CourseKind.HarborStreet))
        {
            lakeChallenge = GetComponent<LakeTownChallenge>();
            if (lakeChallenge == null) lakeChallenge = gameObject.AddComponent<LakeTownChallenge>();
            lakeChallenge.Initialize(vehicle, this);
        }
        if (TestDriveSelection.ConsumeIntroRequest() && followCamera != null)
        {
            intro = new GameObject("Drive Intro").AddComponent<TestDriveIntro>();
            intro.Initialize(vehicle, followCamera, touchControls, BeginDriving);
        }
        else BeginDriving();
    }

    private void BeginDriving()
    {
        if (isReturning) return;
        if (lakeChallenge != null) lakeChallenge.ShowBriefing();
        if (cleanLapChallenge != null && TestDriveSelection.Mode == TestDriveSelection.DriveMode.Challenge)
        {
            var briefing = new GameObject("CleanLapBriefing", typeof(RectTransform)).AddComponent<CleanLapBriefing>();
            briefing.Initialize(cleanLapChallenge, vehicle, this);
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (intro != null && intro.IsPlaying) intro.Skip();
            else ReturnToShowroom();
        }
    }

    public void ReturnToShowroom()
    {
        if (isReturning) return;
        isReturning = true;
        if (intro != null) intro.Cancel();
        if (followCamera != null) followCamera.DisableViewSwitching();
        if (cleanLapChallenge != null) cleanLapChallenge.BindVehicle(null);
        if (lakeChallenge != null) lakeChallenge.StopChallenge();
        if (returnButton != null) returnButton.interactable = false;
        if (touchControls != null) touchControls.BindVehicle(null);
        if (vehicle != null)
        {
            vehicle.SetChallengeStopped(true);
            vehicle.enabled = false;
        }
        StartCoroutine(ReturnAfterSound());
    }

    private IEnumerator ReturnAfterSound()
    {
        if (buttonAudio != null && returnSound != null)
        {
            buttonAudio.PlayOneShot(returnSound);
            yield return new WaitForSecondsRealtime(returnSound.length / Mathf.Max(0.01f, Mathf.Abs(buttonAudio.pitch)));
        }
        SceneManager.LoadScene("ShowcaseScene");
    }
}
