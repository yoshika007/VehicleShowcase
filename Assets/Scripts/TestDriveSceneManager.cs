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

    private TestDriveMovement vehicle;
    private bool isReturning;

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
        if (TestDriveSelection.Mode == TestDriveSelection.DriveMode.Challenge
            && TestDriveSelection.SelectedChallenge == TestDriveSelection.ChallengeKind.HillClimb)
        {
            var hill = new GameObject("Hill Climb Course").AddComponent<HillClimbCourse>();
            hill.transform.SetParent(transform, false);
            hill.Initialize();
            selectedSpawn = hill.Spawn;
        }
        vehicle = Instantiate(catalog.drivingPrefabs[index], selectedSpawn.position, selectedSpawn.rotation);
        vehicle.ConfigureReset(selectedSpawn, followCamera);
        if (touchControls != null) touchControls.BindVehicle(vehicle);
        float framingScale = Mathf.Max(1f, vehicle.GetComponent<BoxCollider>().size.z / 4.74f);
        followCamera.SetTarget(vehicle.transform, framingScale);
        speedDisplay.SetVehicle(vehicle.GetComponent<Rigidbody>());
        minimap.SetVehicle(vehicle.transform);
        if (cleanLapChallenge != null)
        {
            bool challengeMode = TestDriveSelection.Mode == TestDriveSelection.DriveMode.Challenge;
            cleanLapChallenge.ConfigureMode(challengeMode, vehicle);
            if (challengeMode)
            {
                var briefing = new GameObject("CleanLapBriefing", typeof(RectTransform)).AddComponent<CleanLapBriefing>();
                briefing.Initialize(cleanLapChallenge, vehicle, this);
            }
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            ReturnToShowroom();
    }

    public void ReturnToShowroom()
    {
        if (isReturning) return;
        isReturning = true;
        if (cleanLapChallenge != null) cleanLapChallenge.BindVehicle(null);
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
