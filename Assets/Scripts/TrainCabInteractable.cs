using UnityEngine;

/// <summary>
/// Lets the player enter/exit the train cab using a single interact action.
/// </summary>
[RequireComponent(typeof(Collider))]
public class TrainCabInteractable : MonoBehaviour
{
    [Header("Cab Positions")]
    [SerializeField] private Transform drivingPosition;
    [SerializeField] private Transform exitPosition;

    [Header("Parenting")]
    [SerializeField] private Transform trainRoot;

    [Header("Interaction")]
    [SerializeField, Min(0.1f)] private float interactDistance = 2f;

    private PlayerController _nearbyPlayer;
    private PlayerController _activeCabPlayer;
    private Transform _playerOriginalParent;
    private float _interactDistanceSqr;

    private void Awake()
    {
        if (trainRoot == null)
        {
            trainRoot = transform.root;
        }

        _interactDistanceSqr = interactDistance * interactDistance;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
        {
            return;
        }

        RegisterNearbyPlayer(player);
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
        {
            return;
        }

        if (_nearbyPlayer == player && _activeCabPlayer != player)
        {
            UnregisterNearbyPlayer();
        }
    }

    private void OnDisable()
    {
        UnregisterNearbyPlayer();

        if (_activeCabPlayer != null)
        {
            _activeCabPlayer.InteractPressed -= HandleInteractPressed;
            _activeCabPlayer = null;
        }
    }

    private void RegisterNearbyPlayer(PlayerController player)
    {
        if (_nearbyPlayer == player)
        {
            return;
        }

        if (_nearbyPlayer != null)
        {
            _nearbyPlayer.InteractPressed -= HandleInteractPressed;
        }

        _nearbyPlayer = player;
        _nearbyPlayer.InteractPressed += HandleInteractPressed;
    }

    private void UnregisterNearbyPlayer()
    {
        if (_nearbyPlayer == null)
        {
            return;
        }

        _nearbyPlayer.InteractPressed -= HandleInteractPressed;
        _nearbyPlayer = null;
    }

    private void HandleInteractPressed(PlayerController player)
    {
        if (_activeCabPlayer == player)
        {
            ExitCab(player);
            return;
        }

        if (player != _nearbyPlayer || !IsPlayerWithinInteractRange(player))
        {
            return;
        }

        EnterCab(player);
    }

    private bool IsPlayerWithinInteractRange(PlayerController player)
    {
        Transform anchor = drivingPosition != null ? drivingPosition : transform;
        return (player.transform.position - anchor.position).sqrMagnitude <= _interactDistanceSqr;
    }

    private void EnterCab(PlayerController player)
    {
        if (drivingPosition == null)
        {
            Debug.LogWarning("TrainCabInteractable requires a driving position transform.", this);
            return;
        }

        _activeCabPlayer = player;
        _playerOriginalParent = player.transform.parent;

        player.SetMovementEnabled(false);
        player.transform.SetParent(trainRoot, true);
        player.TeleportTo(drivingPosition.position, drivingPosition.rotation);
    }

    private void ExitCab(PlayerController player)
    {
        Vector3 targetPosition;
        Quaternion targetRotation;

        if (exitPosition != null)
        {
            targetPosition = exitPosition.position;
            targetRotation = exitPosition.rotation;
        }
        else
        {
            targetPosition = transform.position + transform.right * 1.5f;
            targetRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        }

        player.transform.SetParent(_playerOriginalParent, true);
        player.TeleportTo(targetPosition, targetRotation);
        player.SetMovementEnabled(true);

        _activeCabPlayer = null;
        _playerOriginalParent = null;
    }
}
