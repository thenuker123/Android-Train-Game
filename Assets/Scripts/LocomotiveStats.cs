using UnityEngine;

/// <summary>
/// Simulates locomotive condition values used by service and driving systems.
/// </summary>
[DisallowMultipleComponent]
public class LocomotiveStats : MonoBehaviour
{
    [Header("Simulation")]
    [SerializeField, Min(1f)] private float maxEngineTemperature = 120f;
    [SerializeField, Min(1f)] private float overheatTemperature = 95f;
    [SerializeField, Min(0f)] private float idleCoolingPerSecond = 2.5f;
    [SerializeField, Min(0f)] private float throttleHeatPerSecond = 18f;
    [SerializeField, Min(0f)] private float overheatDamagePerSecond = 6f;

    [Header("Consumables")]
    [SerializeField, Min(0f)] private float fuelBurnPerSecond = 2.5f;
    [SerializeField, Min(0f)] private float oilBurnPerSecond = 0.8f;

    [Header("Performance")]
    [SerializeField, Range(0.1f, 1f)] private float minimumTorqueMultiplier = 0.35f;

    [Header("Visuals")]
    [SerializeField] private ParticleSystem overheatSmoke;

    [Header("Runtime Stats")]
    [SerializeField, Range(0f, 100f)] private float fuelLevel = 100f;
    [SerializeField, Range(0f, 100f)] private float oilLevel = 100f;
    [SerializeField, Range(0f, 100f)] private float hullCondition = 100f;

    public float EngineTemperature { get; private set; }
    public float FuelLevel => fuelLevel;
    public float OilLevel => oilLevel;
    public float HullCondition => hullCondition;
    public float CurrentTorqueMultiplier { get; private set; } = 1f;

    private TrainController _trainController;
    private bool _isSmoking;

    private void Awake()
    {
        _trainController = GetComponent<TrainController>();
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        float throttleIntensity = _trainController != null ? Mathf.Abs(_trainController.currentThrottle) : 0f;

        float heatGain = throttleIntensity * throttleHeatPerSecond * deltaTime;
        float cooling = idleCoolingPerSecond * deltaTime;
        EngineTemperature = Mathf.Clamp(EngineTemperature + heatGain - cooling, 0f, maxEngineTemperature);

        if (throttleIntensity > 0.001f)
        {
            fuelLevel = Mathf.Max(0f, fuelLevel - (throttleIntensity * fuelBurnPerSecond * deltaTime));
            oilLevel = Mathf.Max(0f, oilLevel - (throttleIntensity * oilBurnPerSecond * deltaTime));
        }

        if (EngineTemperature >= overheatTemperature)
        {
            ApplyHullDamage(overheatDamagePerSecond * deltaTime);
            SetSmokeState(true);
        }
        else
        {
            SetSmokeState(false);
        }

        UpdateTorqueMultiplier();
    }

    public void RepairEngine()
    {
        hullCondition = 100f;
        EngineTemperature = 0f;
        SetSmokeState(false);
        UpdateTorqueMultiplier();
    }

    public void Refuel()
    {
        fuelLevel = 100f;
        UpdateTorqueMultiplier();
    }

    public void TopOffOil()
    {
        oilLevel = 100f;
        UpdateTorqueMultiplier();
    }

    public void FullService()
    {
        Refuel();
        TopOffOil();
        RepairEngine();
    }

    public void ApplyHullDamage(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        hullCondition = Mathf.Max(0f, hullCondition - amount);
        UpdateTorqueMultiplier();
    }

    public float GetMissingFuelPercent()
    {
        return 100f - fuelLevel;
    }

    public float GetMissingOilPercent()
    {
        return 100f - oilLevel;
    }

    public float GetMissingHullPercent()
    {
        return 100f - hullCondition;
    }

    private void UpdateTorqueMultiplier()
    {
        float damageMultiplier = Mathf.Lerp(minimumTorqueMultiplier, 1f, hullCondition / 100f);
        float fuelMultiplier = Mathf.Clamp01(fuelLevel / 100f);
        float oilMultiplier = Mathf.Clamp01(oilLevel / 100f);

        float overheatPenalty = 1f;
        if (EngineTemperature > overheatTemperature)
        {
            float normalizedOverheat = Mathf.InverseLerp(overheatTemperature, maxEngineTemperature, EngineTemperature);
            overheatPenalty = Mathf.Lerp(1f, minimumTorqueMultiplier, normalizedOverheat);
        }

        CurrentTorqueMultiplier = Mathf.Clamp(damageMultiplier * fuelMultiplier * oilMultiplier * overheatPenalty, minimumTorqueMultiplier, 1f);
    }

    private void SetSmokeState(bool shouldSmoke)
    {
        if (overheatSmoke == null || _isSmoking == shouldSmoke)
        {
            return;
        }

        _isSmoking = shouldSmoke;
        if (shouldSmoke)
        {
            overheatSmoke.Play();
        }
        else
        {
            overheatSmoke.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}
