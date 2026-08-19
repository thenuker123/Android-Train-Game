using UnityEngine;

/// <summary>
/// Chooses which connected track path trains are redirected onto at a junction.
/// </summary>
[RequireComponent(typeof(Collider))]
public class RailJunction : MonoBehaviour
{
    [Header("Paths")]
    [SerializeField] private TrackManager straightTrack;
    [SerializeField] private TrackManager divergingTrack;

    [Header("Visual Indicator")]
    [SerializeField] private Transform indicatorVisual;
    [SerializeField] private Vector3 straightEuler = new Vector3(0f, 0f, 0f);
    [SerializeField] private Vector3 divergingEuler = new Vector3(0f, 45f, 0f);

    [SerializeField] private bool startOnDivergingPath;

    public bool IsDivergingActive { get; private set; }
    public TrackManager ActiveTrack => IsDivergingActive ? divergingTrack : straightTrack;

    private void Awake()
    {
        Collider trigger = GetComponent<Collider>();
        trigger.isTrigger = true;

        IsDivergingActive = startOnDivergingPath;
        RefreshIndicator();
    }

    public void SwitchTrack()
    {
        IsDivergingActive = !IsDivergingActive;
        RefreshIndicator();
    }

    private void OnTriggerEnter(Collider other)
    {
        TrainController train = other.GetComponentInParent<TrainController>();
        if (train == null)
        {
            return;
        }

        TrackManager targetTrack = ActiveTrack;
        if (targetTrack != null)
        {
            train.SetTrackManager(targetTrack, true);
        }
    }

    private void RefreshIndicator()
    {
        if (indicatorVisual == null)
        {
            return;
        }

        indicatorVisual.localRotation = Quaternion.Euler(IsDivergingActive ? divergingEuler : straightEuler);
    }
}
