using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Module References")]
    [SerializeField] private InputReader _inputSO;
    [SerializeField] private PlayerMovementModule _movementModule;

    private void Awake()
    {
        if (_inputSO == null)
        {
            Debug.LogError("PlayerInputSO is not assigned in PlayerController.");
        }
        if (_movementModule == null)
        {
            _movementModule = GetComponent<PlayerMovementModule>();
            if (_movementModule == null)
            {
                Debug.LogError("PlayerMovementModule is not assigned and not found on the GameObject.");
            }
        }
    }

    private void OnEnable()
    {
        LinkInputEvents();
    }
    private void OnDisable()
    {
        UnLinkInputEvents();
    }

    private void LinkInputEvents()
    {
        _inputSO.OnMoveInput += HandleDashInput;
    }

    private void UnLinkInputEvents()
    {
        _inputSO.OnMoveInput -= HandleDashInput;
    }

    private void FixedUpdate()
    {
        HandleMoveInput();
    }

    private void HandleMoveInput()
    {
        _movementModule.MovePosition(_inputSO.MoveInput);
    }

    private void HandleDashInput()
    {
        Vector2 moveInput = _inputSO.MoveInput;
        if (moveInput == Vector2.zero)
        {
            return;
        }
        _movementModule.TryStartDash(moveInput);
    }
}
