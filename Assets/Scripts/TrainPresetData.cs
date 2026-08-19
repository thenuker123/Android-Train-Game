using UnityEngine;

/// <summary>
/// Modular train/wagon configuration template used by TrainController and spawning systems.
/// </summary>
[CreateAssetMenu(fileName = "TrainPreset", menuName = "Train Game/Train Preset Data")]
public class TrainPresetData : ScriptableObject
{
    [Header("Identity")]
    public string vehicleName = "DE2";
    public string vehicleType = "Locomotive";

    [Header("Core Physics")]
    [Min(1f)] public float mass = 120000f;
    [Min(1000f)] public float maxTractiveEffort = 420000f;
    [Min(1000f)] public float maxBrakingForce = 460000f;

    [Header("Thermal")]
    [Min(1f)] public float optimalOperatingTemp = 95f;

    [Header("Fuel")]
    public string fuelType = "Diesel";
    [Min(0f)] public float fuelCapacity = 5000f;
}
