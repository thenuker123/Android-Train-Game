using UnityEngine;

/// <summary>
/// Detects unsafe high-speed transitions across sharp waypoint turns.
/// </summary>
public class DerailmentSystem : MonoBehaviour
{
    [SerializeField] private TrainController trainController;
    [SerializeField] private TrackManager trackManager;

    [Header("Derailment Rules")]
    [SerializeField, Range(0f, 180f)] private float sharpTurnAngle = 45f;
    [SerializeField, Min(0f)] private float maxSafeSpeedOnSharpTurn = 12f;

    private void Awake()
    {
        if (trainController == null)
        {
            trainController = FindObjectOfType<TrainController>();
        }

        if (trackManager == null)
        {
            trackManager = FindObjectOfType<TrackManager>();
        }
    }

    private void OnEnable()
    {
        if (trainController != null)
        {
            trainController.WaypointTransitioned += HandleWaypointTransition;
        }
    }

    private void OnDisable()
    {
        if (trainController != null)
        {
            trainController.WaypointTransitioned -= HandleWaypointTransition;
        }
    }

    private void HandleWaypointTransition(int previousIndex, int currentIndex, int nextIndex)
    {
        if (trainController == null || trackManager == null || trackManager.WaypointCount < 3 || trainController.IsDerailed)
        {
            return;
        }

        Transform previous = trackManager.GetWaypoint(previousIndex);
        Transform current = trackManager.GetWaypoint(currentIndex);
        Transform next = trackManager.GetWaypoint(nextIndex);

        if (previous == null || current == null || next == null)
        {
            return;
        }

        Vector3 incoming = (current.position - previous.position).normalized;
        Vector3 outgoing = (next.position - current.position).normalized;
        float turnAngle = Vector3.Angle(incoming, outgoing);

        if (turnAngle >= sharpTurnAngle && Mathf.Abs(trainController.CurrentSpeed) > maxSafeSpeedOnSharpTurn)
        {
            trainController.TriggerDerailment($"speed {Mathf.Abs(trainController.CurrentSpeed):0.0} exceeded safe limit {maxSafeSpeedOnSharpTurn:0.0} at a {turnAngle:0.0}° turn");
        }
    }
}
