using UnityEngine;

/// <summary>
/// Marks a section of track as a curve and defines its safe speed limit.
/// </summary>
public class TrackCurve : MonoBehaviour
{
    [Tooltip("Maximum safe train speed (m/s) while entering this curve.")]
    [Min(0f)]
    public float speedLimit = 10f;
}
