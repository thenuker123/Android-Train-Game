using UnityEngine;

/// <summary>
/// Services locomotives stopped in the station trigger by charging the player's wallet.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ServiceStation : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField, Min(0f)] private float stoppedSpeedThreshold = 0.15f;
    [SerializeField, Min(0.2f)] private float serviceRetryDelay = 1f;

    [Header("Costs")]
    [SerializeField, Min(0f)] private float fuelCostPerPercent = 0.7f;
    [SerializeField, Min(0f)] private float oilCostPerPercent = 1.1f;
    [SerializeField, Min(0f)] private float repairCostPerPercent = 2.2f;

    private float _nextAllowedServiceTime;

    private void Awake()
    {
        Collider trigger = GetComponent<Collider>();
        trigger.isTrigger = true;
    }

    private void OnTriggerStay(Collider other)
    {
        if (Time.time < _nextAllowedServiceTime)
        {
            return;
        }

        TrainController train = other.GetComponentInParent<TrainController>();
        if (train == null || Mathf.Abs(train.CurrentSpeed) > stoppedSpeedThreshold)
        {
            return;
        }

        LocomotiveStats stats = train.GetComponent<LocomotiveStats>();
        JobSystem jobSystem = JobSystem.Instance;
        if (stats == null || jobSystem == null)
        {
            return;
        }

        float serviceCost = CalculateServiceCost(stats);
        if (serviceCost <= 0f)
        {
            _nextAllowedServiceTime = Time.time + serviceRetryDelay;
            return;
        }

        if (jobSystem.TrySpendCash(serviceCost))
        {
            stats.FullService();
        }

        _nextAllowedServiceTime = Time.time + serviceRetryDelay;
    }

    public float CalculateServiceCost(LocomotiveStats stats)
    {
        if (stats == null)
        {
            return 0f;
        }

        return (stats.GetMissingFuelPercent() * fuelCostPerPercent) +
               (stats.GetMissingOilPercent() * oilCostPerPercent) +
               (stats.GetMissingHullPercent() * repairCostPerPercent);
    }
}
