using UnityEngine;

// 플레이어 행동 소리: 근접 공격을 휘두르면(PlayerMeleeModule.OnAttackStarted) 적중 여부와 상관없이 휘두르는 소리를,
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
    [SerializeField] private AudioSource _audioSource; // 비워두면 런타임에 생성

    [Header("Melee Attack")]
    [SerializeField] private AudioClip _attackSwingSound;
    [SerializeField, Range(0f, 1f)] private float _attackSwingVolume = 1f;

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
    [Tooltip("클립의 이 지점(초)에서 재생을 끝낸다. Potion_sip_2는 1.1초 이후가 거의 무음인 여운이라 그 전에 끊는다. 0이면 끝까지 재생한다.")]
    [SerializeField, Min(0f)] private float _drinkEndTime = 1.1f;
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

    // 재생 구간을 잘라 쓰는 소리는 다른 소리와 섞이지 않게 따로 둔다.
    private AudioSource _dashSource;
    private AudioSource _pickupSource;
    private AudioSource _drinkSource;
    private AudioSource _footstepSource;
    private float _nextFootstepTime;

    // Gain으로 키운 클립은 런타임에 새로 만든 것이라 직접 정리한다.
    private AudioClip _amplifiedDrinkSound;
    private AudioClip _amplifiedFootstepSound;

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
        if (_audioSource == null)
        {
            _audioSource = CreateAudioSource();
        }
        _dashSource = CreateAudioSource();
        _pickupSource = CreateAudioSource();
        _drinkSource = CreateAudioSource();
        _footstepSource = CreateAudioSource();

        // 처음 재생할 때 로딩하느라 소리가 늦지 않도록 미리 읽어둔다.
        Preload(_attackSwingSound);
        Preload(_dashSound);
        Preload(_pickupSound);
        Preload(_drinkSound);
        Preload(_footstepSound);

        _amplifiedDrinkSound = CreateAmplifiedClip(_drinkSound, _drinkGain);
        _amplifiedFootstepSound = CreateAmplifiedClip(_footstepSound, _footstepGain);
    }

    private void OnDestroy()
    {
        if (_amplifiedDrinkSound != null)
        {
            Destroy(_amplifiedDrinkSound);
        }
        if (_amplifiedFootstepSound != null)
        {
            Destroy(_amplifiedFootstepSound);
        }
    }

    // 샘플 값을 gain배로 키운 복사본을 만든다. 키울 필요가 없거나 샘플을 읽을 수 없으면 null.
    private static AudioClip CreateAmplifiedClip(AudioClip clip, float gain)
    {
        if (clip == null || gain <= 1f)
        {
            return null;
        }

        float[] samples = new float[clip.samples * clip.channels];
        if (!clip.GetData(samples, 0))
        {
            Debug.LogWarning($"{clip.name} : 샘플을 읽을 수 없어 Gain을 적용하지 못함. (Load Type이 Decompress On Load여야 함)");
            return null;
        }

        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] = Mathf.Clamp(samples[i] * gain, -1f, 1f);
        }

        AudioClip amplified = AudioClip.Create($"{clip.name}_Amplified", clip.samples, clip.channels, clip.frequency, false);
        amplified.SetData(samples, 0);
        return amplified;
    }

    private AudioSource CreateAudioSource()
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        return source;
    }

    private static void Preload(AudioClip clip)
    {
        if (clip != null)
        {
            clip.LoadAudioData();
        }
    }

    private void OnEnable()
    {
        if (_meleeModule != null)
        {
            _meleeModule.OnAttackStarted += HandleAttackStarted;
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
        UpdateCutoff(_dashSource, _dashVolume, _dashEndTime, _dashFadeOutTime);
        UpdateCutoff(_pickupSource, _pickupVolume, _pickupEndTime, _pickupFadeOutTime);
        UpdateCutoff(_drinkSource, _drinkVolume, _drinkEndTime, _drinkFadeOutTime);
        UpdateFootstep();
    }

    // 재생 중인 소리를 지정한 지점에서 서서히 줄여 끝낸다.
    private static void UpdateCutoff(AudioSource source, float volume, float endTime, float fadeOutTime)
    {
        if (endTime <= 0f || !source.isPlaying)
        {
            return;
        }

        float remaining = endTime - source.time;
        if (remaining <= 0f)
        {
            source.Stop();
            return;
        }

        float fade = fadeOutTime > 0f ? Mathf.Clamp01(remaining / fadeOutTime) : 1f;
        source.volume = volume * fade;
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
        AudioClip footstepClip = _amplifiedFootstepSound != null ? _amplifiedFootstepSound : _footstepSound;
        PlayFrom(_footstepSource, footstepClip, _footstepVolume, _footstepStartOffset);
    }

    private static void PlayFrom(AudioSource source, AudioClip clip, float volume, float startOffset)
    {
        source.clip = clip;
        source.volume = volume;
        source.time = Mathf.Min(startOffset, Mathf.Max(0f, clip.length - 0.01f));
        source.Play();
    }

    private void HandleAttackStarted(Vector2 attackDirection)
    {
        if (_attackSwingSound == null)
        {
            return;
        }

        _audioSource.PlayOneShot(_attackSwingSound, _attackSwingVolume);
    }

    private void HandleDashStarted(float dashCooltime)
    {
        if (_dashSound == null)
        {
            return;
        }

        PlayFrom(_dashSource, _dashSound, _dashVolume, _dashStartOffset);
    }

    private void HandleItemTaken()
    {
        if (_pickupSound == null)
        {
            return;
        }

        PlayFrom(_pickupSource, _pickupSound, _pickupVolume, _pickupStartOffset);
    }

    private void HandleDrinkStateChanged(bool isDrinking)
    {
        if (_drinkSound == null)
        {
            return;
        }

        if (isDrinking)
        {
            AudioClip drinkClip = _amplifiedDrinkSound != null ? _amplifiedDrinkSound : _drinkSound;
            PlayFrom(_drinkSource, drinkClip, _drinkVolume, _drinkStartOffset);
            return;
        }

        // 다 마셨으면 남은 끝부분은 마저 재생하고, 도중에 취소했으면 바로 끊는다.
        bool isNearEnd = _drinkEndTime > 0f && _drinkSource.time >= _drinkEndTime - 0.2f;
        if (!isNearEnd)
        {
            _drinkSource.Stop();
        }
    }
}
