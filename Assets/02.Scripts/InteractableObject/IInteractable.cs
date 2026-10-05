using UnityEngine;


namespace Interactable
{
    public interface IInteractable
    {
        bool CanInteract(GameObject interactor);
        void Interact(GameObject interactor);
    }
}