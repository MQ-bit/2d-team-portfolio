using UnityEngine;
using static UnityEngine.InputSystem.InputAction;

[CreateAssetMenu(fileName = "PlayerInputSO", menuName = "Scriptable Objects/PlayerInputSO")]
public class PlayerInputSO : ScriptableObject, PlayerInput.IPlayerActions
{
    private PlayerInput _playerInput;
    private Vector2 _moveInput = Vector2.zero;

    public Vector2 MoveInput => _moveInput;

    private void OnEnable()
    {
        if(_playerInput == null)
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
}