using Interactable;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class InteractableInteractionPrompt : MonoBehaviour
{
    [Header("Prompt UI")]
    [SerializeField] private GameObject _promptVisual;
    [SerializeField] private Image _progressImage;

    [Header("Visibility")]
    [Tooltip("활성화하면 첫 상호작용 완료 전까지 거리와 CanInteract 상태에 관계없이 표시합니다.")]
    [SerializeField] private bool _alwaysVisible;

    private IInteractable _interactable;
    private MonoBehaviour _interactableBehaviour;

    private PlayerController _nearbyPlayer;
    private PlayerInteractionModule _playerInteraction;
    private GameObject _lastInteractor;

    private Collider2D _playerCollider;

    // 성공적으로 상호작용을 완료한 이후에는 CanInteract 결과를 우선합니다.
    private bool _hasCompletedInteraction;

    private void Awake()
    {
        _interactable = GetComponent<IInteractable>();
        _interactableBehaviour = _interactable as MonoBehaviour;

        if (_interactable == null || _interactableBehaviour == null)
        {
            Debug.LogError($"{name}: InteractionPrompt와 같은 GameObject에 IInteractable 구현 컴포넌트가 필요합니다.");

            SetPromptVisible(false);
            enabled = false;
            return;
        }

        if (_promptVisual == gameObject)
        {
            Debug.LogError($"{name}: Prompt Visual에는 루트가 아닌 자식 오브젝트를 할당해야 합니다.");

            enabled = false;
            return;
        }

        ResetProgress();
    }

    private void OnEnable()
    {
        RefreshPromptVisibility();
        ResetProgress();
    }

    private void OnDisable()
    {
        UnsubscribeFromPlayerInteraction();

        _playerCollider = null;
        _nearbyPlayer = null;
        _playerInteraction = null;

        ResetProgress();
        SetPromptVisible(false);
    }

    private void Update()
    {
        ValidateNearbyPlayer();
        RefreshPromptVisibility();
        RefreshProgress();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        RegisterPlayer(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        RegisterPlayer(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other != _playerCollider)
        {
            return;
        }

        ClearNearbyPlayer();
    }

    private void RegisterPlayer(Collider2D other)
    {
        if (other == null)
        {
            return;
        }

        PlayerController player = other.GetComponentInParent<PlayerController>();

        if (player == null)
        {
            return;
        }

        PlayerInteractionModule interaction = player.GetComponent<PlayerInteractionModule>();

        if (interaction == null)
        {
            return;
        }
        if (_nearbyPlayer != null && _nearbyPlayer != player)
        {
            return;
        }

        _playerCollider = other;
        _lastInteractor = player.gameObject;

        if (_nearbyPlayer == player && _playerInteraction == interaction)
        {
            return;
        }

        UnsubscribeFromPlayerInteraction();

        _nearbyPlayer = player;
        _playerInteraction = interaction;

        SubscribeToPlayerInteraction();
        RefreshPromptVisibility();
    }

    private void ClearNearbyPlayer()
    {
        UnsubscribeFromPlayerInteraction();

        _playerCollider = null;
        _nearbyPlayer = null;
        _playerInteraction = null;

        ResetProgress();
        RefreshPromptVisibility();
    }

    private void SubscribeToPlayerInteraction()
    {
        if (_playerInteraction == null)
        {
            return;
        }

        _playerInteraction.OnInteractionStarted += HandleInteractionStarted;
        _playerInteraction.OnInteractionCancelled += HandleInteractionCancelled;
        _playerInteraction.OnInteractionCompleted += HandleInteractionCompleted;
    }

    private void UnsubscribeFromPlayerInteraction()
    {
        if (_playerInteraction == null)
        {
            return;
        }

        _playerInteraction.OnInteractionStarted -= HandleInteractionStarted;
        _playerInteraction.OnInteractionCancelled -= HandleInteractionCancelled;
        _playerInteraction.OnInteractionCompleted -= HandleInteractionCompleted;
    }

    private void HandleInteractionStarted(IInteractable target)
    {
        if (!ReferenceEquals(target, _interactable))
        {
            return;
        }

        ResetProgress();
        RefreshPromptVisibility();
    }

    private void HandleInteractionCancelled(IInteractable target)
    {
        if (!ReferenceEquals(target, _interactable))
        {
            return;
        }

        ResetProgress();
        RefreshPromptVisibility();
    }

    private void HandleInteractionCompleted(IInteractable target)
    {
        if (!ReferenceEquals(target, _interactable))
        {
            return;
        }
        _hasCompletedInteraction = true;

        ResetProgress();
        RefreshPromptVisibility();
    }

    private void RefreshPromptVisibility()
    {
        SetPromptVisible(ShouldShowPrompt());
    }

    private bool ShouldShowPrompt()
    {
        if (_interactable == null || _interactableBehaviour == null || !_interactableBehaviour.isActiveAndEnabled)
        {
            return false;
        }

        bool playerIsNearby = IsPlayerNearby();

        if (_alwaysVisible && !_hasCompletedInteraction)
        {
            return true;
        }

        if (!playerIsNearby && !_alwaysVisible)
        {
            return false;
        }

        GameObject interactor = playerIsNearby ? _nearbyPlayer.gameObject : _lastInteractor;

        if (interactor == null)
        {
            return false;
        }

        return _interactable.CanInteract(interactor);
    }

    private bool IsPlayerNearby()
    {
        return _nearbyPlayer != null && _playerCollider != null && _playerCollider.enabled && _playerCollider.gameObject.activeInHierarchy;
    }

    private void RefreshProgress()
    {
        if (_progressImage == null)
        {
            return;
        }

        bool isHoldingThisObject =
            _promptVisual != null &&
            _promptVisual.activeSelf &&
            _playerInteraction != null &&
            _playerInteraction.isActiveAndEnabled &&
            _playerInteraction.IsInteracting &&
            ReferenceEquals(_playerInteraction.CurrentTarget, _interactable);

        _progressImage.fillAmount = isHoldingThisObject ? _playerInteraction.InteractionProgress : 0f;
    }

    private void ValidateNearbyPlayer()
    {
        if (_nearbyPlayer == null)
        {
            _playerCollider = null;
            return;
        }

        if (!IsPlayerNearby())
        {
            ClearNearbyPlayer();
        }
    }


    private void SetPromptVisible(bool visible)
    {
        if (_promptVisual != null &&
            _promptVisual != gameObject &&
            _promptVisual.activeSelf != visible)
        {
            _promptVisual.SetActive(visible);
        }
    }

    private void ResetProgress()
    {
        if (_progressImage != null)
        {
            _progressImage.fillAmount = 0f;
        }
    }
}