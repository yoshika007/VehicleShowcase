using UnityEngine;
using UnityEngine.EventSystems;

public class VehicleDragRotation : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private float degreesPerScreenWidth = 240f;

    [SerializeField] private float verticalDegreesPerScreenHeight = 40f;
    [SerializeField] private float minimumVerticalAngle = -2f;
    [SerializeField] private float maximumVerticalAngle = 6f;

    [Header("Showroom camera motion")]
    [SerializeField, Range(0f, 8f)] private float idleOrbitDegrees = 3f;
    [SerializeField, Min(4f)] private float idleOrbitPeriod = 22f;
    [SerializeField, Min(0f)] private float idleResumeDelay = 1.5f;

    private float idlePhase;
    private float idleYaw;
    private float idleMotionSpeed;
    private float resumeIdleAt;

    private Vector3 initialCameraPosition;
    private Quaternion initialCameraRotation;
    private Vector3 orbitCenter;
    private float verticalAngle;

    private Transform vehicle;
    private Camera viewCamera;
    private Vector3 pivot;
    private Bounds vehicleBounds;
    private int? activePointer;

    public void SetVehicle(Transform target)
    {
        activePointer = null;
        vehicle = target;
        var nextCamera = Camera.main;
        if (viewCamera != nextCamera)
        {
            viewCamera = nextCamera;
            if (viewCamera != null)
            {
                initialCameraPosition = viewCamera.transform.position;
                initialCameraRotation = viewCamera.transform.rotation;
                // Keep the original viewing direction and orbit around the display area.
                float distance = Vector3.Distance(initialCameraPosition, target.position + Vector3.up * 0.6f);
                orbitCenter = initialCameraPosition + viewCamera.transform.forward * distance;
            }
        }
        ResetCamera();
        var renderers = target.GetComponentsInChildren<Renderer>();
        vehicleBounds = new Bounds(target.position, Vector3.zero);
        if (renderers.Length > 0)
        {
            vehicleBounds = renderers[0].bounds;
            foreach (var renderer in renderers)
                vehicleBounds.Encapsulate(renderer.bounds);
        }
        pivot = new Vector3(vehicleBounds.center.x, target.position.y, vehicleBounds.center.z);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (activePointer.HasValue || vehicle == null || viewCamera == null ||
            eventData.button != PointerEventData.InputButton.Left)
            return;

        // The full-screen drag surface receives background gestures; foreground
        // buttons and panels keep their own EventSystem raycasts.
        activePointer = eventData.pointerId;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (vehicle == null || activePointer != eventData.pointerId) return;
        float angle = -eventData.delta.x / Mathf.Max(1, Screen.width) * degreesPerScreenWidth;
        vehicle.RotateAround(pivot, Vector3.up, angle);
        if (viewCamera == null) return;
        verticalAngle = Mathf.Clamp(verticalAngle + eventData.delta.y /
            Mathf.Max(1, Screen.height) * verticalDegreesPerScreenHeight,
            minimumVerticalAngle, maximumVerticalAngle);
        ApplyCameraPose();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (activePointer == eventData.pointerId)
        {
            activePointer = null;
            resumeIdleAt = Time.unscaledTime + idleResumeDelay;
        }
    }

    private void LateUpdate()
    {
        if (vehicle == null || viewCamera == null) return;
        // Freeze the orbit at its current pose while inspecting the car.
        if (activePointer.HasValue || Time.unscaledTime < resumeIdleAt)
        {
            idleMotionSpeed = 0f;
            return;
        }
        idleMotionSpeed = Mathf.MoveTowards(idleMotionSpeed, 1f, Time.unscaledDeltaTime);
        idlePhase = Mathf.Repeat(idlePhase + Time.unscaledDeltaTime * idleMotionSpeed *
            Mathf.PI * 2f / Mathf.Max(4f, idleOrbitPeriod), Mathf.PI * 2f);
        idleYaw = Mathf.Sin(idlePhase) * idleOrbitDegrees;
        ApplyCameraPose();
    }

    private void ApplyCameraPose()
    {
        var orbit = Quaternion.AngleAxis(idleYaw, Vector3.up) *
            Quaternion.AngleAxis(verticalAngle, initialCameraRotation * Vector3.right);
        viewCamera.transform.SetPositionAndRotation(
            orbitCenter + orbit * (initialCameraPosition - orbitCenter),
            orbit * initialCameraRotation);
    }

    private void ResetCamera()
    {
        verticalAngle = 0f;
        idlePhase = idleYaw = idleMotionSpeed = 0f;
        resumeIdleAt = Time.unscaledTime + idleResumeDelay;
        if (viewCamera != null)
            viewCamera.transform.SetPositionAndRotation(initialCameraPosition, initialCameraRotation);
    }

    private void OnDisable()
    {
        activePointer = null;
        ResetCamera();
    }
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            activePointer = null;
            resumeIdleAt = Time.unscaledTime + idleResumeDelay;
        }
    }
}
