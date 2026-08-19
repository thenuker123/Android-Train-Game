using UnityEngine;

/// <summary>
/// Lets a nearby on-foot player toggle a junction by pressing interact.
/// </summary>
[RequireComponent(typeof(Collider))]
public class JunctionLeverInteractable : MonoBehaviour
{
    [SerializeField] private RailJunction targetJunction;
    [SerializeField, Min(0.2f)] private float interactDistance = 2f;

    private PlayerController _nearbyPlayer;
    private float _interactDistanceSqr;

    private void Awake()
    {
        _interactDistanceSqr = interactDistance * interactDistance;
        Collider trigger = GetComponent<Collider>();
        trigger.isTrigger = true;
    }

    private void OnDisable()
    {
        UnsubscribeCurrentPlayer();
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
        {
            return;
        }

        if (_nearbyPlayer != null && _nearbyPlayer != player)
        {
            _nearbyPlayer.InteractPressed -= OnPlayerInteract;
        }

        _nearbyPlayer = player;
        _nearbyPlayer.InteractPressed -= OnPlayerInteract;
        _nearbyPlayer.InteractPressed += OnPlayerInteract;
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null || player != _nearbyPlayer)
        {
            return;
        }

        UnsubscribeCurrentPlayer();
    }

    private void OnPlayerInteract(PlayerController player)
    {
        if (targetJunction == null)
        {
            return;
        }

        if ((player.transform.position - transform.position).sqrMagnitude > _interactDistanceSqr)
        {
            return;
        }

        targetJunction.SwitchTrack();
    }

    private void UnsubscribeCurrentPlayer()
    {
        if (_nearbyPlayer == null)
        {
            return;
        }

        _nearbyPlayer.InteractPressed -= OnPlayerInteract;
        _nearbyPlayer = null;
    }
}
