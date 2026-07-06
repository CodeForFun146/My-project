using UnityEngine;

public class CarController : MonoBehaviour
{
    [Header("Wheel Colliders")]
    public WheelCollider wheelColliderFL;
    public WheelCollider wheelColliderFR;
    public WheelCollider wheelColliderRL;
    public WheelCollider wheelColliderRR;

    [Header("Wheel Meshes")]
    public Transform wheelMeshFL;
    public Transform wheelMeshFR;
    public Transform wheelMeshRL;
    public Transform wheelMeshRR;

    [Header("Car Settings")]
    public float motorTorque     = 2000f;
    public float brakeTorque     = 5000f;
    public float handbrakeTorque = 8000f;
    public float maxSteerAngle   = 35f;
    public float maxSpeedKMH     = 180f;
    public float downforce       = 100f;

    private Rigidbody _rb;
    private float _throttle;
    private float _steer;
    private bool  _handbrake;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.centerOfMass = new Vector3(0f, -0.5f, 0f);
    }

    private void Update()
    {
        _throttle  = Input.GetAxis("Vertical");
        _steer     = Input.GetAxis("Horizontal");
        _handbrake = Input.GetKey(KeyCode.Space);
    }

    private void FixedUpdate()
    {
        float speedKMH = _rb.linearVelocity.magnitude * 3.6f;

        // Steering
        float angle = _steer * maxSteerAngle;
        wheelColliderFL.steerAngle = angle;
        wheelColliderFR.steerAngle = angle;

        // Motor
        float torque = (speedKMH < maxSpeedKMH) ? _throttle * motorTorque : 0f;
        wheelColliderRL.motorTorque = torque;
        wheelColliderRR.motorTorque = torque;

        // Braking
        if (_handbrake)
        {
            wheelColliderRL.brakeTorque = handbrakeTorque;
            wheelColliderRR.brakeTorque = handbrakeTorque;
            wheelColliderFL.brakeTorque = 0f;
            wheelColliderFR.brakeTorque = 0f;
        }
        else
        {
            float brake = (_throttle < 0f && _rb.linearVelocity.magnitude > 0.5f) ? brakeTorque : 0f;
            wheelColliderFL.brakeTorque = brake;
            wheelColliderFR.brakeTorque = brake;
            wheelColliderRL.brakeTorque = brake;
            wheelColliderRR.brakeTorque = brake;
        }

        // Downforce
        _rb.AddForce(-transform.up * downforce * _rb.linearVelocity.magnitude);

        // Sync wheel meshes
        ApplyWheelPose(wheelColliderFL, wheelMeshFL);
        ApplyWheelPose(wheelColliderFR, wheelMeshFR);
        ApplyWheelPose(wheelColliderRL, wheelMeshRL);
        ApplyWheelPose(wheelColliderRR, wheelMeshRR);
    }

    private void ApplyWheelPose(WheelCollider col, Transform mesh)
    {
        if (col == null || mesh == null) return;
        col.GetWorldPose(out Vector3 pos, out Quaternion rot);
        mesh.position = pos;
        mesh.rotation = rot;
    }
}
