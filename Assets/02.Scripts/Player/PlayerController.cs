using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Module References")]
    [SerializeField] private InputReader _inputSO;
    [SerializeField] private PlayerMovementModule _movementModule;
    [SerializeField] private PlayerThrowingModule _throwingModule;

    private void Awake()
    {
        if (_inputSO == null)
        {
            Debug.LogError($"{gameObject.name} : PlayerInputSO가 할당안됨");
        }
        if (_movementModule == null)
        {
            _movementModule = GetComponent<PlayerMovementModule>();
            if (_movementModule == null)
            {
                Debug.LogError($"{gameObject.name} : PlayerMovementModule 이 할당안됨");
            }
        }
        if (_throwingModule == null)
        {
            _throwingModule = GetComponent<PlayerThrowingModule>();
            if (_throwingModule == null)
            {
                Debug.LogError($"{gameObject.name} : PlayerThrowingModule 이 할당안됨");
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
        if (_throwingModule != null) // TODO : 함수화
        {
            _throwingModule.CancelAim();
        }
    }

    private void LinkInputEvents()
    {
        _inputSO.OnMoveInput += HandleDashInput;
        _inputSO.OnThrowStart += HandleThrowStarted;
        _inputSO.OnThrowEnd += HandleThrowReleased;
    }

    private void UnLinkInputEvents()
    {
        _inputSO.OnMoveInput -= HandleDashInput;
        _inputSO.OnThrowStart -= HandleThrowStarted;
        _inputSO.OnThrowEnd -= HandleThrowReleased;
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
        _movementModule.TryStartDash(moveInput);
    }

    private void HandleThrowStarted(Vector2 screenPosition)
    {
        _throwingModule.BeginAim(screenPosition);
    }

    private void HandleThrowReleased(Vector2 screenPosition)
    {
        _throwingModule.ReleaseThrow(screenPosition);
    }
}
