using System;
using UnityEngine;

/// <summary>
/// Tracks wagon masses and grade forces for train resistance/assist calculations.
/// </summary>
[DisallowMultipleComponent]
public class WagonWeightDistributor : MonoBehaviour
{
    [Serializable]
    public class WagonLoadProfile
    {
        public string wagonName = "Wagon";
        [Min(1f)] public float emptyMassKg = 18000f;
        [Min(0f)] public float cargoMassKg = 0f;
        [Min(0f)] public float maxCargoMassKg = 60000f;
        [Min(1)] public int waypointOffset = 2;
    }

    [SerializeField] private WagonLoadProfile[] wagons = Array.Empty<WagonLoadProfile>();
    [SerializeField, Min(0f)] private float rollingResistanceCoefficient = 0.0016f;

    public int WagonCount => wagons == null ? 0 : wagons.Length;
    public float TotalWagonMassKg { get; private set; }

    private void Awake()
    {
        RecalculateTotalMass();
    }

    public void RecalculateTotalMass()
    {
        float total = 0f;
        if (wagons == null)
        {
            TotalWagonMassKg = 0f;
            return;
        }

        for (int i = 0; i < wagons.Length; i++)
        {
            total += GetWagonMassKg(i);
        }

        TotalWagonMassKg = Mathf.Max(0f, total);
    }

    public void SetCargoMassKg(int wagonIndex, float cargoMassKg)
    {
        if (wagons == null || wagonIndex < 0 || wagonIndex >= wagons.Length)
        {
            return;
        }

        WagonLoadProfile wagon = wagons[wagonIndex];
        wagon.cargoMassKg = Mathf.Clamp(cargoMassKg, 0f, wagon.maxCargoMassKg);
        RecalculateTotalMass();
    }

    public float GetWagonMassKg(int wagonIndex)
    {
        if (wagons == null || wagonIndex < 0 || wagonIndex >= wagons.Length)
        {
            return 0f;
        }

        WagonLoadProfile wagon = wagons[wagonIndex];
        return Mathf.Max(0f, wagon.emptyMassKg + wagon.cargoMassKg);
    }

    /// <summary>
    /// Returns signed gravity/rolling force from wagons in track-forward coordinates.
    /// Positive values push the consist forward, negative values resist forward movement.
    /// </summary>
    public float GetWagonGradeForceNewtons(int locomotiveWaypointIndex, int travelDirection, TrackManager trackManager)
    {
        if (trackManager == null || trackManager.WaypointCount < 2 || wagons == null || wagons.Length == 0)
        {
            return 0f;
        }

        float netForce = 0f;
        int direction = travelDirection >= 0 ? 1 : -1;

        for (int i = 0; i < wagons.Length; i++)
        {
            WagonLoadProfile wagon = wagons[i];
            int offset = Mathf.Max(1, wagon.waypointOffset) * (i + 1);
            int wagonWaypointIndex = WrapWaypointIndex(locomotiveWaypointIndex - (direction * offset), trackManager.WaypointCount);
            int nextWaypointIndex = direction >= 0
                ? trackManager.GetNextWaypointIndex(wagonWaypointIndex)
                : WrapWaypointIndex(wagonWaypointIndex - 1, trackManager.WaypointCount);

            Transform from = trackManager.GetWaypoint(wagonWaypointIndex);
            Transform to = trackManager.GetWaypoint(nextWaypointIndex);
            if (from == null || to == null)
            {
                continue;
            }

            Vector3 segment = to.position - from.position;
            float length = segment.magnitude;
            if (length < 0.001f)
            {
                continue;
            }

            float wagonMass = Mathf.Max(0f, wagon.emptyMassKg + wagon.cargoMassKg);
            float gradeSin = segment.y / length;
            float gravityForceAlongTrack = -wagonMass * Physics.gravity.magnitude * gradeSin;
            float rollingForce = wagonMass * Physics.gravity.magnitude * rollingResistanceCoefficient;

            netForce += gravityForceAlongTrack;
            netForce -= rollingForce * direction;
        }

        return netForce;
    }

    public void ApplyPresetDefaults(TrainPresetData presetData)
    {
        if (presetData == null || wagons == null || wagons.Length == 0)
        {
            return;
        }

        if (string.Equals(presetData.vehicleType, "CargoWagon", StringComparison.OrdinalIgnoreCase))
        {
            wagons[0].emptyMassKg = Mathf.Max(1f, presetData.mass);
        }

        RecalculateTotalMass();
    }

    private static int WrapWaypointIndex(int index, int count)
    {
        if (count <= 0)
        {
            return 0;
        }

        int wrapped = index % count;
        return wrapped < 0 ? wrapped + count : wrapped;
    }
}
