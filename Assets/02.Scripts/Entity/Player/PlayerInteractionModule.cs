using UnityEngine;
using System.Collections.Generic; 
using System;
using Interactable;

public class PlayerInteractionModule : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float _holdDuration = 1f;
    private readonly Dictionary<Collider2D, IInteractable> _candidates = new Dictionary<Collider2D, IInteractable>();
    private readonly List<Collider2D> _staleColliders = new List<Collider2D>(); // ExtractInvalidCandidates()에서 제거할 Collider를 임시로 저장하는 용도.

    private IInteractable _currentTarget;
    private MonoBehaviour _currentTargetBehaviour; //보다 확실한 오브젝트의 상태 체크를 위해 저장하는 용도.
    private float _holdTime;

    public IInteractable CurrentTarget => _currentTarget;
    public bool IsInteracting {get; private set;}
    public float HoldDuration => _holdDuration;

    public float InteractionProgress
    {
        get
        {
            if (!IsInteracting)
            {
                return 0f;
            }
            return  Mathf.Clamp01(_holdTime / _holdDuration);
        }
    }

    public event Action<IInteractable> OnInteractionStarted;
    public event Action<IInteractable> OnInteractionCompleted;
    public event Action<IInteractable> OnInteractionCancelled;

    private void Update()
    {
        ClearInvalidCandidates();

        if (!IsInteracting) return;
        if(!IsCurrentTargetValid())
        {
            CancelInteraction();
            return;
        }

        _holdTime += Time.deltaTime;
        if( _holdTime >= _holdDuration)
        {
            CompleteInteraction();
        }
    }

    private void OnDisable()
    {
        CancelInteraction();
        _candidates.Clear();
        _staleColliders.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        RegisterCandidate(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        RegisterCandidate(other); // 모듈이 활성화 되거나 포탈이 활성화 될 때 그것을 보완한다.
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if(!_candidates.TryGetValue(other, out IInteractable interactable)) return;

        _candidates.Remove(other);

        if(IsInteracting && ReferenceEquals(interactable, _currentTarget))
        {
            CancelInteraction();
        }
    }

    public bool TryBeginInteraction()
    {
        if(IsInteracting) return false;
        ClearInvalidCandidates();

        IInteractable target = FindClosestInteractable();
        if(target == null || target is not MonoBehaviour targetBehaviour) return false;

        _currentTarget = target;
        _currentTargetBehaviour = targetBehaviour;
        _holdTime = 0f;
        IsInteracting = true;

        OnInteractionStarted?.Invoke(_currentTarget);
        return true;
    }

    public void CancelInteraction()
    {
        if(!IsInteracting) return;

        IInteractable canceledTarget = _currentTarget;

        ResetInteractionState();
        OnInteractionCancelled?.Invoke(canceledTarget);
    }

    private void CompleteInteraction()
    {
        if(!IsInteracting) return;

        IInteractable completedTarget = _currentTarget;

        ResetInteractionState();
        completedTarget.Interact(gameObject);
        OnInteractionCompleted?.Invoke(completedTarget);
    }

    private void ResetInteractionState()
    {
        IsInteracting = false;
        _holdTime = 0f;
        _currentTarget = null;
        _currentTargetBehaviour = null;
    }

    private void RegisterCandidate(Collider2D other)
    {
        if(other == null || _candidates.ContainsKey(other)) return;

        IInteractable interactable = other.GetComponentInParent<IInteractable>();

        if(interactable is not MonoBehaviour interactableBehaviour || !interactableBehaviour.isActiveAndEnabled) return;
        _candidates.Add(other, interactable);
    }

    private IInteractable FindClosestInteractable()
    {
        IInteractable closestTarget = null;
        float closestDistance = float.PositiveInfinity;
        int closestInstanceId = int.MaxValue;

        foreach(KeyValuePair<Collider2D, IInteractable> pair in _candidates)
        {
            Collider2D candidateCollider = pair.Key;
            IInteractable candidate = pair.Value;

            if(!IsCandidateValid(candidateCollider, candidate) || !candidate.CanInteract(gameObject))
            {
                continue;
            }

            float distance = (candidateCollider.bounds.center - transform.position).sqrMagnitude;
            int instanceId = candidateCollider.GetInstanceID();

            if(distance < closestDistance || (Mathf.Approximately(distance, closestDistance) && instanceId < closestInstanceId))
            {
                closestTarget = candidate;
                closestDistance = distance;
                closestInstanceId = instanceId;
            }
        }   
        return closestTarget;
    }

    private bool IsCurrentTargetValid()
    {
        if(_currentTarget == null || _currentTargetBehaviour == null || !_currentTargetBehaviour.isActiveAndEnabled)
        {
            return false;
        }

        return HasCandidateFor(_currentTarget) && _currentTarget.CanInteract(gameObject);
    }

    private bool HasCandidateFor(IInteractable target)
    {
        foreach(KeyValuePair<Collider2D, IInteractable> pair in _candidates)
        {
            if(IsCandidateValid(pair.Key, pair.Value) && ReferenceEquals(pair.Value, target))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsCandidateValid(Collider2D collider, IInteractable candidate)
    {
        if(collider == null || !collider.enabled||!collider.gameObject.activeInHierarchy)
        {
            return false;
        }

        return candidate is MonoBehaviour behaviour && behaviour != null && behaviour.isActiveAndEnabled;
    }

    private void ClearInvalidCandidates()
    {
        _staleColliders.Clear();
        foreach(KeyValuePair<Collider2D, IInteractable> pair in _candidates)
        {
            if(!IsCandidateValid(pair.Key, pair.Value))
            {
                _staleColliders.Add(pair.Key);
            }
        }

        foreach(Collider2D staleCollider in _staleColliders)
        {
            _candidates.Remove(staleCollider);
        }

        _staleColliders.Clear();
    }


}
