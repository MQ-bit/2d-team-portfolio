using Interactable;
using UnityEngine;

public class InteractablePortal : MonoBehaviour, IInteractable
{
    [Header("Portal Settings")]
    [SerializeField] private Transform _destination;

    [Header("Visual Effect Settings")]
    [SerializeField] private ParticleSystem _effectOnPlayerEnter;
    [SerializeField] private ParticleSystem _teleportEffect;

    public Transform Destination => _destination;

    private void Awake()
    {
        if (_destination == null)
        {
            _destination = transform.GetChild(0);
        }
    }

    public bool CanInteract(GameObject interactor)
    {
        return _destination != null
            && _destination != transform;
    }

    public void Interact(GameObject interactor)
    {
        if (!CanInteract(interactor))
        {
            return;
        }

        if (interactor.TryGetComponent(out PlayerController playerController))
        {
            playerController.CancelAllActions();
        }
        if (interactor.TryGetComponent(out Rigidbody2D rigid))
        {
            rigid.linearVelocity = Vector2.zero;
            rigid.position = new Vector2(_destination.position.x, _destination.position.y);
        }
        _teleportEffect?.Play();

        SceneVisualEffectManager.Instance.TryFadeOut(0.25f);
    }
}
