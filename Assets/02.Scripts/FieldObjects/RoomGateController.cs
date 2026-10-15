using UnityEngine;

public class RoomGateController : MonoBehaviour
{
    [SerializeField] private Collider2D _blockingColliders;
    [SerializeField] private Animator _animator;
    [SerializeField] private MinimapMarker _minimapMarker;

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
        if (_minimapMarker == null)
        {
            _minimapMarker = GetComponentInChildren<MinimapMarker>();
        }
    }

    private void Start()
    {
        SetOpen(true);
    }

    public void SetOpen(bool isOpen)
    {
        IsOpen = isOpen;

        if (isOpen)
        {
            SetCollidersEnabled(false);
            SetDoorOpenAnimation(true);
            _minimapMarker.SetVisible(false);
        }
        else
        {
            SetCollidersEnabled(true);
            SetDoorOpenAnimation(false);
            _minimapMarker.SetVisible(true);
        }
    }

    private void SetDoorOpenAnimation(bool isOpen)
    {
        _animator.SetBool(_isOpenString, isOpen);
    }

    private void SetCollidersEnabled(bool enabled)
    {
        _blockingColliders.enabled = enabled;
    }
}