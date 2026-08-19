using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Bridges mobile UI controls (joystick, drag look, and interact button) to PlayerController.
/// </summary>
public class PlayerMobileInput : MonoBehaviour, IDragHandler, IEndDragHandler
{
    [SerializeField] private PlayerController playerController;
    [SerializeField, Min(0f)] private float lookDragScale = 1f;

    private Vector2 _moveInput;
    private Vector2 _lookInput;

    private void Awake()
    {
        if (playerController == null)
        {
            playerController = FindObjectOfType<PlayerController>();
        }
    }

    private void Update()
    {
        if (playerController == null)
        {
            return;
        }

        playerController.SetMoveInput(_moveInput);
        playerController.SetLookInput(_lookInput);
        _lookInput = Vector2.zero;
    }

    /// <summary>
    /// Called by joystick UI systems that expose a full Vector2 axis.
    /// </summary>
    public void SetMoveInput(Vector2 value)
    {
        _moveInput = Vector2.ClampMagnitude(value, 1f);
    }

    /// <summary>
    /// Called by slider/events that expose horizontal movement independently.
    /// </summary>
    public void SetMoveX(float value)
    {
        _moveInput.x = Mathf.Clamp(value, -1f, 1f);
    }

    /// <summary>
    /// Called by slider/events that expose vertical movement independently.
    /// </summary>
    public void SetMoveY(float value)
    {
        _moveInput.y = Mathf.Clamp(value, -1f, 1f);
    }

    /// <summary>
    /// Optional method if the drag region sends explicit look deltas via UnityEvent.
    /// </summary>
    public void AddLookDelta(Vector2 delta)
    {
        _lookInput += delta * lookDragScale;
    }

    public void OnDrag(PointerEventData eventData)
    {
        AddLookDelta(eventData.delta);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _lookInput = Vector2.zero;
    }

    public void OnInteractButtonPressed()
    {
        if (playerController != null)
        {
            playerController.TriggerInteract();
        }
    }
}
