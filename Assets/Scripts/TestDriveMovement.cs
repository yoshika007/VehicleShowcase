using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class TestDriveMovement : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 8f;
    [SerializeField, Min(0f)] private float turnSpeed = 60f;

    private Rigidbody vehicleBody;
    private float moveInput;
    private float steerInput;

    private void Awake()
    {
        vehicleBody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        moveInput = 0f;
        steerInput = 0f;
        if (keyboard == null) return;

        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            moveInput += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            moveInput -= 1f;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            steerInput -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            steerInput += 1f;
    }

    private void FixedUpdate()
    {
        Quaternion nextRotation = vehicleBody.rotation;
        if (Mathf.Abs(moveInput * moveSpeed) > 0.01f)
        {
            // Reversing changes which way the car turns, like steering a real car.
            float turnAngle = steerInput * turnSpeed * moveInput * Time.fixedDeltaTime;
            nextRotation *= Quaternion.Euler(0f, turnAngle, 0f);
            vehicleBody.MoveRotation(nextRotation);
        }

        // Move along the car's new facing direction.
        Vector3 velocity = nextRotation * Vector3.forward * (moveInput * moveSpeed);
        // Let gravity and the road handle vertical movement.
        velocity.y = vehicleBody.linearVelocity.y;
        vehicleBody.linearVelocity = velocity;
    }

    private void OnDisable()
    {
        moveInput = 0f;
        steerInput = 0f;
        if (vehicleBody == null) return;
        Vector3 velocity = vehicleBody.linearVelocity;
        vehicleBody.linearVelocity = new Vector3(0f, velocity.y, 0f);
    }
}
