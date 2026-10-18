using TMPro;
using UnityEngine;

public class TestDriveSpeedDisplay : MonoBehaviour
{
    [SerializeField] private Rigidbody vehicleBody;
    [SerializeField] private TMP_Text speedText;

    private int displayedSpeed = -1;

    private void OnEnable()
    {
        displayedSpeed = -1;
        UpdateDisplay();
    }

    private void LateUpdate()
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (speedText == null) return;
        float speedKmh = vehicleBody != null ? vehicleBody.linearVelocity.magnitude * 3.6f : 0f;
        int roundedSpeed = Mathf.RoundToInt(speedKmh);
        if (roundedSpeed == displayedSpeed) return;

        displayedSpeed = roundedSpeed;
        speedText.SetText("{0}", roundedSpeed);
    }
}
