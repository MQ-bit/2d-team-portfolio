using Interactable;
using UnityEngine;

public class InteractablePortal : MonoBehaviour, IInteractable
{
    [Header("Visual Effect Settings")]
    [SerializeField] private ParticleSystem _effectOnPlayerEnter;
    [SerializeField] private ParticleSystem _teleportEffect;

    public bool CanInteract(GameObject interactor)
    {
        if (interactor == null || !interactor.TryGetComponent(out PlayerController _))
        {
            return false;
        }

        LevelManager levelManager = LevelManager.Instance;

        return levelManager != null && levelManager.CanCompleteStage;
    }

    public void Interact(GameObject interactor)
    {
        if (!CanInteract(interactor))
        {
            return;
        }

        _effectOnPlayerEnter?.Stop();
        _teleportEffect?.Play();

        LevelManager.Instance.TryCompleteStage();
    }
}
