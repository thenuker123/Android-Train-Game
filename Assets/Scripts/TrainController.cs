using System;
using UnityEngine;

/// <summary>
/// Handles train movement along track waypoints using mobile-ready input values.
/// </summary>
public class TrainController : MonoBehaviour
{
    [Header("Track")]
    [SerializeField] private TrackManager trackManager;
    [SerializeField, Min(0.01f)] private float waypointReachDistance = 0.25f;

    [Header("Physics")]
    [SerializeField, Min(0f)] private float maxSpeed = 25f;
    [SerializeField, Min(0f)] private float acceleration = 6f;
    [SerializeField, Min(0f)] private float brakingPower = 12f;
    [SerializeField, Min(0f)] private float trackFriction = 2f;

    [Header("Mobile Input")]
    [Range(-1f, 1f)] public float currentThrottle;
    [Range(0f, 1f)] public float currentBrake;

    public bool IsDerailed { get; private set; }
    public bool isDerailed => IsDerailed;
    public float CurrentSpeed => _currentSpeed;
    public int CurrentWaypointIndex => _currentWaypointIndex;

    public event Action<int, int, int> WaypointTransitioned;

    private Transform _cachedTransform;
    private float _currentSpeed;
    private int _currentWaypointIndex;
    private float _waypointReachDistanceSqr;

    private void Awake()
    {
        _cachedTransform = transform;
        _waypointReachDistanceSqr = waypointReachDistance * waypointReachDistance;

        if (trackManager == null)
        {
            trackManager = FindObjectOfType<TrackManager>();
        }
    }

    private void Start()
    {
        if (trackManager != null && trackManager.WaypointCount > 0)
        {
            _currentWaypointIndex = trackManager.GetClosestWaypointIndex(_cachedTransform.position);
        }
    }

    private void Update()
    {
        if (IsDerailed || trackManager == null || trackManager.WaypointCount == 0)
        {
            return;
        }

        float deltaTime = Time.deltaTime;
        UpdateSpeed(deltaTime);
        MoveAlongTrack(deltaTime);
    }

    public void SetThrottle(float value)
    {
        currentThrottle = Mathf.Clamp(value, -1f, 1f);
    }

    public void SetBrake(float value)
    {
        currentBrake = Mathf.Clamp01(value);
    }

    public void TriggerDerailment(string reason)
    {
        if (IsDerailed)
        {
            return;
        }

        IsDerailed = true;
        _currentSpeed = 0f;
        currentThrottle = 0f;
        currentBrake = 1f;
        Debug.LogError($"Train derailed: {reason}");
    }

    private void UpdateSpeed(float deltaTime)
    {
        float targetSpeed = currentThrottle * maxSpeed;
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, acceleration * deltaTime);

        if (Mathf.Abs(currentThrottle) < 0.001f)
        {
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, 0f, trackFriction * deltaTime);
        }

        if (currentBrake > 0f)
        {
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, 0f, brakingPower * currentBrake * deltaTime);
        }

        _currentSpeed = Mathf.Clamp(_currentSpeed, -maxSpeed, maxSpeed);
    }

    private void MoveAlongTrack(float deltaTime)
    {
        if (Mathf.Abs(_currentSpeed) < 0.001f)
        {
            return;
        }

        int travelDirection = _currentSpeed >= 0f ? 1 : -1;
        int targetWaypointIndex = GetAdjacentWaypointIndex(_currentWaypointIndex, travelDirection);
        Transform targetWaypoint = trackManager.GetWaypoint(targetWaypointIndex);

        if (targetWaypoint == null)
        {
            return;
        }

        Vector3 targetPosition = targetWaypoint.position;
        float moveStep = Mathf.Abs(_currentSpeed) * deltaTime;
        _cachedTransform.position = Vector3.MoveTowards(_cachedTransform.position, targetPosition, moveStep);

        Vector3 lookDirection = targetPosition - _cachedTransform.position;
        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            _cachedTransform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        }

        if ((_cachedTransform.position - targetPosition).sqrMagnitude <= _waypointReachDistanceSqr)
        {
            int previousWaypointIndex = _currentWaypointIndex;
            _currentWaypointIndex = targetWaypointIndex;
            int nextWaypointIndex = GetAdjacentWaypointIndex(_currentWaypointIndex, travelDirection);
            WaypointTransitioned?.Invoke(previousWaypointIndex, _currentWaypointIndex, nextWaypointIndex);
        }
    }

    private int GetAdjacentWaypointIndex(int fromIndex, int direction)
    {
        int waypointCount = trackManager.WaypointCount;
        if (waypointCount <= 1)
        {
            return 0;
        }

        if (direction >= 0)
        {
            return trackManager.GetNextWaypointIndex(fromIndex);
        }

        int clampedIndex = Mathf.Clamp(fromIndex, 0, waypointCount - 1);
        return clampedIndex == 0 ? waypointCount - 1 : clampedIndex - 1;
    }
}
