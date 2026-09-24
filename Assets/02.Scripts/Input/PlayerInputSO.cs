using UnityEngine;
using System;
using static UnityEngine.InputSystem.InputAction;

[CreateAssetMenu(fileName = "InputReader", menuName = "Scriptable Objects/InputReaderSO")]
public class InputReader : ScriptableObject, PlayerInput.IPlayerActions
{
    private PlayerInput _playerInput;

    private Vector2 _moveInput = Vector2.zero;
    private Vector2 _mousePosition = Vector2.zero;
    private bool _isThrowHolding = false;

    //Properties
    public Vector2 MoveInput => _moveInput;
    public Vector2 MousePosition => _mousePosition;
    public bool IsThrowHolding => _isThrowHolding;

    //Events
    public event Action OnMoveInput;
    public event Action<Vector2> OnThrowStart;
    public event Action<Vector2> OnThrowEnd;

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
        _isThrowHolding = false;
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

    public void OnAimPosition(CallbackContext context)
    {
        if (context.performed)
        {
            _mousePosition = context.ReadValue<Vector2>();
        }
    }

    public void OnThrow(CallbackContext context)
    {
        if (context.performed)
        {
            if (_isThrowHolding) return;

            _isThrowHolding = true;
            UpdateMousePosition();

            OnThrowStart?.Invoke(_mousePosition);
            return;
        }

        if(context.canceled && _isThrowHolding)
        {
            UpdateMousePosition();
            _isThrowHolding = false;
            OnThrowEnd?.Invoke(_mousePosition);
        }
    }

    private void UpdateMousePosition()
    {
        _mousePosition = _playerInput.Player.AimPosition.ReadValue<Vector2>();
    }
}