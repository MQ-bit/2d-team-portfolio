using UnityEngine;
using System;
using static UnityEngine.InputSystem.InputAction;

[CreateAssetMenu(fileName = "InputReader", menuName = "Scriptable Objects/InputReaderSO")]
public class InputReader : ScriptableObject, PlayerInput.IPlayerActions //인풋리더라고 이름
{
    private PlayerInput _playerInput;
    private Vector2 _moveInput = Vector2.zero;

    public Vector2 MoveInput => _moveInput;

    public event Action OnMoveInput;

    private void OnEnable()
    {
        if (_playerInput == null)
        {
            _playerInput = new PlayerInput();
        }

        _playerInput.Player.SetCallbacks(this);
        _playerInput.Player.Enable();
    }

    private void OnDisable()
    {
        _playerInput.Player.Disable();
    }

    public void OnMove(CallbackContext context)
    {
        if (context.performed || context.canceled)
        {
            _moveInput = context.ReadValue<Vector2>();
        }
    }

    public void OnDash(CallbackContext context)
    {
        if (context.performed)
        {
            OnMoveInput?.Invoke();
        }
        Debug.Log("Dash input received");
    }
}