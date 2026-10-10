using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PortalInteractionPrompt : MonoBehaviour
{
    [Header("Prompt UI")]
    [SerializeField] private GameObject _promptVisual;
    [SerializeField] private Image _progressImage;

    private InteractablePortal _portal;
    private PlayerInteractionModule _playerInteraction;

    // 플레이어에 Collider가 여러 개 있을 수 있어서, 범위 안의 Collider들을 따로 추적합니다.
    private readonly HashSet<Collider2D> _playerColliders = new HashSet<Collider2D>();

    private void Awake()
    {
        // 이 스크립트가 포탈 루트에 붙으므로 같은 오브젝트의 포탈 컴포넌트를 가져옵니다.
        _portal = GetComponent<InteractablePortal>();

        ResetProgress();
    }

    private void OnEnable()
    {
        // 포탈이 활성화되어 있는 동안에는 플레이어 거리와 관계없이 F키 안내를 표시합니다.
        if (_promptVisual != null)
        {
            _promptVisual.SetActive(true);
        }

        ResetProgress();
    }

    private void OnDisable()
    {
        // 포탈을 다시 활성화할 때 이전 플레이어 감지 정보와 진행도가 남지 않도록 정리합니다.
        _playerColliders.Clear();
        _playerInteraction = null;
        ResetProgress();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        RegisterPlayer(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // 범위 안에 있는 동안 플레이어 모듈을 다시 확인합니다.
        RegisterPlayer(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!_playerColliders.Remove(other))
        {
            return;
        }

        // 범위를 벗어나면 진행도만 초기화하고 F키 안내는 계속 표시합니다.
        if (_playerColliders.Count == 0)
        {
            _playerInteraction = null;
            ResetProgress();
        }
    }

    private void Update()
    {
        if (_portal == null || _playerInteraction == null || !_playerInteraction.isActiveAndEnabled)
        {
            ResetProgress();
            return;
        }

        if (_progressImage == null)
        {
            return;
        }

        // 안내 표시와 실제 상호작용 범위는 별개입니다.
        // 기존 모듈이 이 포탈을 홀드 중일 때만 실제 진행도를 테두리에 반영합니다.
        bool isHoldingThisPortal =
            _playerInteraction.IsInteracting &&
            ReferenceEquals(_playerInteraction.CurrentTarget, _portal);

        _progressImage.fillAmount =
            isHoldingThisPortal ? _playerInteraction.InteractionProgress : 0f;
    }

    private void RegisterPlayer(Collider2D other)
    {
        // Collider가 플레이어 자식 오브젝트에 있어도 부모에서 PlayerController를 찾습니다.
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
        {
            return;
        }

        // 현재 프로젝트에서는 PlayerInteractionModule이 플레이어 루트에 있습니다.
        PlayerInteractionModule interaction =
            player.GetComponent<PlayerInteractionModule>();

        if (interaction == null)
        {
            return;
        }

        _playerColliders.Add(other);
        _playerInteraction = interaction;
    }

    private void ResetProgress()
    {
        if (_progressImage != null)
        {
            _progressImage.fillAmount = 0f;
        }
    }
}
