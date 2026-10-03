using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Module References")]
    [SerializeField] private InputReader _inputSO;
    [SerializeField] private PlayerMovementModule _movementModule;
    [SerializeField] private PlayerThrowingModule _throwingModule;
    [SerializeField] private PlayerInventoryModule _inventoryModule;
    [SerializeField] private PlayerDrinkingModule _drinkingModule;
    [SerializeField] private PlayerMeleeModule _meleeModule;
    [SerializeField] private PlayerInteractionModule _interactionModule;
    [SerializeField] private PlayerStatus _playerStatus;

    public bool CanReadInput { get; private set; } = true;

    // UI 연결을 위한 프로퍼티
    public PlayerMovementModule MovementModule => _movementModule;
    public PlayerInventoryModule InventoryModule => _inventoryModule;
    public PlayerMeleeModule MeleeModule => _meleeModule;
    public PlayerInteractionModule InteractionModule => _interactionModule;
    public PlayerStatus PlayerStatus => _playerStatus;

    public Vector2 ReadableMoveInput => CanReadInput ? _inputSO.MoveInput : Vector2.zero;

    private void Awake()
    {
        GetModules();
    }

    private void GetModules()
    {
        if (_inputSO == null)
        {
            Debug.LogError($"{gameObject.name} : PlayerInputSO가 할당 안됨");
        }
        if (_movementModule == null)
        {
            _movementModule = GetComponent<PlayerMovementModule>();
        }
        if (_throwingModule == null)
        {
            _throwingModule = GetComponent<PlayerThrowingModule>();
        }
        if (_inventoryModule == null)
        {
            _inventoryModule = GetComponent<PlayerInventoryModule>();
        }
        if (_drinkingModule == null)
        {
            _drinkingModule = GetComponent<PlayerDrinkingModule>();
        }
        if (_meleeModule == null)
        {
            _meleeModule = GetComponent<PlayerMeleeModule>();
        }
        if (_interactionModule == null)
        {
            _interactionModule = GetComponent<PlayerInteractionModule>();
        }
        if (_playerStatus == null)
        {
            _playerStatus = GetComponent<PlayerStatus>();
        }
    }

    private void OnEnable()
    {
        LinkInputEvents();
    }

    private void OnDisable()
    {
        UnLinkInputEvents();
        _throwingModule?.CancelAim();
        _interactionModule?.CancelInteraction();
    }

    private void LinkInputEvents()
    {
        _inputSO.OnMoveInput += HandleDashInput;
        _inputSO.OnPrimaryActionStart += HandlePrimaryActionStart;
        _inputSO.OnPrimaryActionEnd += HandlePrimaryActionEnd;
        _inputSO.OnItemSlotChanged += HandleItemSlotChanged;
        _inputSO.OnDrinkInputChanged += HandleDrinkInputChanged;
        _inputSO.OnMeleeToggleRequested += HandleMeleeToggleRequested;
        _inputSO.OnInteractInputChanged += HandleInteractInputChanged;
    }

    private void UnLinkInputEvents()
    {
        _inputSO.OnMoveInput -= HandleDashInput;
        _inputSO.OnPrimaryActionStart -= HandlePrimaryActionStart;
        _inputSO.OnPrimaryActionEnd -= HandlePrimaryActionEnd;
        _inputSO.OnItemSlotChanged -= HandleItemSlotChanged;
        _inputSO.OnDrinkInputChanged -= HandleDrinkInputChanged;
        _inputSO.OnMeleeToggleRequested -= HandleMeleeToggleRequested;
        _inputSO.OnInteractInputChanged -= HandleInteractInputChanged;
    }

    private void FixedUpdate()
    {
        if (!CanReadInput) return;
        HandleMoveInput();
    }

    private void Update()
    {
        if (!CanReadInput) return;

        if (_throwingModule.IsAiming)
        {
            _throwingModule.UpdateAim(_inputSO.MousePosition);
        }
        else if (_meleeModule.IsMeleeModeActivated)
        {
            _meleeModule.UpdateIndicator(_inputSO.MousePosition);
        }
    }

    private void HandleMoveInput()
    {
        if (!CanReadInput || _meleeModule.IsSwinging) return;

        _movementModule.MovePosition(_inputSO.MoveInput);
    }

    private void HandleDashInput()
    {
        if (!CanReadInput) return;

        Vector2 moveInput = _inputSO.MoveInput;

        if (moveInput == Vector2.zero)
        {
            return;
        }

        if (!_movementModule.TryStartDash(moveInput))
        {
            return;
        }
        CancelActionsConfilictWithDash();
    }

    private void HandleItemSlotChanged(int newSlotIndex)
    {
        if (!CanReadInput || _drinkingModule.IsDrinking)
        {
            return;
        }

        _inventoryModule.SelectSlot(newSlotIndex);
    }

    private void HandleDrinkInputChanged(bool isHolding)
    {
        if (!isHolding)
        {
            _drinkingModule.CancelDrink();
            return;
        }

        if (_throwingModule.IsAiming || _movementModule.IsDashing || _meleeModule.IsSwinging)
        {
            return;
        }

        _drinkingModule.TryBeginDrink();
    }

    private void HandlePrimaryActionStart(Vector2 mousePosition)
    {
        if (!CanReadInput || _drinkingModule.IsDrinking) return;

        if (_meleeModule.IsMeleeModeActivated)
        {
            if (_movementModule.IsDashing) return;

            _meleeModule.TryAttack(mousePosition);
            return;
        }
        else //potionThrow
        {
            if (_meleeModule.IsSwinging) return;
            _throwingModule.BeginAim(mousePosition);
        }
    }

    private void HandlePrimaryActionEnd(Vector2 mousePosition)
    {
        if (!CanReadInput) return;

        _throwingModule.ReleaseThrow(mousePosition);
    }

    private void HandleMeleeToggleRequested()
    {
        if (!CanReadInput || !_meleeModule.TryToggleMeleeMode())
        {
            return;
        }

        if (_meleeModule.IsMeleeModeActivated)
        {
            _throwingModule.CancelAim();
        }
    }

    public void SetGamePlayInputEnabled(bool isEnabled)
    {
        if (CanReadInput == isEnabled) return;

        CanReadInput = isEnabled;
        if (!CanReadInput)
        {
            CancelAllActions();
        }
    }

    private void CancelActionsConfilictWithDash()
    {
        _throwingModule.CancelAim();
        _drinkingModule.CancelDrink();
        _meleeModule.CancelSwing();
    }
    
    public void CancelAllActions()
    {
        CancelActionsConfilictWithDash();
        _movementModule.CancelDash();
        _interactionModule.CancelInteraction();
    }

    private void HandleInteractInputChanged(bool isHolding)
    {
        if (_interactionModule == null) return;
        if (!isHolding)
        {
            _interactionModule.CancelInteraction();
        }
        else
        {
            if (!CanReadInput) return;
            _interactionModule.TryBeginInteraction();            
        }
    }
}
