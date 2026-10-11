using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody))]
public class CarController : NetworkBehaviour
{
    [Header("UI Controls")]
    [SerializeField] private Buttons btnGas;
    [SerializeField] private Buttons btnBrake;
    [SerializeField] private Buttons btnLeft;
    [SerializeField] private Buttons btnRight;

    [Header("Drive Settings")]
    [SerializeField] private float acceleration = 35f;
    [SerializeField] private float maxSpeed = 25f;
    [SerializeField] private float brakeForce = 45f;
    [SerializeField] private float reverseSpeed = 16f;
    [SerializeField] private float coastDrag = 14f;
    [SerializeField] private float corneringDrag = 4f;

    [Header("Lights")]
    [SerializeField] private Light brakeLightL;
    [SerializeField] private Light brakeLightR;
    [SerializeField] private float idleLightIntensity = 0.5f;
    [SerializeField] private float brakeLightIntensity = 8.0f;

    [Header("Audio")]
    [SerializeField] private AudioSource engineAudio;
    [SerializeField] private float minPitch = 0.75f;
    [SerializeField] private float maxPitch = 2.2f;

    [Header("Steering Settings")]
    [SerializeField] private float turnSpeed = 105f;
    [Range(0f, 1f)]
    [SerializeField] private float lateralGrip = 0.90f;
    [Range(0f, 1f)]
    [SerializeField] private float driftGrip = 0.76f;

    [Header("Body Lean")]
    [SerializeField] private Transform carBody;
    [SerializeField] private float bodyRollAngle = 4.5f;
    [SerializeField] private float bodyPitchAngle = 3.0f;
    [SerializeField] private float bodyLeanSpeed = 8f;
    [SerializeField] private float engineRumble = 0.006f;

    [Header("Visual Wheels")]
    [SerializeField] private Transform wheelFL;
    [SerializeField] private Transform wheelFR;
    [SerializeField] private Transform wheelRL;
    [SerializeField] private Transform wheelRR;
    [SerializeField] private float maxSteerVisualAngle = 35f;
    [SerializeField] private float bodyYawAngle = 12f;

    private Rigidbody rb;
    private float targetSteerInput;
    private float throttleInput;
    private float currentRollAngle;
    private Vector3 initialBodyLocalPos;

    private Quaternion initialRotationFL;
    private Quaternion initialRotationFR;
    private Quaternion initialRotationRL;
    private Quaternion initialRotationRR;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (wheelFL) initialRotationFL = wheelFL.localRotation;
        if (wheelFR) initialRotationFR = wheelFR.localRotation;
        if (wheelRL) initialRotationRL = wheelRL.localRotation;
        if (wheelRR) initialRotationRR = wheelRR.localRotation;

        if (!btnGas) btnGas = GameObject.Find("Btn_Gas")?.GetComponent<Buttons>();
        if (!btnBrake) btnBrake = GameObject.Find("Btn_Brake")?.GetComponent<Buttons>();
        if (!btnLeft) btnLeft = GameObject.Find("Btn_Left")?.GetComponent<Buttons>();
        if (!btnRight) btnRight = GameObject.Find("Btn_Right")?.GetComponent<Buttons>();

        if (!carBody)
        {
            Transform found = transform.Find("Visuals");
            if (found) carBody = found;
        }

        if (carBody) initialBodyLocalPos = carBody.localPosition;
    }

    private void Update()
    {
        if (IsSpawned && !IsOwner) return;

        ProcessInput();
        UpdateWheelVisuals();
        UpdateBodyJuice();

        bool isBraking = throttleInput < 0f;
        float targetIntensity = isBraking ? brakeLightIntensity : idleLightIntensity;

        if (brakeLightL) brakeLightL.intensity = targetIntensity;
        if (brakeLightR) brakeLightR.intensity = targetIntensity;

        if (engineAudio && rb)
        {
            float speed = Vector3.Dot(rb.linearVelocity, transform.forward);
            float speedRatio = Mathf.Clamp01(Mathf.Abs(speed) / maxSpeed);
            engineAudio.pitch = Mathf.Lerp(minPitch, maxPitch, speedRatio);
        }
    }

    private void FixedUpdate()
    {
        if (IsSpawned && !IsOwner) return;

        ApplyMovement();
        ApplySteering();
        ApplyLateralGrip();
    }

    private void ProcessInput()
    {
        bool uiGas = btnGas && btnGas.IsPressed;
        bool uiBrake = btnBrake && btnBrake.IsPressed;
        bool uiLeft = btnLeft && btnLeft.IsPressed;
        bool uiRight = btnRight && btnRight.IsPressed;

        bool keyGas = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
        bool keyBrake = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.Space);
        float keySteer = Input.GetAxisRaw("Horizontal");

        if (uiGas || keyGas) throttleInput = 1f;
        else if (uiBrake || keyBrake) throttleInput = -1f;
        else throttleInput = 0f;

        float rawSteer = 0f;
        if (uiLeft) rawSteer = -1f;
        else if (uiRight) rawSteer = 1f;
        else rawSteer = keySteer;
        targetSteerInput = Mathf.MoveTowards(targetSteerInput, rawSteer, 8f * Time.deltaTime);
    }

    private void ApplyMovement()
    {
        float currentForwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);

        if (throttleInput > 0f)
        {
            if (currentForwardSpeed < maxSpeed)
            {
                rb.AddForce(transform.forward * acceleration, ForceMode.Acceleration);
            }
        }
        else if (throttleInput < 0f)
        {
            if (currentForwardSpeed > 0.5f)
            {
                rb.AddForce(-transform.forward * brakeForce, ForceMode.Acceleration);
            }
            else if (currentForwardSpeed > -reverseSpeed)
            {
                rb.AddForce(-transform.forward * acceleration, ForceMode.Acceleration);
            }
        }
        else
        {
            if (Mathf.Abs(currentForwardSpeed) > 0.2f)
            {
                float dragDirection = -Mathf.Sign(currentForwardSpeed);
                rb.AddForce(transform.forward * (dragDirection * coastDrag), ForceMode.Acceleration);
            }
            else
            {
                Vector3 vel = rb.linearVelocity;
                vel.x = Mathf.MoveTowards(vel.x, 0f, Time.fixedDeltaTime * 10f);
                vel.z = Mathf.MoveTowards(vel.z, 0f, Time.fixedDeltaTime * 10f);
                rb.linearVelocity = vel;
            }
        }

        if (targetSteerInput != 0f && Mathf.Abs(currentForwardSpeed) > 1f)
        {
            rb.AddForce(-transform.forward * (Mathf.Abs(targetSteerInput) * corneringDrag), ForceMode.Acceleration);
        }
    }

    private void ApplySteering()
    {
        float currentForwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);

        if (Mathf.Abs(currentForwardSpeed) > 0.2f)
        {
            float direction = Mathf.Sign(currentForwardSpeed);
            float speedFactor = Mathf.Clamp01(Mathf.Abs(currentForwardSpeed) / 4f);
            float turnAmount = targetSteerInput * (turnSpeed * speedFactor) * direction * Time.fixedDeltaTime;

            Quaternion deltaRotation = Quaternion.Euler(0f, turnAmount, 0f);
            rb.MoveRotation(rb.rotation * deltaRotation);

        }
    }

    private void ApplyLateralGrip()
    {
        float currentGrip = (targetSteerInput != 0f) ? driftGrip : lateralGrip;

        Vector3 sidewaysVelocity = transform.right * Vector3.Dot(rb.linearVelocity, transform.right);
        rb.linearVelocity -= sidewaysVelocity * Mathf.Clamp01(currentGrip * 12f * Time.fixedDeltaTime);
    }

    private void UpdateWheelVisuals()
    {
        float currentSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        float rollDelta = (currentSpeed / 0.28f) * Mathf.Rad2Deg * Time.deltaTime;
        currentRollAngle = (currentRollAngle + rollDelta) % 360f;

        float visualSteer = targetSteerInput * maxSteerVisualAngle;

        if (wheelFL)
            wheelFL.localRotation = initialRotationFL * Quaternion.Euler(0f, visualSteer, 0f) * Quaternion.Euler(currentRollAngle, 0f, 0f);
        if (wheelFR)
            wheelFR.localRotation = initialRotationFR * Quaternion.Euler(0f, visualSteer, 0f) * Quaternion.Euler(currentRollAngle, 0f, 0f);

        if (wheelRL)
            wheelRL.localRotation = initialRotationRL * Quaternion.Euler(currentRollAngle, 0f, 0f);
        if (wheelRR)
            wheelRR.localRotation = initialRotationRR * Quaternion.Euler(currentRollAngle, 0f, 0f);
    }

    private void UpdateBodyJuice()
    {
        if (!carBody) return;

        float currentSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        float speedRatio = Mathf.Clamp01(Mathf.Abs(currentSpeed) / maxSpeed);

        float targetRoll = -targetSteerInput * (bodyRollAngle * speedRatio);

        float targetPitch = 0f;
        if (throttleInput > 0f && currentSpeed < maxSpeed) targetPitch = bodyPitchAngle * 0.7f;
        else if (throttleInput < 0f && currentSpeed > 1f) targetPitch = -bodyPitchAngle;

        float targetYaw = targetSteerInput * (bodyYawAngle * speedRatio);
        Quaternion targetBodyRot = Quaternion.Euler(targetPitch, targetYaw, targetRoll);
        carBody.localRotation = Quaternion.Slerp(carBody.localRotation, targetBodyRot, bodyLeanSpeed * Time.deltaTime);

        if (Mathf.Abs(currentSpeed) > 0.5f)
        {
            float shakeX = (Mathf.PerlinNoise(Time.time * 25f, 0f) - 0.5f) * engineRumble * speedRatio;
            float shakeY = (Mathf.PerlinNoise(0f, Time.time * 25f) - 0.5f) * engineRumble * speedRatio;
            carBody.localPosition = initialBodyLocalPos + new Vector3(shakeX, shakeY, 0f);
        }
        else
        {
            carBody.localPosition = initialBodyLocalPos;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            Camera chaseCam = FindFirstObjectByType<Camera>();
            if (chaseCam)
            {
                chaseCam.target = transform;
            }
        }
    }
}