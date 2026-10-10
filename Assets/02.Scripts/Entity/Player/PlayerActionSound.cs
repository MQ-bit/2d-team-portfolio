using UnityEngine;

// 플레이어 행동 소리: 근접 공격을 휘두르면(PlayerMeleeModule.OnAttackStarted) 적중 여부와 상관없이 휘두르는 소리를,
// 대시(구르기)를 시작하면(PlayerMovementModule.OnDashStarted) 구르는 소리를 한 번 낸다.
// 걷는 동안에는(PlayerAnimationDriver.IsWalking) A/B 발소리를 일정 간격으로 번갈아 재생한다.
// 포션을 획득하면(PlayerInventoryModule.OnItemTaken) 획득 소리를 한 번 낸다.
// 포션을 마시기 시작하면(PlayerDrinkingModule.OnDrinkStateChanged) 마시는 소리를 내고, 도중에 취소하면 끊는다.
public class PlayerActionSound : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private PlayerMeleeModule _meleeModule;
    [SerializeField] private PlayerMovementModule _movementModule;
    [SerializeField] private PlayerInventoryModule _inventoryModule;
    [SerializeField] private PlayerDrinkingModule _drinkingModule;
    [SerializeField] private PlayerStatus _status;
    [SerializeField] private PlayerAnimationDriver _animationDriver; // 걷는 애니메이션이 나오는 동안만 발소리를 내기 위함

    private SoundHandle _drinkHandle;
    private float _nextFootstepTime;
    private bool _playFootstepB;

    private void Awake()
    {
        if (_status == null)
        {
            _status = GetComponent<PlayerStatus>();
        }
        if (_meleeModule == null)
        {
            _meleeModule = GetComponent<PlayerMeleeModule>();
        }
        if (_movementModule == null)
        {
            _movementModule = GetComponent<PlayerMovementModule>();
        }
        if (_inventoryModule == null)
        {
            _inventoryModule = GetComponent<PlayerInventoryModule>();
        }
        if (_drinkingModule == null)
        {
            _drinkingModule = GetComponent<PlayerDrinkingModule>();
        }
        if (_animationDriver == null)
        {
            _animationDriver = GetComponent<PlayerAnimationDriver>();
        }

        foreach (GameSoundId id in new[] { GameSoundId.PlayerSwing, GameSoundId.PlayerMeleeHit,
            GameSoundId.PlayerDash, GameSoundId.PlayerPickup, GameSoundId.PlayerDrink, GameSoundId.PlayerFootstep,
            GameSoundId.PlayerFootstepB, GameSoundId.PlayerDeath })
        {
            SoundManager.Preload(id);
        }
    }

    private void OnEnable()
    {
        if (_status != null)
        {
            _status.OnDeath += HandleDeath;
        }
        if (_meleeModule != null)
        {
            _meleeModule.OnAttackStarted += HandleAttackStarted;
            _meleeModule.OnMeleeHit += HandleMeleeHit;
        }
        if (_movementModule != null)
        {
            _movementModule.OnDashStarted += HandleDashStarted;
        }
        if (_inventoryModule != null)
        {
            _inventoryModule.OnItemTaken += HandleItemTaken;
        }
        if (_drinkingModule != null)
        {
            _drinkingModule.OnDrinkStateChanged += HandleDrinkStateChanged;
        }
    }

    private void OnDisable()
    {
        _nextFootstepTime = 0f;
        _playFootstepB = false;
        if (_status != null)
        {
            _status.OnDeath -= HandleDeath;
        }
        if (_meleeModule != null)
        {
            _meleeModule.OnAttackStarted -= HandleAttackStarted;
            _meleeModule.OnMeleeHit -= HandleMeleeHit;
        }
        if (_movementModule != null)
        {
            _movementModule.OnDashStarted -= HandleDashStarted;
        }
        if (_inventoryModule != null)
        {
            _inventoryModule.OnItemTaken -= HandleItemTaken;
        }
        if (_drinkingModule != null)
        {
            _drinkingModule.OnDrinkStateChanged -= HandleDrinkStateChanged;
        }
    }

    private void Update()
    {
        UpdateFootstep();
    }

    private void UpdateFootstep()
    {
        if (_status != null && _status.IsDead) return;
        if (_animationDriver == null)
        {
            return;
        }

        if (!_animationDriver.IsWalking)
        {
            // 다시 걷기 시작하면 A 발소리부터 바로 재생한다.
            _nextFootstepTime = 0f;
            _playFootstepB = false;
            return;
        }

        if (Time.time < _nextFootstepTime)
        {
            return;
        }

        GameSoundId footstepId = _playFootstepB ? GameSoundId.PlayerFootstepB : GameSoundId.PlayerFootstep;
        GameSoundData.Entry footstep = SoundManager.GetSound(footstepId);
        if (footstep?.Clip == null) return;

        _nextFootstepTime = Time.time + Mathf.Max(0.05f, footstep.Interval);
        footstep.Play();
        _playFootstepB = !_playFootstepB;
    }

    private void HandleDeath()
    {
        // 사망음이 나오기 전에 전투음과 장판의 반복음을 정리한다.
        SoundManager.FadeOutChannel(SoundChannel.Player, 0.15f);
        SoundManager.FadeOutChannel(SoundChannel.Potion, 0.15f);
        SoundManager.FadeOutChannel(SoundChannel.Sfx, 0.15f);
        SoundManager.PlayBgmAfterSound(GameSoundId.PlayerDeath, GameBgmId.GameEnd);
    }

    private void HandleAttackStarted(Vector2 attackDirection)
    {
        if (_status != null && _status.IsDead) return;
        SoundManager.Play(GameSoundId.PlayerSwing);
    }

    private void HandleMeleeHit(Vector2 direction, bool killed)
    {
        if (_status != null && _status.IsDead) return;
        SoundManager.Play(GameSoundId.PlayerMeleeHit);
    }

    private void HandleDashStarted(float dashCooltime)
    {
        if (_status != null && _status.IsDead) return;
        SoundManager.Play(GameSoundId.PlayerDash);
    }

    private void HandleItemTaken()
    {
        if (_status != null && _status.IsDead) return;
        SoundManager.Play(GameSoundId.PlayerPickup);
    }

    private void HandleDrinkStateChanged(bool isDrinking)
    {
        if (isDrinking)
        {
            if (_status != null && _status.IsDead) return;
            _drinkHandle = SoundManager.Play(GameSoundId.PlayerDrink);
            return;
        }

        // 다 마셨으면 남은 끝부분은 마저 재생하고, 도중에 취소했으면 바로 끊는다.
        float endTime = SoundManager.GetSound(GameSoundId.PlayerDrink)?.EndTime ?? 0f;
        bool isNearEnd = endTime > 0f && _drinkHandle.Time >= endTime - 0.1f;
        if (!isNearEnd)
        {
            _drinkHandle.Stop();
        }
    }
}
