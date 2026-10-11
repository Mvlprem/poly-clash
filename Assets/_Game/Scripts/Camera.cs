using UnityEngine;

public class Camera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Camera View")]
    [SerializeField] private float distance = 4.5f;
    [SerializeField] private float height = 1.8f;
    [SerializeField] private float pitchAngle = 14f;

    private float currentYaw;
    private float yawVel;

    private void Start()
    {
        if (target) currentYaw = target.eulerAngles.y;
    }

    private void LateUpdate()
    {
        if (!target) return;

        Quaternion targetRotation = Quaternion.Euler(pitchAngle, target.eulerAngles.y, 0f);
        Vector3 targetPosition = target.position - (Quaternion.Euler(0f, target.eulerAngles.y, 0f) * Vector3.forward * distance);
        targetPosition.y = target.position.y + height;

        transform.position = targetPosition;
        transform.rotation = targetRotation;
    }
}