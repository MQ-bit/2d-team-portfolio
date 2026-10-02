using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using static UnityEngine.InputSystem.InputAction;

[CreateAssetMenu(fileName = "InputReader", menuName = "Scriptable Objects/InputReaderSO")]
public class InputReader : ScriptableObject, PlayerInput.IPlayerActions
{
    private PlayerInput _playerInput;

    private Vector2 _moveInput = Vector2.zero;
    private Vector2 _mousePosition = Vector2.zero;
    private bool _isPrimaryActionHolding = false;
    private bool _isDrinkingHolding = false;

    //Properties
    public Vector2 MoveInput => _moveInput;
    public Vector2 MousePosition => _mousePosition;
    public bool IsPrimaryActionHolding => _isPrimaryActionHolding;
    public bool IsDrinkingHolding => _isDrinkingHolding;

    //Events
    public event Action OnMoveInput;
    public event Action<Vector2> OnPrimaryActionStart;
    public event Action<Vector2> OnPrimaryActionEnd;
    public event Action<int> OnItemSlotChanged;
    public event Action<bool> OnDrinkInputChanged;
    public event Action OnMeleeToggleRequested;
    public event Action OnPotionInformationTabToggled;

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
        _isPrimaryActionHolding = false;
        _isDrinkingHolding = false;
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

    public void OnPrimaryAction(CallbackContext context)
    {
        if (context.performed)
        {
            if (_isPrimaryActionHolding) return;

            _isPrimaryActionHolding = true;
            UpdateMousePosition();

            OnPrimaryActionStart?.Invoke(_mousePosition);
            return;
        }

        if (context.canceled && _isPrimaryActionHolding)
        {
            UpdateMousePosition();
            _isPrimaryActionHolding = false;
            OnPrimaryActionEnd?.Invoke(_mousePosition);
        }
    }

    private void UpdateMousePosition()
    {
        _mousePosition = _playerInput.Player.AimPosition.ReadValue<Vector2>();
    }

    public void OnItemSlotChange(CallbackContext context)
    {
        if (context.performed && context.control is KeyControl keyControl)
        {
            int slotIndex = keyControl.keyCode switch
            {
                Key.Digit1 => 0,
                Key.Digit2 => 1,
                Key.Digit3 => 2,
                Key.Digit4 => 3,
                Key.Digit5 => 4,
                Key.Digit6 => 5,
                _ => -1
            };

            if (slotIndex != -1)
            {
                OnItemSlotChanged?.Invoke(slotIndex);
            }
        }
    }

    public void OnPotionDrink(CallbackContext context)
    {
        if (context.performed)
        {
            if (_isDrinkingHolding) return;

            _isDrinkingHolding = true;
            OnDrinkInputChanged?.Invoke(true);
            return;
        }

        if (context.canceled && _isDrinkingHolding)
        {
            _isDrinkingHolding = false;
            OnDrinkInputChanged?.Invoke(false);
        }
    }

    public void OnToggleAttackMode(CallbackContext context)
    {
        if (context.performed)
        {
            OnMeleeToggleRequested?.Invoke();
        }
    }

    public void OnToggleInformationTab(CallbackContext context)
    {
        if (context.performed)
        {
            OnPotionInformationTabToggled?.Invoke();
        }
    }
}