using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Physical job booklet item containing contract data.
/// </summary>
public class JobBooklet : MonoBehaviour
{
    [SerializeField] private string originStation;
    [SerializeField] private string destinationStation;
    [SerializeField] private string cargoType;
    [SerializeField, Min(0f)] private float payout = 100f;
    [SerializeField] private List<string> requiredWagonIds = new List<string>();

    public string OriginStation => originStation;
    public string DestinationStation => destinationStation;
    public string CargoType => cargoType;
    public float Payout => payout;
    public IReadOnlyList<string> RequiredWagonIds => requiredWagonIds;
    public bool IsTaken { get; private set; }
    public bool IsCompleted { get; private set; }

    public void MarkTaken()
    {
        IsTaken = true;
    }

    public bool CanValidateAtStation(string stationName)
    {
        return !IsCompleted && IsTaken && !string.IsNullOrEmpty(stationName) &&
               string.Equals(destinationStation, stationName, System.StringComparison.OrdinalIgnoreCase);
    }

    public void MarkCompleted()
    {
        IsCompleted = true;
    }
}
