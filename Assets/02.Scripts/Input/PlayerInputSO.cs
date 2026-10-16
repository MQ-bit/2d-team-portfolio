using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using static UnityEngine.InputSystem.InputAction;

[CreateAssetMenu(fileName = "InputReader", menuName = "Scriptable Objects/InputReaderSO")]
public class InputReader : ScriptableObject, PlayerInput.IPlayerActions
{
    private PlayerInput _playerInput;
    private bool _isReplay;

    public static InputReader CreateReplayReader()
    {
        var reader = CreateInstance<InputReader>();
        reader.ReleaseInput();
        reader._isReplay = true;
        reader.name = "Performance replay input";
        return reader;
    }
    public void ReplayState(Vector2 move, Vector2 screenAim)
    {
        if (!_isReplay) throw new InvalidOperationException("Replay requires a runtime replay reader.");
        _moveInput = Vector2.ClampMagnitude(move, 1);
        _mousePosition = screenAim;
    }
    public void ReplayPrimary(bool pressed)
    {
        if (!_isReplay) throw new InvalidOperationException("Not a replay reader.");
        _isPrimaryActionHolding = pressed;
        if (pressed) OnPrimaryActionStart?.Invoke(_mousePosition);
        else OnPrimaryActionEnd?.Invoke(_mousePosition);
    }
    public void ReplaySelectSlot(int slot) { if (_isReplay) OnItemSlotChanged?.Invoke(slot); }
    public void ReplayToggleMelee() { if (_isReplay) OnMeleeToggleRequested?.Invoke(); }
    public void ReplayDash() { if (_isReplay) OnMoveInput?.Invoke(); }

    private Vector2 _moveInput = Vector2.zero;
    private Vector2 _mousePosition = Vector2.zero;
    private bool _isPrimaryActionHolding = false;
    private bool _isDrinkingHolding = false;
    private bool _isInteractHolding = false;

    //Properties
    public Vector2 MoveInput => _moveInput;
    public Vector2 MousePosition => _mousePosition;
    public bool IsPrimaryActionHolding => _isPrimaryActionHolding;
    public bool IsDrinkingHolding => _isDrinkingHolding;
    public bool IsInteractHolding => _isInteractHolding;

    //Events
    public event Action OnMoveInput;
    public event Action<Vector2> OnPrimaryActionStart;
    public event Action<Vector2> OnPrimaryActionEnd;
    public event Action<int> OnItemSlotChanged;

    public event Action<int> OnItemSlotChangedScrolled;

    public event Action<bool> OnDrinkInputChanged;
    public event Action OnMeleeToggleRequested;
    public event Action OnPotionInformationTabToggled;
    public event Action OnPauseToggled;
    public event Action<bool> OnInteractInputChanged;

    private void OnEnable()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
        UnityEditor.EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
#endif
        if (Application.isPlaying)
            InitializeInput();
    }

    private void InitializeInput()
    {
        if (_isReplay || _playerInput != null) return;

        _playerInput = new PlayerInput();
        _playerInput.Player.SetCallbacks(this);
        _playerInput.Player.Enable();
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
#endif
        ReleaseInput();
    }

#if UNITY_EDITOR
    private void HandlePlayModeStateChanged(UnityEditor.PlayModeStateChange state)
    {
        if (state == UnityEditor.PlayModeStateChange.EnteredPlayMode)
            InitializeInput();
        else if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
            ReleaseInput();
    }
#endif

    private void ReleaseInput()
    {
        if (_playerInput != null)
        {
            // Unsubscribe before Disable triggers canceled callbacks during teardown.
            _playerInput.Player.SetCallbacks(null);
            _playerInput.Disable();
            _playerInput.Dispose();
            _playerInput = null;
        }

        _moveInput = Vector2.zero;
        _mousePosition = Vector2.zero;
        _isPrimaryActionHolding = false;
        _isDrinkingHolding = false;

        if (_isInteractHolding)
        {
            _isInteractHolding = false;
            OnInteractInputChanged?.Invoke(false);
        }
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

    public void OnItemSlotChangeScroll(CallbackContext context)
    {
        if (!context.performed) return;

        float scroll = context.ReadValue<float>();
        if (Mathf.Approximately(scroll, 0f)) return;

        int direction = scroll < 0f ? 1 : -1;
        OnItemSlotChangedScrolled?.Invoke(direction);
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

    public void OnPause(CallbackContext context)
    {
        if (context.performed)
        {
            OnPauseToggled?.Invoke();
        }
    }

    public void OnInteractionStart(CallbackContext context)
    {
        if (context.performed)
        {
            if (_isInteractHolding) return;

            _isInteractHolding = true;
            OnInteractInputChanged?.Invoke(true);
            return;
        }

        if (context.canceled && _isInteractHolding)
        {
            _isInteractHolding = false;
            OnInteractInputChanged?.Invoke(false);
        }
    }
}
