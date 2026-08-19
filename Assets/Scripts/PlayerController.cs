using System;
using UnityEngine;

/// <summary>
/// Lightweight mobile first-person controller using externally supplied move/look input.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraPivot;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 3.5f;
    [SerializeField, Min(0f)] private float gravity = 20f;

    [Header("Look")]
    [SerializeField, Min(0f)] private float lookSensitivity = 0.15f;
    [SerializeField, Range(0f, 89f)] private float lookPitchLimit = 80f;

    [Header("Runtime Input")]
    public Vector2 moveInput;
    public Vector2 lookInput;

    public event Action<PlayerController> InteractPressed;

    private CharacterController _characterController;
    private Transform _cachedTransform;
    private float _verticalVelocity;
    private float _pitch;
    private bool _movementEnabled = true;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _cachedTransform = transform;

        if (cameraPivot == null && Camera.main != null)
        {
            cameraPivot = Camera.main.transform;
        }

        if (cameraPivot != null)
        {
            _pitch = NormalizePitch(cameraPivot.localEulerAngles.x);
        }
    }

    private void Update()
    {
        ApplyLook(Time.deltaTime);
        ApplyMovement(Time.deltaTime);
    }

    public void SetMoveInput(Vector2 value)
    {
        moveInput = Vector2.ClampMagnitude(value, 1f);
    }

    public void SetLookInput(Vector2 value)
    {
        lookInput = value;
    }

    public void SetMovementEnabled(bool enabled)
    {
        _movementEnabled = enabled;

        if (!enabled)
        {
            moveInput = Vector2.zero;
            _verticalVelocity = 0f;
        }
    }

    public void TriggerInteract()
    {
        InteractPressed?.Invoke(this);
    }

    public void TeleportTo(Vector3 worldPosition, Quaternion worldRotation)
    {
        bool wasEnabled = _characterController.enabled;
        _characterController.enabled = false;
        _cachedTransform.SetPositionAndRotation(worldPosition, worldRotation);
        _characterController.enabled = wasEnabled;
        _verticalVelocity = 0f;
    }

    private void ApplyLook(float deltaTime)
    {
        if (lookInput.sqrMagnitude <= 0f)
        {
            return;
        }

        float yawDelta = lookInput.x * lookSensitivity;
        float pitchDelta = lookInput.y * lookSensitivity;

        _cachedTransform.Rotate(0f, yawDelta, 0f, Space.Self);

        if (cameraPivot != null)
        {
            _pitch = Mathf.Clamp(_pitch - pitchDelta, -lookPitchLimit, lookPitchLimit);
            cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        lookInput = Vector2.zero;
    }

    private void ApplyMovement(float deltaTime)
    {
        if (_characterController.isGrounded && _verticalVelocity < 0f)
        {
            _verticalVelocity = -1f;
        }

        if (_movementEnabled)
        {
            Vector3 right = _cachedTransform.right;
            Vector3 forward = _cachedTransform.forward;
            right.y = 0f;
            forward.y = 0f;
            right.Normalize();
            forward.Normalize();

            Vector3 lateralMove = (right * moveInput.x) + (forward * moveInput.y);
            if (lateralMove.sqrMagnitude > 1f)
            {
                lateralMove.Normalize();
            }

            _characterController.Move(lateralMove * (moveSpeed * deltaTime));
        }

        _verticalVelocity -= gravity * deltaTime;
        _characterController.Move(Vector3.up * (_verticalVelocity * deltaTime));
    }

    private static float NormalizePitch(float rawPitch)
    {
        return rawPitch > 180f ? rawPitch - 360f : rawPitch;
    }
}
