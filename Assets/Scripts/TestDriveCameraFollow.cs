using UnityEngine;

public class TestDriveCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 followOffset = new Vector3(0f, 3.6f, -7.5f);
    [SerializeField] private Vector3 lookOffset = new Vector3(0f, 0.7f, 5f);
    [SerializeField, Min(0.01f)] private float followSharpness = 10f;
    [SerializeField, Min(0.01f)] private float lookSharpness = 12f;

    private void LateUpdate()
    {
        if (target == null) return;

        // Rotate the offset with the car so the camera stays behind it through turns.
        Vector3 desiredPosition = target.position + target.rotation * followOffset;
        float followBlend = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followBlend);

        Vector3 lookPoint = target.position + target.rotation * lookOffset;
        Vector3 lookDirection = lookPoint - transform.position;
        if (lookDirection.sqrMagnitude < 0.0001f) return;

        Quaternion desiredRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        float lookBlend = 1f - Mathf.Exp(-lookSharpness * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, lookBlend);
    }
}
