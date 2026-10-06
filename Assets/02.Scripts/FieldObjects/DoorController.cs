using UnityEngine;

public class RoomGateController : MonoBehaviour
{
    [SerializeField] private Renderer[] _renderers;
    [SerializeField] private Collider2D[] _blockingColliders;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        if (_renderers == null || _renderers.Length == 0)
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
        }

        if (_blockingColliders == null || _blockingColliders.Length == 0)
        {
            _blockingColliders = GetComponentsInChildren<Collider2D>(true);
        }
    }

    public void SetOpen(bool isOpen)
    {
        IsOpen = isOpen;

        if (isOpen)
        {
            SetCollidersEnabled(false);
            SetRenderersEnabled(false);
        }
        else
        {
            SetRenderersEnabled(true);
            SetCollidersEnabled(true);
        }
    }

    private void SetRenderersEnabled(bool enabled)
    {
        foreach (Renderer target in _renderers)
        {
            if (target != null)
            {
                target.enabled = enabled;
            }
        }
    }

    private void SetCollidersEnabled(bool enabled)
    {
        foreach (Collider2D target in _blockingColliders)
        {
            if (target != null)
            {
                target.enabled = enabled;
            }
        }

        Physics2D.SyncTransforms();
    }
}