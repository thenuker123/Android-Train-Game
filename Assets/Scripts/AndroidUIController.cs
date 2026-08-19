using UnityEngine;

/// <summary>
/// Forwards Android UI control values directly to the train controller.
/// </summary>
public class AndroidUIController : MonoBehaviour
{
    [SerializeField] private TrainController trainController;

    private void Awake()
    {
        if (trainController == null)
        {
            trainController = FindObjectOfType<TrainController>();
        }
    }

    public void OnThrottleChanged(float value)
    {
        if (trainController != null)
        {
            trainController.SetThrottle(value);
        }
    }

    public void OnBrakeChanged(float value)
    {
        if (trainController != null)
        {
            trainController.SetBrake(value);
        }
    }
}
