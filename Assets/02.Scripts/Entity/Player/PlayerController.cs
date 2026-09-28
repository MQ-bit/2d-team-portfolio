using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Module References")]
    [SerializeField] private InputReader _inputSO;
    [SerializeField] private PlayerMovementModule _movementModule;
    [SerializeField] private PlayerThrowingModule _throwingModule;
    [SerializeField] private PlayerInventoryModule _inventoryModule;
    [SerializeField] private PlayerDrinkingModule _drinkingModule;

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
    }

    private void OnEnable()
    {
        LinkInputEvents();
    }

    private void OnDisable()
    {
        UnLinkInputEvents();
        if (_throwingModule != null)
        {
            _throwingModule.CancelAim();
        }
    }

    private void LinkInputEvents()
    {
        _inputSO.OnMoveInput += HandleDashInput;
        _inputSO.OnThrowStart += HandleThrowStarted;
        _inputSO.OnThrowEnd += HandleThrowReleased;
        _inputSO.OnItemSlotChanged += HandleItemSlotChanged;
        _inputSO.OnDrinkInputChanged += HandleDrinkInputChanged;
    }

    private void UnLinkInputEvents()
    {
        _inputSO.OnMoveInput -= HandleDashInput;
        _inputSO.OnThrowStart -= HandleThrowStarted;
        _inputSO.OnThrowEnd -= HandleThrowReleased;
        _inputSO.OnItemSlotChanged -= HandleItemSlotChanged;
        _inputSO.OnDrinkInputChanged -= HandleDrinkInputChanged;
    }

    private void FixedUpdate()
    {
        HandleMoveInput();
    }

    private void Update()
    {
        if (_inputSO.IsThrowHolding)
        {
            _throwingModule.UpdateAim(_inputSO.MousePosition);
        }
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

        if (_movementModule.TryStartDash(moveInput))
        {
            _drinkingModule.CancelDrink();
        }
    }

    private void HandleThrowStarted(Vector2 screenPosition)
    {
        if (_drinkingModule.IsDrinking)
        {
            return;
        }
        _throwingModule.BeginAim(screenPosition);
    }

    private void HandleThrowReleased(Vector2 screenPosition)
    {
        _throwingModule.ReleaseThrow(screenPosition);
    }

    private void HandleItemSlotChanged(int newSlotIndex)
    {
        if (_drinkingModule.IsDrinking)
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

        if (_throwingModule.IsAiming || _movementModule.IsDashing)
        {
            return;
        }

        _drinkingModule.TryBeginDrink();
    }
}
