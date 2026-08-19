using UnityEngine;

/// <summary>
/// Handles basic train movement and derailment checks on curve entry.
/// </summary>
public class TrainController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0f)] private float maxSpeed = 25f;
    [SerializeField, Min(0f)] private float acceleration = 5f;
    [SerializeField, Min(0f)] private float brakeDeceleration = 8f;

    [Header("State")]
    [SerializeField, Range(0f, 1f)] private float throttle;
    [SerializeField, Range(0f, 1f)] private float brake;

    public bool isDerailed { get; private set; }
    public float CurrentSpeed => _currentSpeed;

    private float _currentSpeed;

    private void Update()
    {
        if (isDerailed)
        {
            return;
        }

        // Move speed toward target speed from throttle input.
        float targetSpeed = throttle * maxSpeed;
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, acceleration * Time.deltaTime);

        // Apply braking after throttle update.
        if (brake > 0f)
        {
            _currentSpeed = Mathf.Max(0f, _currentSpeed - (brakeDeceleration * brake * Time.deltaTime));
        }

        if (_currentSpeed > 0f)
        {
            transform.Translate(Vector3.forward * (_currentSpeed * Time.deltaTime));
        }
    }

    public void SetThrottle(float value)
    {
        throttle = Mathf.Clamp01(value);
    }

    public void SetBrake(float value)
    {
        brake = Mathf.Clamp01(value);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDerailed)
        {
            return;
        }

        TrackCurve curve = other.GetComponent<TrackCurve>();
        if (curve != null && _currentSpeed > curve.speedLimit)
        {
            isDerailed = true;
            _currentSpeed = 0f;
            throttle = 0f;
            brake = 1f;
        }
    }
}
