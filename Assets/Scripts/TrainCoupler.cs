using UnityEngine;

/// <summary>
/// Handles low-speed wagon coupling and high-speed impact derailment logic.
/// </summary>
[RequireComponent(typeof(Collider))]
public class TrainCoupler : MonoBehaviour
{
    [Header("Coupling")]
    [SerializeField, Min(0f)] private float maxSafeCoupleSpeed = 2f;
    [SerializeField] private bool autoConnectOnContact = true;

    [Header("Air Brake Link")]
    [SerializeField, Min(1f)] private float brakeLagMultiplier = 1.5f;
    [SerializeField, Min(0.1f)] private float brakeResponseSpeed = 2f;

    [Header("Impact")]
    [SerializeField, Min(0f)] private float impactDamageMultiplier = 3f;

    public bool CanConnect => _candidateCoupler != null && _connectedCoupler == null;
    public bool IsConnected => _connectedCoupler != null;

    private TrainCoupler _candidateCoupler;
    private TrainCoupler _connectedCoupler;
    private FixedJoint _joint;
    private Rigidbody _rootBody;

    private TrainController _trainController;
    private LocomotiveStats _locomotiveStats;
    private TrainController _connectedTrainController;

    private void Awake()
    {
        Collider trigger = GetComponent<Collider>();
        trigger.isTrigger = true;

        _rootBody = GetComponentInParent<Rigidbody>();
        _trainController = GetComponentInParent<TrainController>();
        _locomotiveStats = GetComponentInParent<LocomotiveStats>();
    }

    private void Update()
    {
        if (_connectedTrainController == null || _trainController == null)
        {
            return;
        }

        float lag = brakeResponseSpeed / brakeLagMultiplier;
        float targetBrake = _trainController.currentBrake;
        float nextBrake = Mathf.MoveTowards(_connectedTrainController.currentBrake, targetBrake, lag * Time.deltaTime);
        _connectedTrainController.SetBrake(nextBrake);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent(out TrainCoupler otherCoupler) || otherCoupler == this)
        {
            return;
        }

        if (_connectedCoupler != null || otherCoupler._connectedCoupler != null)
        {
            return;
        }

        float relativeSpeed = GetRelativeSpeed(otherCoupler);
        if (relativeSpeed > maxSafeCoupleSpeed)
        {
            HandleHighSpeedImpact(otherCoupler, relativeSpeed);
            return;
        }

        _candidateCoupler = otherCoupler;
        if (autoConnectOnContact)
        {
            ConnectToCandidate();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (_candidateCoupler != null && other.gameObject == _candidateCoupler.gameObject)
        {
            _candidateCoupler = null;
        }
    }

    public bool ConnectToCandidate()
    {
        if (_candidateCoupler == null || _candidateCoupler._connectedCoupler != null || _connectedCoupler != null)
        {
            return false;
        }

        _connectedCoupler = _candidateCoupler;
        _candidateCoupler._connectedCoupler = this;

        Rigidbody otherBody = _candidateCoupler._rootBody;
        if (_rootBody != null && otherBody != null)
        {
            _joint = gameObject.AddComponent<FixedJoint>();
            _joint.connectedBody = otherBody;
            _joint.enableCollision = false;
        }

        if (_trainController != null)
        {
            _connectedTrainController = _candidateCoupler.GetComponentInParent<TrainController>();
        }

        _candidateCoupler = null;
        return true;
    }

    public void Disconnect()
    {
        if (_connectedCoupler == null)
        {
            return;
        }

        if (_joint != null)
        {
            Destroy(_joint);
            _joint = null;
        }

        TrainCoupler previous = _connectedCoupler;
        _connectedCoupler = null;
        _connectedTrainController = null;

        if (previous._connectedCoupler == this)
        {
            previous._connectedCoupler = null;
        }
    }

    private float GetRelativeSpeed(TrainCoupler otherCoupler)
    {
        Vector3 myVelocity = _rootBody != null ? _rootBody.velocity : Vector3.zero;
        Vector3 otherVelocity = otherCoupler._rootBody != null ? otherCoupler._rootBody.velocity : Vector3.zero;

        if (_rootBody == null && _trainController != null)
        {
            myVelocity = _trainController.transform.forward * _trainController.CurrentSpeed;
        }

        if (otherCoupler._rootBody == null && otherCoupler._trainController != null)
        {
            otherVelocity = otherCoupler._trainController.transform.forward * otherCoupler._trainController.CurrentSpeed;
        }

        return (myVelocity - otherVelocity).magnitude;
    }

    private void HandleHighSpeedImpact(TrainCoupler otherCoupler, float relativeSpeed)
    {
        if (_trainController != null)
        {
            _trainController.TriggerDerailment("Coupler collision at unsafe speed.");
        }

        if (otherCoupler._trainController != null)
        {
            otherCoupler._trainController.TriggerDerailment("Coupler collision at unsafe speed.");
        }

        float damage = (relativeSpeed - maxSafeCoupleSpeed) * impactDamageMultiplier;
        _locomotiveStats?.ApplyHullDamage(damage);
        otherCoupler._locomotiveStats?.ApplyHullDamage(damage);
    }
}
