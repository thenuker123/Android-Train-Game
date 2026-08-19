using UnityEngine;

/// <summary>
/// Wireless remote item for adjusting train throttle and brake while on foot.
/// </summary>
public class ItemTrainRemote : MonoBehaviour, IInventoryItem
{
    [SerializeField] private string itemName = "Train Remote";
    [SerializeField, Min(1f)] private float controlRange = 120f;
    [SerializeField, Min(0.01f)] private float throttleStep = 0.1f;
    [SerializeField, Min(0.01f)] private float brakeStep = 0.1f;

    public string ItemName => itemName;

    private TrainController _lockedTrain;

    public void OnEquipped()
    {
    }

    public void OnUnequipped()
    {
    }

    public void OnUse()
    {
        if (_lockedTrain == null)
        {
            _lockedTrain = FindNearestTrain();
        }

        if (_lockedTrain != null)
        {
            _lockedTrain.SetBrake(Mathf.Max(0f, _lockedTrain.currentBrake - brakeStep));
        }
    }

    public void IncreaseThrottle()
    {
        TrainController train = ResolveTrain();
        if (train == null)
        {
            return;
        }

        train.SetThrottle(train.currentThrottle + throttleStep);
    }

    public void DecreaseThrottle()
    {
        TrainController train = ResolveTrain();
        if (train == null)
        {
            return;
        }

        train.SetThrottle(train.currentThrottle - throttleStep);
    }

    public void IncreaseBrake()
    {
        TrainController train = ResolveTrain();
        if (train == null)
        {
            return;
        }

        train.SetBrake(train.currentBrake + brakeStep);
    }

    public void DecreaseBrake()
    {
        TrainController train = ResolveTrain();
        if (train == null)
        {
            return;
        }

        train.SetBrake(train.currentBrake - brakeStep);
    }

    private TrainController ResolveTrain()
    {
        if (_lockedTrain != null)
        {
            float sqrDistance = (_lockedTrain.transform.position - transform.position).sqrMagnitude;
            if (sqrDistance <= controlRange * controlRange && !_lockedTrain.IsDerailed)
            {
                return _lockedTrain;
            }
        }

        _lockedTrain = FindNearestTrain();
        return _lockedTrain;
    }

    private TrainController FindNearestTrain()
    {
        TrainController[] trains = FindObjectsOfType<TrainController>();
        TrainController nearest = null;
        float nearestSqrDistance = controlRange * controlRange;

        for (int i = 0; i < trains.Length; i++)
        {
            if (trains[i] == null || trains[i].IsDerailed)
            {
                continue;
            }

            float sqrDistance = (trains[i].transform.position - transform.position).sqrMagnitude;
            if (sqrDistance <= nearestSqrDistance)
            {
                nearestSqrDistance = sqrDistance;
                nearest = trains[i];
            }
        }

        return nearest;
    }
}
