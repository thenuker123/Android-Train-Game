using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages active job contracts and the player's wallet.
/// </summary>
public class JobSystem : MonoBehaviour
{
    public static JobSystem Instance { get; private set; }

    [SerializeField] private List<JobBooklet> activeJobs = new List<JobBooklet>();
    [SerializeField, Min(0f)] private float walletBalance;

    public float WalletBalance => walletBalance;
    public IReadOnlyList<JobBooklet> ActiveJobs => activeJobs;
    public JobBooklet HeldBooklet { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void RegisterJob(JobBooklet jobBooklet)
    {
        if (jobBooklet == null || activeJobs.Contains(jobBooklet))
        {
            return;
        }

        activeJobs.Add(jobBooklet);
    }

    public bool TryTakeJobBooklet(JobBooklet jobBooklet)
    {
        if (jobBooklet == null || jobBooklet.IsCompleted)
        {
            return false;
        }

        RegisterJob(jobBooklet);
        jobBooklet.MarkTaken();
        HeldBooklet = jobBooklet;
        return true;
    }

    public bool TryValidateHeldBooklet(string destinationStation, IReadOnlyList<string> deliveredWagonIds)
    {
        if (HeldBooklet == null || !HeldBooklet.CanValidateAtStation(destinationStation))
        {
            return false;
        }

        if (!HasAllRequiredWagons(HeldBooklet, deliveredWagonIds))
        {
            return false;
        }

        walletBalance += HeldBooklet.Payout;
        HeldBooklet.MarkCompleted();
        activeJobs.Remove(HeldBooklet);
        HeldBooklet = null;
        return true;
    }

    public bool TrySpendCash(float amount)
    {
        if (amount <= 0f)
        {
            return true;
        }

        if (walletBalance < amount)
        {
            return false;
        }

        walletBalance -= amount;
        return true;
    }

    public void AddCash(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        walletBalance += amount;
    }

    private static bool HasAllRequiredWagons(JobBooklet booklet, IReadOnlyList<string> deliveredWagonIds)
    {
        IReadOnlyList<string> required = booklet.RequiredWagonIds;
        if (required == null || required.Count == 0)
        {
            return true;
        }

        if (deliveredWagonIds == null || deliveredWagonIds.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < required.Count; i++)
        {
            bool found = false;
            for (int j = 0; j < deliveredWagonIds.Count; j++)
            {
                if (string.Equals(required[i], deliveredWagonIds[j], System.StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return false;
            }
        }

        return true;
    }
}
