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
        vehicle = Instantiate(catalog.drivingPrefabs[index], spawnPoint.position, spawnPoint.rotation);
        vehicle.ConfigureReset(spawnPoint, followCamera);
        float framingScale = Mathf.Max(1f, vehicle.GetComponent<BoxCollider>().size.z / 4.74f);
        followCamera.SetTarget(vehicle.transform, framingScale);
        speedDisplay.SetVehicle(vehicle.GetComponent<Rigidbody>());
        minimap.SetVehicle(vehicle.transform);
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
        if (returnButton != null) returnButton.interactable = false;
        if (vehicle != null) vehicle.enabled = false;
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
