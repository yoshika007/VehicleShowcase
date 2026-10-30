using UnityEngine;

public class TestDriveCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 followOffset = new Vector3(0f, 3.6f, -7.5f);
    [SerializeField] private Vector3 lookOffset = new Vector3(0f, 0.7f, 5f);
    [SerializeField, Min(0.01f)] private float followSharpness = 10f;
    [SerializeField, Min(0.01f)] private float lookSharpness = 12f;

    [SerializeField] private UnityEngine.UI.Button viewButton;
    [SerializeField] private TMPro.TMP_Text viewLabel;
    [SerializeField] private AudioSource buttonAudio;
    [SerializeField] private AudioClip switchSound;
    [SerializeField, Range(40f, 100f)] private float driverFieldOfView = 70f;
    private Transform driverView;
    private TestDriveDriverView driverProfile;
    private Camera viewCamera;
    private float chaseFieldOfView, chaseNearClip;
    private float framingScale = 1f;
    private bool canSwitchView = true;
    private int viewMode; // 0: chase, 1: in-car, 2: unobstructed in-car
    public bool IsDriverView => viewMode != 0;

    private void Initialize()
    {
        if (viewCamera != null) return;
        viewCamera = GetComponent<Camera>();
        if (viewCamera == null) return;
        chaseFieldOfView = viewCamera.fieldOfView;
        chaseNearClip = viewCamera.nearClipPlane;
    }

    public void ToggleView()
    {
        if (!canSwitchView || target == null || driverView == null) return;
        viewMode = (viewMode + 1) % 3;
        if (buttonAudio != null && switchSound != null) buttonAudio.PlayOneShot(switchSound);
        RefreshView();
        SnapToTarget();
    }

    public void DisableViewSwitching()
    {
        canSwitchView = false;
        if (viewButton != null) viewButton.interactable = false;
    }

    private void RefreshView()
    {
        Initialize();
        if (viewCamera != null)
        {
            viewCamera.fieldOfView = IsDriverView ? driverFieldOfView : chaseFieldOfView;
            viewCamera.nearClipPlane = IsDriverView ? .03f : chaseNearClip;
        }
        if (driverProfile != null) driverProfile.SetView(IsDriverView, viewMode == 2);
        if (viewLabel != null) viewLabel.text = "CHANGE VIEW";
        if (viewButton != null) viewButton.interactable = canSwitchView && target != null && driverView != null;
    }

    public void SetTarget(Transform vehicle, float scale = 1f)
    {
        if (driverProfile != null) driverProfile.SetView(false, false);
        target = vehicle;
        driverView = target != null ? target.Find("DriverView") : null;
        driverProfile = driverView != null ? driverView.GetComponent<TestDriveDriverView>() : null;
        viewMode = 0;
        canSwitchView = target != null;
        framingScale = scale;
        RefreshView();
        SnapToTarget();
    }

    public void SnapToTarget()
    {
        if (target == null) return;
        if (IsDriverView && driverView != null)
        {
            SetCenteredDriverView();
            return;
        }
        Vector3 position = target.position + Quaternion.Euler(0f, target.eulerAngles.y, 0f) * (followOffset * framingScale);
        Vector3 lookPoint = target.position + Quaternion.Euler(0f, target.eulerAngles.y, 0f) * (lookOffset * framingScale);
        transform.SetPositionAndRotation(position, Quaternion.LookRotation(lookPoint - position, Vector3.up));
    }

    private void SetCenteredDriverView()
    {
        Vector3 localPosition = target.InverseTransformPoint(driverView.position);
        localPosition.x = 0f;
        transform.SetPositionAndRotation(target.TransformPoint(localPosition), driverView.rotation);
    }

    private void OnEnable()
    {
        if (target != null) RefreshView();
    }

    private void OnDisable()
    {
        if (driverProfile != null) driverProfile.SetView(false, false);
    }

    private void LateUpdate()
    {
        if (target == null) return;
        if (IsDriverView && driverView != null)
        {
            // Follow the vehicle through bumps and turns while staying centered.
            SetCenteredDriverView();
            return;
        }

        // Rotate the offset with the car so the camera stays behind it through turns.
        Vector3 desiredPosition = target.position + Quaternion.Euler(0f, target.eulerAngles.y, 0f) * (followOffset * framingScale);
        float followBlend = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followBlend);

        Vector3 lookPoint = target.position + Quaternion.Euler(0f, target.eulerAngles.y, 0f) * (lookOffset * framingScale);
        Vector3 lookDirection = lookPoint - transform.position;
        if (lookDirection.sqrMagnitude < 0.0001f) return;

        Quaternion desiredRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        float lookBlend = 1f - Mathf.Exp(-lookSharpness * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, lookBlend);
    }
}
