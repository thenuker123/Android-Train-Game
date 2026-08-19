using System;
using UnityEngine;

/// <summary>
/// Advanced locomotive and train-line physics simulation optimized for waypoint-based tracks.
/// </summary>
[DisallowMultipleComponent]
public class TrainPhysicsAdvanced : MonoBehaviour
{
    public enum ElectricalCircuit
    {
        MainAlternator,
        FuelPump,
        Electronics,
        DynamicBrake
    }

    [Header("Track")]
    [SerializeField] private TrackManager trackManager;
    [SerializeField, Min(0.01f)] private float waypointReachDistance = 0.25f;

    [Header("Locomotive")]
    [SerializeField, Min(1f)] private float locomotiveMassKg = 120000f;
    [SerializeField, Range(0.1f, 1f)] private float weightOnDriversRatio = 0.72f;
    [SerializeField, Min(1000f)] private float maxTractiveEffortN = 420000f;
    [SerializeField, Min(0f)] private float maxSpeed = 35f;
    [SerializeField, Min(0f)] private float aerodynamicDragCoefficient = 4.25f;
    [SerializeField, Min(0f)] private float rollingResistanceCoefficient = 0.0018f;

    [Header("Adhesion")]
    [SerializeField, Range(0.05f, 0.6f)] private float dryAdhesionCoefficient = 0.28f;
    [SerializeField, Range(0.05f, 1f)] private float wetAdhesionMultiplier = 0.55f;
    [SerializeField] private bool wetTrack;
    [SerializeField, Range(0f, 1f)] private float slipForceLossMultiplier = 0.75f;
    [SerializeField] private ParticleSystem wheelSlipSparks;

    [Header("Sanding")]
    [SerializeField, Min(0f)] private float sandCapacityKg = 80f;
    [SerializeField, Min(0f)] private float sandConsumptionKgPerSecond = 0.35f;
    [SerializeField, Min(0f)] private float sandingAdhesionBonus = 0.1f;

    [Header("Air Brake")]
    [SerializeField, Min(1f)] private float brakePipeNominalPressureKpa = 620f;
    [SerializeField, Min(1f)] private float brakePipeMinPressureKpa = 360f;
    [SerializeField, Min(1f)] private float brakePipeApplyRateKpaPerSecond = 40f;
    [SerializeField, Min(1f)] private float brakePipeReleaseRateKpaPerSecond = 55f;
    [SerializeField, Min(1f)] private float maxBrakeCylinderPressureKpa = 380f;
    [SerializeField, Min(0.01f)] private float brakeCylinderBuildRate = 2f;
    [SerializeField, Min(0.01f)] private float brakeCylinderReleaseRate = 2.6f;
    [SerializeField, Min(1f)] private float brakeAuxReservoirCapacityKpa = 480f;
    [SerializeField, Min(0.01f)] private float brakeAuxRechargeRateKpaPerSecond = 25f;
    [SerializeField, Min(0f)] private float brakePropagationSecondsPerWagon = 0.2f;
    [SerializeField, Min(0f)] private float maxServiceBrakeForceN = 460000f;

    [Header("Traction Motor Thermal")]
    [SerializeField, Min(1f)] private float tractionMotorMaxTemperatureC = 180f;
    [SerializeField, Min(0f)] private float tractionMotorHeatRate = 22f;
    [SerializeField, Min(0f)] private float tractionMotorCoolingRate = 11f;

    [Header("Input")]
    [Range(-1f, 1f)] public float currentThrottle;
    [Range(0f, 1f)] public float currentBrake;

    public bool IsDerailed { get; private set; }
    public bool isDerailed => IsDerailed;
    public float CurrentSpeed => _currentSpeed;
    public int CurrentWaypointIndex => _currentWaypointIndex;
    public TrackManager CurrentTrackManager => trackManager;
    public float WheelSlip => _wheelSlip;
    public bool IsSanding => _isSanding;
    public float SandRemainingKg => _sandRemainingKg;
    public float BrakePipePressureKpa => _brakePipePressureKpa;
    public float AuxiliaryReservoirPressureKpa => _auxiliaryReservoirPressureKpa;
    public float TractionMotorTemperatureC => _tractionMotorTemperatureC;
    public float TractionMotorLoad01 => _tractionMotorLoad01;

    public event Action<int, int, int> WaypointTransitioned;

    private const int BrakeHistorySampleCount = 512;
    private const float BrakeHistoryStep = 0.1f;

    private Transform _cachedTransform;
    private float _waypointReachDistanceSqr;
    private int _currentWaypointIndex;
    private float _currentSpeed;
    private float _wheelSlip;
    private float _sandRemainingKg;
    private bool _isSanding;
    private float _brakePipePressureKpa;
    private float _auxiliaryReservoirPressureKpa;
    private float _brakeCylinderPressureKpa;
    private float _tractionMotorTemperatureC;
    private float _tractionMotorLoad01;
    private float _brakeHistoryTimer;
    private int _brakeHistoryWriteIndex;
    private readonly float[] _brakeCommandHistory = new float[BrakeHistorySampleCount];
    private readonly bool[] _circuitOperational = { true, true, true, true };

    private LocomotiveStats _locomotiveStats;
    private WagonWeightDistributor _wagonWeightDistributor;
    private FuseCabinetInteractable _fuseCabinet;

    protected virtual void Awake()
    {
        _cachedTransform = transform;
        _waypointReachDistanceSqr = waypointReachDistance * waypointReachDistance;

        if (trackManager == null)
        {
            trackManager = FindObjectOfType<TrackManager>();
        }

        _locomotiveStats = GetComponent<LocomotiveStats>();
        _wagonWeightDistributor = GetComponent<WagonWeightDistributor>();
        _fuseCabinet = GetComponent<FuseCabinetInteractable>();
        if (_fuseCabinet != null)
        {
            _fuseCabinet.BindPhysics(this);
        }

        _sandRemainingKg = sandCapacityKg;
        _brakePipePressureKpa = brakePipeNominalPressureKpa;
        _auxiliaryReservoirPressureKpa = brakeAuxReservoirCapacityKpa;
    }

    protected virtual void Start()
    {
        if (trackManager != null && trackManager.WaypointCount > 0)
        {
            _currentWaypointIndex = trackManager.GetClosestWaypointIndex(_cachedTransform.position);
        }
    }

    protected virtual void Update()
    {
        if (IsDerailed || trackManager == null || trackManager.WaypointCount == 0)
        {
            return;
        }

        float deltaTime = Time.deltaTime;
        UpdateAirBrakeSimulation(deltaTime);
        UpdateTractionMotorThermals(deltaTime);
        UpdateSpeed(deltaTime);
        MoveAlongTrack(deltaTime);
        UpdateSlipVisuals();
    }

    public virtual void ApplyPreset(TrainPresetData preset)
    {
        if (preset == null)
        {
            return;
        }

        locomotiveMassKg = Mathf.Max(1f, preset.mass);
        maxTractiveEffortN = Mathf.Max(1000f, preset.maxTractiveEffort);
        maxServiceBrakeForceN = Mathf.Max(1000f, preset.maxBrakingForce);

        if (string.Equals(preset.vehicleType, "Passenger", StringComparison.OrdinalIgnoreCase))
        {
            brakePipeApplyRateKpaPerSecond = Mathf.Max(brakePipeApplyRateKpaPerSecond, 55f);
            brakePipeReleaseRateKpaPerSecond = Mathf.Max(brakePipeReleaseRateKpaPerSecond, 60f);
        }
        else if (string.Equals(preset.vehicleType, "CargoWagon", StringComparison.OrdinalIgnoreCase))
        {
            brakePipeApplyRateKpaPerSecond = Mathf.Max(30f, brakePipeApplyRateKpaPerSecond * 0.8f);
            brakePipeReleaseRateKpaPerSecond = Mathf.Max(35f, brakePipeReleaseRateKpaPerSecond * 0.8f);
        }

        if (string.Equals(preset.fuelType, "Electric", StringComparison.OrdinalIgnoreCase))
        {
            tractionMotorCoolingRate = Mathf.Max(tractionMotorCoolingRate, 14f);
        }
        else if (string.Equals(preset.fuelType, "Coal/Steam", StringComparison.OrdinalIgnoreCase))
        {
            tractionMotorHeatRate = Mathf.Max(tractionMotorHeatRate, 26f);
        }

        _wagonWeightDistributor?.ApplyPresetDefaults(preset);
    }

    public void SetThrottle(float value)
    {
        currentThrottle = Mathf.Clamp(value, -1f, 1f);
    }

    public void SetBrake(float value)
    {
        currentBrake = Mathf.Clamp01(value);
    }

    /// <summary>
    /// Toggles sand deployment.
    /// </summary>
    public void DeploySand()
    {
        DeploySand(!_isSanding);
    }

    public void DeploySand(bool enabled)
    {
        _isSanding = enabled && _sandRemainingKg > 0f;
    }

    public void SetWetTrack(bool isWet)
    {
        wetTrack = isWet;
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
        _wheelSlip = 0f;
        Debug.LogError($"Train derailed: {reason}");
    }

    public void SetTrackManager(TrackManager nextTrackManager, bool snapToClosestWaypoint = true)
    {
        if (nextTrackManager == null || nextTrackManager.WaypointCount == 0)
        {
            return;
        }

        trackManager = nextTrackManager;

        if (snapToClosestWaypoint)
        {
            _currentWaypointIndex = trackManager.GetClosestWaypointIndex(_cachedTransform.position);
        }
    }

    public void SetCircuitOperational(ElectricalCircuit circuit, bool operational)
    {
        _circuitOperational[(int)circuit] = operational;
    }

    public bool IsCircuitOperational(ElectricalCircuit circuit)
    {
        return _circuitOperational[(int)circuit];
    }

    private void UpdateAirBrakeSimulation(float deltaTime)
    {
        float targetPipePressure = Mathf.Lerp(brakePipeNominalPressureKpa, brakePipeMinPressureKpa, currentBrake);
        float pipeRate = targetPipePressure < _brakePipePressureKpa ? brakePipeApplyRateKpaPerSecond : brakePipeReleaseRateKpaPerSecond;
        _brakePipePressureKpa = Mathf.MoveTowards(_brakePipePressureKpa, targetPipePressure, pipeRate * deltaTime);

        float commandedCylinderPressure = Mathf.Clamp(brakePipeNominalPressureKpa - _brakePipePressureKpa, 0f, maxBrakeCylinderPressureKpa);
        float availablePressure = Mathf.Min(commandedCylinderPressure, _auxiliaryReservoirPressureKpa);

        if (availablePressure > _brakeCylinderPressureKpa)
        {
            float buildDelta = maxBrakeCylinderPressureKpa * brakeCylinderBuildRate * deltaTime;
            float nextPressure = Mathf.Min(availablePressure, _brakeCylinderPressureKpa + buildDelta);
            float drained = Mathf.Max(0f, nextPressure - _brakeCylinderPressureKpa);
            _brakeCylinderPressureKpa = nextPressure;
            _auxiliaryReservoirPressureKpa = Mathf.Max(0f, _auxiliaryReservoirPressureKpa - drained);
        }
        else
        {
            float releaseDelta = maxBrakeCylinderPressureKpa * brakeCylinderReleaseRate * deltaTime;
            _brakeCylinderPressureKpa = Mathf.MoveTowards(_brakeCylinderPressureKpa, availablePressure, releaseDelta);
        }

        float rechargeRate = IsEnginePowerAvailable() ? brakeAuxRechargeRateKpaPerSecond : brakeAuxRechargeRateKpaPerSecond * 0.3f;
        _auxiliaryReservoirPressureKpa = Mathf.MoveTowards(
            _auxiliaryReservoirPressureKpa,
            brakeAuxReservoirCapacityKpa,
            rechargeRate * deltaTime);

        float localBrakeRatio = maxBrakeCylinderPressureKpa > 0.01f ? Mathf.Clamp01(_brakeCylinderPressureKpa / maxBrakeCylinderPressureKpa) : 0f;

        _brakeHistoryTimer += deltaTime;
        while (_brakeHistoryTimer >= BrakeHistoryStep)
        {
            _brakeHistoryTimer -= BrakeHistoryStep;
            _brakeCommandHistory[_brakeHistoryWriteIndex] = localBrakeRatio;
            _brakeHistoryWriteIndex = (_brakeHistoryWriteIndex + 1) % BrakeHistorySampleCount;
        }
    }

    private void UpdateTractionMotorThermals(float deltaTime)
    {
        float throttleDemand = Mathf.Abs(currentThrottle);
        float slipHeatMultiplier = 1f + (_wheelSlip * 1.5f);
        float heat = throttleDemand * tractionMotorHeatRate * slipHeatMultiplier * deltaTime;
        float cooling = tractionMotorCoolingRate * deltaTime;

        _tractionMotorTemperatureC = Mathf.Clamp(_tractionMotorTemperatureC + heat - cooling, 0f, tractionMotorMaxTemperatureC);
        _tractionMotorLoad01 = Mathf.Clamp01((throttleDemand * 0.8f) + (_wheelSlip * 0.6f));
    }

    private void UpdateSpeed(float deltaTime)
    {
        int travelDirection = ResolveTravelDirection();
        float throttleSign = Mathf.Sign(currentThrottle);
        float throttleMagnitude = Mathf.Abs(currentThrottle);

        if (!IsEnginePowerAvailable())
        {
            throttleMagnitude = 0f;
            throttleSign = 0f;
        }

        float torqueMultiplier = _locomotiveStats != null ? _locomotiveStats.CurrentTorqueMultiplier : 1f;
        float requestedTractiveEffort = throttleMagnitude * maxTractiveEffortN * torqueMultiplier;

        float trackGradeSin = GetCurrentTrackGradeSin(travelDirection);
        float driverNormalForce = (locomotiveMassKg * weightOnDriversRatio) * Physics.gravity.magnitude * Mathf.Sqrt(Mathf.Max(0f, 1f - (trackGradeSin * trackGradeSin)));

        float adhesion = dryAdhesionCoefficient * (wetTrack ? wetAdhesionMultiplier : 1f);
        if (_isSanding && _sandRemainingKg > 0f)
        {
            adhesion += sandingAdhesionBonus;
            _sandRemainingKg = Mathf.Max(0f, _sandRemainingKg - (sandConsumptionKgPerSecond * deltaTime));
            if (_sandRemainingKg <= 0f)
            {
                _isSanding = false;
            }
        }

        float maxAdhesionForce = driverNormalForce * Mathf.Max(0f, adhesion);
        _wheelSlip = requestedTractiveEffort <= 0.01f
            ? Mathf.MoveTowards(_wheelSlip, 0f, 3.5f * deltaTime)
            : Mathf.Clamp01((requestedTractiveEffort - maxAdhesionForce) / requestedTractiveEffort);

        float tractiveEffort = Mathf.Min(requestedTractiveEffort, maxAdhesionForce);
        if (_wheelSlip > 0f)
        {
            tractiveEffort *= Mathf.Lerp(1f, 1f - slipForceLossMultiplier, _wheelSlip);
        }

        float propulsionForce = tractiveEffort * throttleSign;

        float totalTrainMassKg = locomotiveMassKg;
        float wagonGradeForce = 0f;

        if (_wagonWeightDistributor != null)
        {
            totalTrainMassKg += _wagonWeightDistributor.TotalWagonMassKg;
            wagonGradeForce = _wagonWeightDistributor.GetWagonGradeForceNewtons(_currentWaypointIndex, travelDirection, trackManager);
        }

        float locomotiveGradeForce = -locomotiveMassKg * Physics.gravity.magnitude * trackGradeSin;

        float motionSign = Mathf.Abs(_currentSpeed) > 0.05f
            ? Mathf.Sign(_currentSpeed)
            : (Mathf.Abs(throttleSign) > 0.01f ? throttleSign : 0f);

        float delayedBrakeRatio = GetDelayedBrakeRatio();
        float serviceBrakeRatio = Mathf.Clamp01(Mathf.Lerp(_brakeCylinderPressureKpa / Mathf.Max(1f, maxBrakeCylinderPressureKpa), delayedBrakeRatio, 0.65f));

        float dynamicBrakeRatio = 0f;
        if (IsCircuitOperational(ElectricalCircuit.DynamicBrake) && motionSign != 0f)
        {
            float opposingThrottle = Mathf.Clamp01(-currentThrottle * motionSign);
            dynamicBrakeRatio = opposingThrottle * 0.6f;
        }

        float totalBrakeRatio = Mathf.Clamp01(serviceBrakeRatio + dynamicBrakeRatio);
        float brakingForce = totalBrakeRatio * maxServiceBrakeForceN;
        float rollingResistance = totalTrainMassKg * Physics.gravity.magnitude * rollingResistanceCoefficient;
        float aeroDrag = aerodynamicDragCoefficient * _currentSpeed * _currentSpeed;

        float resistiveForce = motionSign == 0f ? 0f : motionSign * (rollingResistance + brakingForce + aeroDrag);

        float netForce = propulsionForce + locomotiveGradeForce + wagonGradeForce - resistiveForce;
        float acceleration = totalTrainMassKg > 0.001f ? netForce / totalTrainMassKg : 0f;

        _currentSpeed += acceleration * deltaTime;
        _currentSpeed = Mathf.Clamp(_currentSpeed, -maxSpeed, maxSpeed);

        if (Mathf.Abs(_currentSpeed) < 0.03f && Mathf.Abs(propulsionForce) < 300f)
        {
            _currentSpeed = 0f;
        }
    }

    private float GetDelayedBrakeRatio()
    {
        if (_wagonWeightDistributor == null)
        {
            return _brakeCylinderPressureKpa / Mathf.Max(1f, maxBrakeCylinderPressureKpa);
        }

        float delaySeconds = _wagonWeightDistributor.WagonCount * brakePropagationSecondsPerWagon;
        int steps = Mathf.Clamp(Mathf.RoundToInt(delaySeconds / BrakeHistoryStep), 0, BrakeHistorySampleCount - 1);
        int readIndex = _brakeHistoryWriteIndex - 1 - steps;
        while (readIndex < 0)
        {
            readIndex += BrakeHistorySampleCount;
        }

        return _brakeCommandHistory[readIndex];
    }

    private bool IsEnginePowerAvailable()
    {
        return IsCircuitOperational(ElectricalCircuit.MainAlternator)
               && IsCircuitOperational(ElectricalCircuit.FuelPump)
               && IsCircuitOperational(ElectricalCircuit.Electronics);
    }

    private int ResolveTravelDirection()
    {
        if (Mathf.Abs(_currentSpeed) > 0.001f)
        {
            return _currentSpeed >= 0f ? 1 : -1;
        }

        return currentThrottle >= 0f ? 1 : -1;
    }

    private float GetCurrentTrackGradeSin(int travelDirection)
    {
        if (trackManager == null || trackManager.WaypointCount < 2)
        {
            return 0f;
        }

        int fromIndex = Mathf.Clamp(_currentWaypointIndex, 0, trackManager.WaypointCount - 1);
        int toIndex = travelDirection >= 0
            ? trackManager.GetNextWaypointIndex(fromIndex)
            : (fromIndex == 0 ? trackManager.WaypointCount - 1 : fromIndex - 1);

        Transform from = trackManager.GetWaypoint(fromIndex);
        Transform to = trackManager.GetWaypoint(toIndex);
        if (from == null || to == null)
        {
            return 0f;
        }

        Vector3 segment = (to.position - from.position);
        float length = segment.magnitude;
        if (length < 0.001f)
        {
            return 0f;
        }

        return segment.y / length;
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

    private void UpdateSlipVisuals()
    {
        if (wheelSlipSparks == null)
        {
            return;
        }

        bool shouldPlay = _wheelSlip >= 0.1f;
        if (shouldPlay && !wheelSlipSparks.isPlaying)
        {
            wheelSlipSparks.Play();
        }
        else if (!shouldPlay && wheelSlipSparks.isPlaying)
        {
            wheelSlipSparks.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}
