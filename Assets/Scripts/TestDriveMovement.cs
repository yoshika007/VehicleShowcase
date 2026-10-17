using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class TestDriveMovement : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 8f;
    [SerializeField, Min(0f)] private float reverseSpeed = 4f;
    [SerializeField, Min(0f)] private float acceleration = 5f;
    [SerializeField, Min(0f)] private float braking = 12f;
    [SerializeField, Min(0f)] private float coastingDeceleration = 2f;
    [SerializeField, Min(0f)] private float turnSpeed = 60f;

    private Rigidbody vehicleBody;
    private float moveInput;
    private float steerInput;
    private bool brakeInput;

    private void Awake()
    {
        vehicleBody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        moveInput = 0f;
        steerInput = 0f;
        brakeInput = false;
        if (keyboard == null) return;

        bool forwardPressed = keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed;
        bool reversePressed = keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed;
        moveInput = (forwardPressed ? 1f : 0f) - (reversePressed ? 1f : 0f);
        brakeInput = keyboard.spaceKey.isPressed || (forwardPressed && reversePressed);

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            steerInput -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            steerInput += 1f;
    }

    private void FixedUpdate()
    {
        // Read actual speed so collisions cannot leave a hidden speed building up.
        float currentSpeed = Vector3.Dot(vehicleBody.linearVelocity, transform.forward);
        if (Mathf.Abs(currentSpeed) < 0.01f) currentSpeed = 0f;
        bool changingDirection = moveInput != 0f && currentSpeed * moveInput < 0f;

        float targetSpeed;
        float changeRate;
        if (brakeInput || changingDirection)
        {
            // Reach zero before accelerating in the opposite direction.
            targetSpeed = 0f;
            changeRate = braking;
        }
        else if (moveInput != 0f)
        {
            targetSpeed = moveInput > 0f ? moveSpeed : -reverseSpeed;
            changeRate = acceleration;
        }
        else
        {
            targetSpeed = 0f;
            changeRate = coastingDeceleration;
        }

        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, changeRate * Time.fixedDeltaTime);

        Quaternion nextRotation = vehicleBody.rotation;
        if (Mathf.Abs(currentSpeed) > 0.01f)
        {
            // Steering follows speed, including coasting and reversing.
            float speedRatio = Mathf.Clamp(currentSpeed / Mathf.Max(0.01f, moveSpeed), -1f, 1f);
            float turnAngle = steerInput * turnSpeed * speedRatio * Time.fixedDeltaTime;
            nextRotation *= Quaternion.Euler(0f, turnAngle, 0f);
            vehicleBody.MoveRotation(nextRotation);
        }

        Vector3 velocity = nextRotation * Vector3.forward * currentSpeed;
        // Let gravity and the road handle vertical movement.
        velocity.y = vehicleBody.linearVelocity.y;
        vehicleBody.linearVelocity = velocity;
    }

    private void OnDisable()
    {
        moveInput = 0f;
        steerInput = 0f;
        brakeInput = false;
        if (vehicleBody == null) return;
        Vector3 velocity = vehicleBody.linearVelocity;
        vehicleBody.linearVelocity = new Vector3(0f, velocity.y, 0f);
    }
}
