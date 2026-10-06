using UnityEngine;

public class RoomGateController : MonoBehaviour
{
    [SerializeField] private Collider2D _blockingColliders;
    [SerializeField] private Animator _animator;

    public bool IsOpen { get; private set; }
    private static int _isOpenString = Animator.StringToHash("IsOpen");

    private void Awake()
    {
        if (_blockingColliders == null)
        {
            _blockingColliders = GetComponent<Collider2D>();
        }
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }
    }

    public void SetOpen(bool isOpen)
    {
        IsOpen = isOpen;

        if (isOpen)
        {
            SetCollidersEnabled(false);
            SetDoorOpenAnimation(true);
        }
        else
        {
            SetCollidersEnabled(true);
            SetDoorOpenAnimation(false);
        }
    }

    private void SetDoorOpenAnimation(bool isOpen)
    {
        _animator.SetBool(_isOpenString, isOpen);
    }

    private void SetCollidersEnabled(bool enabled)
    {
        _blockingColliders.enabled = enabled;
        //Physics2D.SyncTransforms();
    }
}