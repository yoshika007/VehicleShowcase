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
        [System.NonSerialized] public WheelFrictionCurve forwardFriction;
        [System.NonSerialized] public WheelFrictionCurve sidewaysFriction;
        [System.NonSerialized] public float surfaceGrip = 1f;
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
    [Tooltip("Speed in m/s at which steering reaches its high-speed angle. Zero uses maximum forward speed.")]
    [SerializeField, Min(0f)] private float steeringFullReductionSpeed;
    [SerializeField, Min(0f)] private float steeringResponse = 90f;

    [Header("Reset")]
    [SerializeField] private Transform resetPoint;
    [SerializeField] private TestDriveCameraFollow followCamera;

    private TestDriveTouchControls touchControls;
    private Rigidbody vehicleBody;
    private BoxCollider chassisCollider;
    private int configuredWheelCount;
    private float bogAttemptTime;
    private Vector3 bogAttemptOrigin;
    private Vector3 boggedOrigin;
    private TestDriveSurface boggedSurface;
    private bool challengeInputLocked;
    private RigidbodyConstraints preChallengeConstraints;
    public bool IsChallengeStopped { get; private set; }
    public bool IsChallengeCoasting { get; private set; }
    public event System.Action<Collider> ColliderContact;
    public int ResetCount { get; private set; }
    public bool IsBraking { get; private set; }
    public bool IsReversing { get; private set; }
    public bool IsBoggedDown { get; private set; }
    public float BoggingAttemptSeconds => bogAttemptTime;
    public bool CanRecoverLocally => IsBoggedDown && boggedSurface != null && boggedSurface.RecoveryPoint != null;
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
        chassisCollider = GetComponent<BoxCollider>();
        initialPosition = vehicleBody.position;
        initialRotation = vehicleBody.rotation;
        foreach (var wheel in wheels)
        {
            if (wheel.collider == null) continue;
            configuredWheelCount++;
            if (wheel.driven) drivenWheelCount++;
            if (wheel.visual != null)
                wheel.visualRotationOffset = Quaternion.Inverse(wheel.collider.transform.rotation) * wheel.visual.rotation;
            wheel.forwardFriction = wheel.collider.forwardFriction;
            wheel.sidewaysFriction = wheel.collider.sidewaysFriction;
            wheel.collider.ConfigureVehicleSubsteps(5f, 12, 15);
        }
    }

    private void Update()
    {
        if (IsChallengeStopped)
        {
            ClearControls();
            return;
        }

        if (IsChallengeCoasting)
        {
            moveInput = steerInput = 0f;
            brakeInput = resetRequested = false;
            return;
        }

        moveInput = 0f;
        steerInput = 0f;
        brakeInput = false;
        var keyboard = Keyboard.current;
        bool forward = false;
        bool reverse = false;
        if (keyboard != null)
        {
            if (keyboard.rKey.wasPressedThisFrame) resetRequested = true;
            forward = keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed;
            reverse = keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed;
            brakeInput = keyboard.spaceKey.isPressed;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) steerInput -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) steerInput += 1f;
        }
        if (touchControls != null)
        {
            forward |= touchControls.Forward;
            reverse |= touchControls.Reverse;
            steerInput += touchControls.Steering;
        }
        moveInput = (forward ? 1f : 0f) - (reverse ? 1f : 0f);
        brakeInput |= forward && reverse;
        steerInput = Mathf.Clamp(steerInput, -1f, 1f);
    }

    private void FixedUpdate()
    {
        if (IsChallengeStopped)
        {
            ClearControls();
            vehicleBody.linearVelocity = Vector3.zero;
            vehicleBody.angularVelocity = Vector3.zero;
            return;
        }

        if (resetRequested)
        {
            RecoverOrResetVehicle();
            return;
        }

        if (challengeInputLocked) { moveInput = steerInput = 0f; brakeInput = false; }
        float speed = Vector3.Dot(vehicleBody.linearVelocity, transform.forward);
        bool changingDirection = speed * moveInput < -0.4f;
        bool braking = brakeInput || changingDirection;
        IsBraking = braking;
        // BRAKE / REV first slows forward travel, then selects reverse. Keep white
        // lamps on while coasting backward, with active braking taking priority.
        IsReversing = !braking && (moveInput < 0f || speed < -.15f);
        float speedLimit = moveInput > 0f ? moveSpeed : reverseSpeed;
        float speedInRequestedDirection = speed * moveInput;
        // Ease engine torque near the limit; never clamp the Rigidbody's velocity.
        float availableTorque = Mathf.Clamp01((speedLimit - speedInRequestedDirection) / 2f);
        float requestedTorque = !brakeInput && moveInput != 0f ? moveInput * motorTorque * availableTorque : 0f;
        // A direction change starts a fresh ramp in the requested direction.
        if (moveInput * currentMotorTorque < 0f) currentMotorTorque = 0f;
        // Keep building engine torque through automatic direction-change braking:
        // slight rollback on a hill must not reset the launch ramp every time.
        // Explicit braking and lifting off still cut power immediately.
        currentMotorTorque = brakeInput || moveInput == 0f || speedInRequestedDirection >= speedLimit
            ? 0f
            : Mathf.MoveTowards(currentMotorTorque, requestedTorque, torqueResponse * Time.fixedDeltaTime);

        float appliedBrake = braking ? brakeTorque : (moveInput == 0f ? coastingBrakeTorque : 0f);
        // Hold at rest when no pedal is pressed, instead of slowly creeping.
        if (moveInput == 0f && Mathf.Abs(speed) < 0.15f) appliedBrake = brakeTorque;

        float steeringReferenceSpeed = steeringFullReductionSpeed > 0f ? steeringFullReductionSpeed : moveSpeed;
        float speedRatio = Mathf.Clamp01(Mathf.Abs(speed) / Mathf.Max(0.01f, steeringReferenceSpeed));
        float steerLimit = Mathf.Lerp(maximumSteerAngle, highSpeedSteerAngle, speedRatio);
        currentSteerAngle = Mathf.MoveTowards(currentSteerAngle, steerInput * steerLimit, steeringResponse * Time.fixedDeltaTime);

        Vector3 surfaceResistance = Vector3.zero;
        int assignedWheelCount = 0;
        int mudContacts = 0;
        TestDriveSurface mudSurface = null;
        float chassisBottom = chassisCollider != null ? chassisCollider.bounds.min.y : float.PositiveInfinity;
        foreach (var wheel in wheels)
        {
            if (wheel.collider == null) continue;
            assignedWheelCount++;
            float grip = 1f;
            bool grounded = wheel.collider.GetGroundHit(out WheelHit hit);
            if (grounded && hit.collider.TryGetComponent<TestDriveSurface>(out var surface))
            {
                grip = surface.GripMultiplier;
                if (surface.SurfaceKind == TestDriveSurface.Kind.Mud)
                {
                    mudContacts++;
                    mudSurface = surface;
                }
                Vector3 planarVelocity = Vector3.ProjectOnPlane(vehicleBody.linearVelocity, hit.normal);
                float planarSpeed = planarVelocity.magnitude;
                if (planarSpeed > 0.001f)
                {
                    float resistance = surface.ResistanceAcceleration + surface.VelocityResistance * planarSpeed;
                    if (surface.SurfaceKind == TestDriveSurface.Kind.Mud && surface.MudBogResistance > 0f)
                    {
                        float depth = Mathf.Max(0f, surface.VisibleWetSurfaceHeight - hit.point.y);
                        float tireBurial = Mathf.InverseLerp(.65f, 1.15f, depth / Mathf.Max(.05f, wheel.collider.radius));
                        float chassisBurial = Mathf.InverseLerp(0f, .25f, surface.VisibleWetSurfaceHeight - chassisBottom);
                        float drivenFraction = drivenWheelCount / (float)Mathf.Max(1, configuredWheelCount);
                        float tractionHelp = Mathf.Lerp(1f, .35f, drivenFraction);
                        resistance += surface.MudBogResistance * Mathf.Max(tireBurial, chassisBurial * .7f) * tractionHelp;
                    }
                    // Limit resistance to this step's speed so it cannot reverse a slow vehicle.
                    resistance = Mathf.Min(resistance, planarSpeed / Time.fixedDeltaTime);
                    surfaceResistance -= planarVelocity / planarSpeed * resistance;
                }
            }
            if (!Mathf.Approximately(grip, wheel.surfaceGrip))
            {
                var forward = wheel.forwardFriction;
                var sideways = wheel.sidewaysFriction;
                forward.stiffness *= grip;
                sideways.stiffness *= grip;
                wheel.collider.forwardFriction = forward;
                wheel.collider.sidewaysFriction = sideways;
                wheel.surfaceGrip = grip;
            }
            wheel.collider.steerAngle = wheel.steering ? currentSteerAngle : 0f;
            wheel.collider.motorTorque = wheel.driven && !braking ? currentMotorTorque / Mathf.Max(1, drivenWheelCount) : 0f;
            float wheelBrake = appliedBrake;
            if (!braking && moveInput == 0f && wheel.driven && grounded)
            {
                // Loose surfaces can leave driven wheels spinning much faster than the car.
                // Shed that stored spin on lift-off instead of continuing to propel the body.
                Vector3 rollingDirection = Quaternion.AngleAxis(wheel.collider.steerAngle,
                    wheel.collider.transform.up) * wheel.collider.transform.forward;
                float roadSpeed = Mathf.Abs(Vector3.Dot(vehicleBody.GetPointVelocity(hit.point),
                    Vector3.ProjectOnPlane(rollingDirection, hit.normal).normalized));
                float radius = Mathf.Max(0.01f, wheel.collider.radius);
                float excessAngularSpeed = Mathf.Max(0f, Mathf.Abs(wheel.collider.rpm) * Mathf.PI / 30f
                    - (roadSpeed + 0.75f) / radius);
                float wheelInertia = 0.5f * wheel.collider.mass * radius * radius;
                // Bound the spin correction so it does not intentionally brake below road speed.
                float spinBrake = excessAngularSpeed * wheelInertia / Time.fixedDeltaTime;
                wheelBrake = Mathf.Max(wheelBrake, Mathf.Min(brakeTorque, spinBrake));
            }
            wheel.collider.brakeTorque = wheelBrake;
        }
        // One wheel entering mud contributes only its share; airborne and dry wheels add no drag.
        if (assignedWheelCount > 0 && surfaceResistance.sqrMagnitude > 0f)
            vehicleBody.AddForce(surfaceResistance / assignedWheelCount, ForceMode.Acceleration);
        UpdateBogging(mudContacts, mudSurface, braking);
        // Wheel contact forces drive and rotate the car; no velocity or rotation overrides.
    }

    private void UpdateBogging(int contacts, TestDriveSurface surface, bool braking)
    {
        Vector3 progress = vehicleBody.position - boggedOrigin;
        progress.y = 0f;
        if (contacts == 0 || (IsBoggedDown && progress.magnitude > 1f))
        {
            IsBoggedDown = false;
            boggedSurface = null;
            bogAttemptTime = 0f;
        }
        if (IsBoggedDown) return;
        // Idle, braking, dry-ground obstacles and normal slow progress are not bogging.
        if (contacts < 2 || Mathf.Abs(moveInput) < .5f || braking)
        {
            bogAttemptTime = 0f;
            return;
        }
        if (bogAttemptTime == 0f) bogAttemptOrigin = vehicleBody.position;
        Vector3 distance = vehicleBody.position - bogAttemptOrigin;
        distance.y = 0f;
        float speed = Vector3.ProjectOnPlane(vehicleBody.linearVelocity, Vector3.up).magnitude;
        if (distance.magnitude > .6f || speed > 1f)
        {
            bogAttemptTime = 0f;
            return;
        }
        bogAttemptTime += Time.fixedDeltaTime;
        if (bogAttemptTime >= 3.5f && speed < .35f)
        {
            IsBoggedDown = true;
            boggedSurface = surface;
            boggedOrigin = vehicleBody.position;
        }
    }

    private void OnCollisionEnter(Collision collision) => ColliderContact?.Invoke(collision.collider);
    private void OnCollisionStay(Collision collision) => ColliderContact?.Invoke(collision.collider);

    private void LateUpdate()
    {
        foreach (var wheel in wheels)
        {
            if (wheel.collider == null || wheel.visual == null) continue;
            wheel.collider.GetWorldPose(out Vector3 position, out Quaternion rotation);
            wheel.visual.SetPositionAndRotation(position, rotation * wheel.visualRotationOffset);
        }
    }

    public void ConfigureTouchInput(TestDriveTouchControls controls) => touchControls = controls;

    public void SetChallengeInputLocked(bool locked)
    {
        challengeInputLocked = locked;
        if (locked) ClearControls();
    }

    public void SetChallengeCoasting()
    {
        SetChallengeStopped(false);
        IsChallengeCoasting = true;
        challengeInputLocked = true;
        moveInput = steerInput = currentMotorTorque = 0f;
        brakeInput = resetRequested = false;
        IsBraking = IsReversing = false;
        if (touchControls != null) touchControls.ReleaseAll();
        // Release engine/brake input without changing either velocity or snapping steering.
        foreach (var wheel in wheels)
        {
            if (wheel.collider == null) continue;
            wheel.collider.motorTorque = 0f;
            wheel.collider.brakeTorque = coastingBrakeTorque;
        }
    }

    public void SetChallengeStopped(bool stopped)
    {
        IsChallengeCoasting = false;
        if (IsChallengeStopped == stopped) return;
        if (vehicleBody == null) vehicleBody = GetComponent<Rigidbody>();
        if (stopped)
        {
            preChallengeConstraints = vehicleBody.constraints;
            IsChallengeStopped = true;
            ClearControls();
            vehicleBody.linearVelocity = Vector3.zero;
            vehicleBody.angularVelocity = Vector3.zero;
            vehicleBody.constraints = RigidbodyConstraints.FreezeAll;
            vehicleBody.Sleep();
        }
        else
        {
            IsChallengeStopped = false;
            vehicleBody.constraints = preChallengeConstraints;
            ClearControls();
            vehicleBody.WakeUp();
        }
    }

    public void ConfigureReset(Transform spawnPoint, TestDriveCameraFollow cameraFollow)
    {
        resetPoint = spawnPoint;
        followCamera = cameraFollow;
    }

    public void RecoverOrResetVehicle()
    {
        if (IsChallengeStopped || IsChallengeCoasting) return;
        if (CanRecoverLocally)
        {
            Transform point = boggedSurface.RecoveryPoint;
            ResetAt(point.position, point.rotation);
        }
        else ResetVehicle();
    }

    public void ResetVehicle()
    {
        if (IsChallengeStopped || IsChallengeCoasting) return;
        ResetAt(resetPoint != null ? resetPoint.position : initialPosition,
            resetPoint != null ? resetPoint.rotation : initialRotation);
    }

    private void ResetAt(Vector3 position, Quaternion rotation)
    {
        ResetCount++;
        ClearControls();
        if (TryGetComponent<TestDriveDust>(out var dust)) dust.ClearDust();
        vehicleBody.linearVelocity = Vector3.zero;
        vehicleBody.angularVelocity = Vector3.zero;
        vehicleBody.position = position;
        vehicleBody.rotation = rotation;
        transform.SetPositionAndRotation(position, rotation);
        vehicleBody.WakeUp();
        if (followCamera != null) followCamera.SnapToTarget();
    }

    private void ClearControls()
    {
        IsBraking = false;
        IsReversing = false;
        IsBoggedDown = false;
        bogAttemptTime = 0f;
        boggedSurface = null;
        if (touchControls != null) touchControls.ReleaseAll();
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
