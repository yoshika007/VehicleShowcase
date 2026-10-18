using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class TestDriveMovement : MonoBehaviour
{
    [System.Serializable]
    private class Wheel
    {
        public WheelCollider collider;
        public Transform visual;
        public bool steering;
        public bool driven;
        [System.NonSerialized] public Quaternion visualRotationOffset;
    }

    [Header("Wheel setup")]
    [SerializeField] private Wheel[] wheels = new Wheel[0];

    [Header("Engine and brakes")]
    [SerializeField, Min(0f)] private float moveSpeed = 8f;
    [SerializeField, Min(0f)] private float reverseSpeed = 4f;
    [SerializeField, Min(0f)] private float motorTorque = 1600f;
    [SerializeField, Min(0f)] private float torqueResponse = 2500f;
    [SerializeField, Min(0f)] private float brakeTorque = 1800f;
    [SerializeField, Min(0f)] private float coastingBrakeTorque = 80f;

    [Header("Steering")]
    [SerializeField, Range(0f, 45f)] private float maximumSteerAngle = 30f;
    [SerializeField, Range(0f, 45f)] private float highSpeedSteerAngle = 12f;
    [SerializeField, Min(0f)] private float steeringResponse = 90f;

    [Header("Reset")]
    [SerializeField] private Transform resetPoint;
    [SerializeField] private TestDriveCameraFollow followCamera;

    private Rigidbody vehicleBody;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private float moveInput;
    private float steerInput;
    private float currentSteerAngle;
    private float currentMotorTorque;
    private int drivenWheelCount;
    private bool brakeInput;
    private bool resetRequested;

    private void Awake()
    {
        vehicleBody = GetComponent<Rigidbody>();
        initialPosition = vehicleBody.position;
        initialRotation = vehicleBody.rotation;
        foreach (var wheel in wheels)
        {
            if (wheel.collider == null) continue;
            if (wheel.driven) drivenWheelCount++;
            if (wheel.visual != null)
                wheel.visualRotationOffset = Quaternion.Inverse(wheel.collider.transform.rotation) * wheel.visual.rotation;
            wheel.collider.ConfigureVehicleSubsteps(5f, 12, 15);
        }
    }

    private void Update()
    {
        moveInput = 0f;
        steerInput = 0f;
        brakeInput = false;
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.rKey.wasPressedThisFrame) resetRequested = true;
        bool forward = keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed;
        bool reverse = keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed;
        moveInput = (forward ? 1f : 0f) - (reverse ? 1f : 0f);
        brakeInput = keyboard.spaceKey.isPressed || (forward && reverse);
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) steerInput -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) steerInput += 1f;
    }

    private void FixedUpdate()
    {
        if (resetRequested)
        {
            ResetVehicle();
            return;
        }

        float speed = Vector3.Dot(vehicleBody.linearVelocity, transform.forward);
        bool changingDirection = speed * moveInput < -0.4f;
        bool braking = brakeInput || changingDirection;
        float speedLimit = moveInput > 0f ? moveSpeed : reverseSpeed;
        float speedInRequestedDirection = speed * moveInput;
        // Ease engine torque near the limit; never clamp the Rigidbody's velocity.
        float availableTorque = Mathf.Clamp01((speedLimit - speedInRequestedDirection) / 2f);
        float requestedTorque = !braking && moveInput != 0f ? moveInput * motorTorque * availableTorque : 0f;
        currentMotorTorque = braking || speedInRequestedDirection >= speedLimit
            ? 0f
            : Mathf.MoveTowards(currentMotorTorque, requestedTorque, torqueResponse * Time.fixedDeltaTime);

        float appliedBrake = braking ? brakeTorque : (moveInput == 0f ? coastingBrakeTorque : 0f);
        // Hold at rest when no pedal is pressed, instead of slowly creeping.
        if (moveInput == 0f && Mathf.Abs(speed) < 0.15f) appliedBrake = brakeTorque;

        float speedRatio = Mathf.Clamp01(Mathf.Abs(speed) / Mathf.Max(0.01f, moveSpeed));
        float steerLimit = Mathf.Lerp(maximumSteerAngle, highSpeedSteerAngle, speedRatio);
        currentSteerAngle = Mathf.MoveTowards(currentSteerAngle, steerInput * steerLimit, steeringResponse * Time.fixedDeltaTime);

        foreach (var wheel in wheels)
        {
            if (wheel.collider == null) continue;
            wheel.collider.steerAngle = wheel.steering ? currentSteerAngle : 0f;
            wheel.collider.motorTorque = wheel.driven ? currentMotorTorque / Mathf.Max(1, drivenWheelCount) : 0f;
            wheel.collider.brakeTorque = appliedBrake;
        }
        // Wheel contact forces now drive and rotate the car; no velocity or rotation overrides.
    }

    private void LateUpdate()
    {
        foreach (var wheel in wheels)
        {
            if (wheel.collider == null || wheel.visual == null) continue;
            wheel.collider.GetWorldPose(out Vector3 position, out Quaternion rotation);
            wheel.visual.SetPositionAndRotation(position, rotation * wheel.visualRotationOffset);
        }
    }

    public void ConfigureReset(Transform spawnPoint, TestDriveCameraFollow cameraFollow)
    {
        resetPoint = spawnPoint;
        followCamera = cameraFollow;
    }

    public void ResetVehicle()
    {
        ClearControls();
        vehicleBody.linearVelocity = Vector3.zero;
        vehicleBody.angularVelocity = Vector3.zero;
        Vector3 position = resetPoint != null ? resetPoint.position : initialPosition;
        Quaternion rotation = resetPoint != null ? resetPoint.rotation : initialRotation;
        vehicleBody.position = position;
        vehicleBody.rotation = rotation;
        transform.SetPositionAndRotation(position, rotation);
        vehicleBody.WakeUp();
        if (followCamera != null) followCamera.SnapToTarget();
    }

    private void ClearControls()
    {
        moveInput = 0f;
        steerInput = 0f;
        brakeInput = false;
        resetRequested = false;
        currentSteerAngle = 0f;
        currentMotorTorque = 0f;
        foreach (var wheel in wheels)
        {
            if (wheel.collider == null) continue;
            wheel.collider.motorTorque = 0f;
            wheel.collider.brakeTorque = brakeTorque;
            wheel.collider.steerAngle = 0f;
        }
    }

    private void OnDisable()
    {
        ClearControls();
    }
}
