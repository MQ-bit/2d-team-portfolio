using UnityEngine;

// 플레이어 행동 소리: 근접 공격을 휘두르면(PlayerMeleeModule.OnAttackStarted) 적중 여부와 상관없이 휘두르는 소리를,
// 근접 공격이 적에게 적중하면(PlayerMeleeModule.OnMeleeHit) 타격 소리를 한 번 낸다.
// 대시(구르기)를 시작하면(PlayerMovementModule.OnDashStarted) 구르는 소리를 한 번 낸다.
// 걷는 동안에는(PlayerAnimationDriver.IsWalking) 발소리를 일정 간격으로 반복한다.
// 포션을 획득하면(PlayerInventoryModule.OnItemTaken) 획득 소리를 한 번 낸다.
// 포션을 마시기 시작하면(PlayerDrinkingModule.OnDrinkStateChanged) 마시는 소리를 내고, 도중에 취소하면 끊는다.
public class PlayerActionSound : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private PlayerMeleeModule _meleeModule;
    [SerializeField] private PlayerMovementModule _movementModule;
    [SerializeField] private PlayerInventoryModule _inventoryModule;
    [SerializeField] private PlayerDrinkingModule _drinkingModule;
    [SerializeField] private PlayerAnimationDriver _animationDriver; // 걷는 애니메이션이 나오는 동안만 발소리를 내기 위함

    [Header("Melee Attack")]
    [SerializeField] private AudioClip _attackSwingSound;
    [SerializeField, Range(0f, 1f)] private float _attackSwingVolume = 1f;

    [Header("Melee Hit")]
    [SerializeField] private AudioClip _meleeHitSound;
    [SerializeField, Range(0f, 1f)] private float _meleeHitVolume = 1f;

    [Header("Dash")]
    [SerializeField] private AudioClip _dashSound;
    [SerializeField, Range(0f, 1f)] private float _dashVolume = 1f;
    [Tooltip("클립 앞부분을 이만큼(초) 건너뛰고 재생한다. Dash_roll_3은 가장 큰 소리가 0.35초 지점에 있어서 그 직전부터 재생해야 늦게 들리지 않는다.")]
    [SerializeField, Min(0f)] private float _dashStartOffset = 0.34f;
    [Tooltip("클립의 이 지점(초)에서 재생을 끝낸다. Dash_roll_3은 0.65초 이후에 두 번째 소리가 있어서 그 전에 끊는다. 0이면 끝까지 재생한다.")]
    [SerializeField, Min(0f)] private float _dashEndTime = 0.62f;
    [Tooltip("끝나기 전 이 시간(초) 동안 서서히 줄여서 뚝 끊기지 않게 한다.")]
    [SerializeField, Min(0f)] private float _dashFadeOutTime = 0.08f;

    [Header("Item Pickup")]
    [SerializeField] private AudioClip _pickupSound;
    [SerializeField, Range(0f, 1f)] private float _pickupVolume = 1f;
    [Tooltip("클립 앞부분을 이만큼(초) 건너뛰고 재생한다.")]
    [SerializeField, Min(0f)] private float _pickupStartOffset = 0.06f;
    [Tooltip("클립의 이 지점(초)에서 재생을 끝낸다. Bell_tink_1은 0.6초 이후에 두 번째 소리가 있어서 그 전에 끊는다. 0이면 끝까지 재생한다.")]
    [SerializeField, Min(0f)] private float _pickupEndTime = 0.5f;
    [Tooltip("끝나기 전 이 시간(초) 동안 서서히 줄여서 뚝 끊기지 않게 한다.")]
    [SerializeField, Min(0f)] private float _pickupFadeOutTime = 0.2f;

    [Header("Drink")]
    [SerializeField] private AudioClip _drinkSound;
    [SerializeField, Range(0f, 1f)] private float _drinkVolume = 1f;
    [Tooltip("원본이 작게 녹음된 소리를 키우는 배율. Volume은 1이 최대라서 그보다 키울 때 쓴다. 1이면 원본 그대로.")]
    [SerializeField, Min(1f)] private float _drinkGain = 8f;
    [Tooltip("클립 앞부분을 이만큼(초) 건너뛰고 재생한다.")]
    [SerializeField, Min(0f)] private float _drinkStartOffset = 0.05f;
    [Tooltip("클립의 이 지점(초)에서 재생을 끝낸다. 마시는 시간(0.5초)이 끝나는 지점(시작 오프셋 + 0.5초)에서 끊는다. 0이면 끝까지 재생한다.")]
    [SerializeField, Min(0f)] private float _drinkEndTime = 0.55f;
    [Tooltip("끝나기 전 이 시간(초) 동안 서서히 줄여서 뚝 끊기지 않게 한다.")]
    [SerializeField, Min(0f)] private float _drinkFadeOutTime = 0.05f;

    [Header("Footstep")]
    [SerializeField] private AudioClip _footstepSound;
    [SerializeField, Range(0f, 1f)] private float _footstepVolume = 1f;
    [Tooltip("원본이 작게 녹음된 소리를 키우는 배율. Volume은 1이 최대라서 그보다 키울 때 쓴다. 1이면 원본 그대로.")]
    [SerializeField, Min(1f)] private float _footstepGain = 1f;
    [Tooltip("발소리 사이의 간격(초)")]
    [SerializeField, Min(0.05f)] private float _footstepInterval = 0.35f;
    [Tooltip("클립 앞부분을 이만큼(초) 건너뛰고 재생한다.")]
    [SerializeField, Min(0f)] private float _footstepStartOffset = 0.04f;

    private SoundHandle _drinkHandle;
    private float _nextFootstepTime;

    private void Awake()
    {
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

        // 처음 재생할 때 로딩하느라 소리가 늦지 않도록 미리 읽어둔다.
        SoundManager.Preload(_attackSwingSound);
        SoundManager.Preload(_meleeHitSound);
        SoundManager.Preload(_dashSound);
        SoundManager.Preload(_pickupSound);
        SoundManager.Preload(_drinkSound, _drinkGain);
        SoundManager.Preload(_footstepSound, _footstepGain);
    }

    private void OnEnable()
    {
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
        if (_footstepSound == null || _animationDriver == null)
        {
            return;
        }

        if (!_animationDriver.IsWalking)
        {
            // 다시 걷기 시작하면 첫 발소리가 바로 나게 한다.
            _nextFootstepTime = 0f;
            return;
        }

        if (Time.time < _nextFootstepTime)
        {
            return;
        }

        _nextFootstepTime = Time.time + _footstepInterval;
        SoundManager.Play(_footstepSound, SoundChannel.Player, _footstepVolume,
            gain: _footstepGain, startOffset: _footstepStartOffset);
    }

    private void HandleAttackStarted(Vector2 attackDirection)
    {
        SoundManager.Play(_attackSwingSound, SoundChannel.Player, _attackSwingVolume);
    }

    private void HandleMeleeHit(Vector2 attackDirection, bool hasKilled)
    {
        SoundManager.Play(_meleeHitSound, SoundChannel.Player, _meleeHitVolume);
    }

    private void HandleDashStarted(float dashCooltime)
    {
        SoundManager.Play(_dashSound, SoundChannel.Player, _dashVolume,
            startOffset: _dashStartOffset, endTime: _dashEndTime, fadeOutTime: _dashFadeOutTime);
    }

    private void HandleItemTaken()
    {
        SoundManager.Play(_pickupSound, SoundChannel.Player, _pickupVolume,
            startOffset: _pickupStartOffset, endTime: _pickupEndTime, fadeOutTime: _pickupFadeOutTime);
    }

    private void HandleDrinkStateChanged(bool isDrinking)
    {
        if (isDrinking)
        {
            _drinkHandle = SoundManager.Play(_drinkSound, SoundChannel.Player, _drinkVolume, gain: _drinkGain,
                startOffset: _drinkStartOffset, endTime: _drinkEndTime, fadeOutTime: _drinkFadeOutTime);
            return;
        }

        // 다 마셨으면 남은 끝부분은 마저 재생하고, 도중에 취소했으면 바로 끊는다.
        bool isNearEnd = _drinkEndTime > 0f && _drinkHandle.Time >= _drinkEndTime - 0.1f;
        if (!isNearEnd)
        {
            _drinkHandle.Stop();
        }
    }
}
