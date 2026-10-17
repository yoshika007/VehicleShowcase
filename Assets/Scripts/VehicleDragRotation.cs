using UnityEngine;
using UnityEngine.EventSystems;

public class VehicleDragRotation : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private float degreesPerScreenWidth = 240f;

    [SerializeField] private float verticalDegreesPerScreenHeight = 40f;
    [SerializeField] private float minimumVerticalAngle = -2f;
    [SerializeField] private float maximumVerticalAngle = 6f;

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

        // Begin only over the displayed car. Foreground UI receives its own pointer events.
        var renderers = vehicle.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        vehicleBounds = renderers[0].bounds;
        foreach (var renderer in renderers) vehicleBounds.Encapsulate(renderer.bounds);
        var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        for (int i = 0; i < 8; i++)
        {
            var corner = vehicleBounds.center + Vector3.Scale(vehicleBounds.extents,
                new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            Vector3 screen = viewCamera.WorldToScreenPoint(corner);
            if (screen.z <= 0) return;
            min = Vector2.Min(min, screen);
            max = Vector2.Max(max, screen);
        }
        if (Rect.MinMaxRect(min.x, min.y, max.x, max.y).Contains(eventData.pressPosition))
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
        var orbit = Quaternion.AngleAxis(verticalAngle, initialCameraRotation * Vector3.right);
        viewCamera.transform.SetPositionAndRotation(
            orbitCenter + orbit * (initialCameraPosition - orbitCenter),
            orbit * initialCameraRotation);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (activePointer == eventData.pointerId) activePointer = null;
    }

    private void ResetCamera()
    {
        verticalAngle = 0f;
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
        if (!hasFocus) activePointer = null;
    }
}
