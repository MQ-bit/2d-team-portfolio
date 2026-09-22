using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerInputSO _playerInputSO;
    [SerializeField] private PlayerMovementModule _movementModule;

    private void Awake()
    {
        if(_playerInputSO == null)
        {
            Debug.LogError("PlayerInputSO is not assigned in PlayerController.");
        }
        if(_movementModule == null)
        {
            _movementModule = GetComponent<PlayerMovementModule>();
            if(_movementModule == null)
            {
                Debug.LogError("PlayerMovementModule is not assigned and not found on the GameObject.");
            }
        }
    }

    
    private void FixedUpdate()
    {
        HandleMoveInput();
    }

    private void HandleMoveInput()
    {
        _movementModule.MovePosition(_playerInputSO.MoveInput);
    }


}
