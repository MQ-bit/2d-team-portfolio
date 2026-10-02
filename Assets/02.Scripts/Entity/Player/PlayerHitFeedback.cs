using System.Collections;
using UnityEngine;

// 플레이어 전투 타격감: 근접 공격이 적중하거나(PlayerMeleeModule.OnMeleeHit) 플레이어가 피해를 입으면(PlayerStatus.OnDamaged)
// 히트스톱과 카메라 흔들림을 준다. 피해를 입으면 스프라이트도 잠깐 번쩍인다. 수치는 인스펙터에서 조절한다.
public class PlayerHitFeedback : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private PlayerMeleeModule _meleeModule;
    [SerializeField] private PlayerStatus _status;
    [SerializeField] private SpriteRenderer _spriteRenderer;

    [Header("Melee Hit")]
    [SerializeField, Min(0f)] private float _meleeHitStopDuration = 0.05f;
    [SerializeField, Min(0f)] private float _meleeHitShakeForce = 0.15f;

    [Header("Melee Kill")]
    [SerializeField, Min(0f)] private float _meleeKillHitStopDuration = 0.09f;
    [SerializeField, Min(0f)] private float _meleeKillShakeForce = 0.3f;

    [Header("Player Damaged")]
    [SerializeField, Min(0f)] private float _damagedHitStopDuration = 0.07f;
    [SerializeField, Min(0f)] private float _damagedShakeForce = 0.35f;
    [SerializeField, Min(0f)] private float _minDamageForFeedback = 1f; // 장판 같은 지속 피해의 작은 틱에는 반응하지 않도록
    [SerializeField, Min(0f)] private float _damagedFeedbackCooldown = 0.25f; // 실제 시간 기준

    [Header("Damaged Flash")]
    [SerializeField] private Material _flashMaterial; // SpriteRenderer.color는 곱연산이라 흰색을 낼 수 없어 단색 머티리얼로 잠깐 교체
    [SerializeField, Min(0.01f)] private float _flashDuration = 0.1f;

    private Material _originalMaterial;
    private Coroutine _flashRoutine;

    private float _lastDamagedFeedbackTime = float.NegativeInfinity;

    private void Awake()
    {
        if (_meleeModule == null)
        {
            _meleeModule = GetComponent<PlayerMeleeModule>();
        }
        if (_status == null)
        {
            _status = GetComponent<PlayerStatus>();
        }
        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        if (_spriteRenderer != null)
        {
            _originalMaterial = _spriteRenderer.sharedMaterial;
        }
    }

    private void OnEnable()
    {
        if (_meleeModule != null)
        {
            _meleeModule.OnMeleeHit += HandleMeleeHit;
        }
        if (_status != null)
        {
            _status.OnDamaged += HandleDamaged;
        }
    }

    private void OnDisable()
    {
        if (_meleeModule != null)
        {
            _meleeModule.OnMeleeHit -= HandleMeleeHit;
        }
        if (_status != null)
        {
            _status.OnDamaged -= HandleDamaged;
        }

        StopFlash();
    }

    private void HandleMeleeHit(Vector2 attackDirection, bool hasKilled)
    {
        if (hasKilled)
        {
            Play(_meleeKillHitStopDuration, attackDirection, _meleeKillShakeForce);
        }
        else
        {
            Play(_meleeHitStopDuration, attackDirection, _meleeHitShakeForce);
        }
    }

    // hitDirection(공격이 날아온 진행 방향)이 있으면 그 방향으로 화면이 밀리고, 없으면 무작위 방향으로 흔들린다.
    private void HandleDamaged(float amount, Vector2 hitDirection)
    {
        // 히트스톱 중에는 Time.time이 멈추므로 쿨타임은 실제 시간으로 잰다.
        if (amount < _minDamageForFeedback || Time.unscaledTime - _lastDamagedFeedbackTime < _damagedFeedbackCooldown)
        {
            return;
        }

        _lastDamagedFeedbackTime = Time.unscaledTime;
        Play(_damagedHitStopDuration, hitDirection, _damagedShakeForce);
        Flash();
    }

    private void Play(float hitStopDuration, Vector2 shakeDirection, float shakeForce)
    {
        HitStop.Request(hitStopDuration);

        if (CameraController.Instance != null)
        {
            CameraController.Instance.ShakeCamera(shakeDirection, shakeForce);
        }
    }

    private void Flash()
    {
        if (_spriteRenderer == null || _flashMaterial == null)
        {
            return;
        }

        StopFlash();
        _flashRoutine = StartCoroutine(FlashCoroutine());
    }

    // 게임 시간 기준으로 기다려서 히트스톱으로 멈춘 동안에는 계속 번쩍인 채로 보이게 한다.
    private IEnumerator FlashCoroutine()
    {
        _spriteRenderer.sharedMaterial = _flashMaterial;

        yield return new WaitForSeconds(_flashDuration);

        _spriteRenderer.sharedMaterial = _originalMaterial;
        _flashRoutine = null;
    }

    private void StopFlash()
    {
        if (_flashRoutine != null)
        {
            StopCoroutine(_flashRoutine);
            _flashRoutine = null;
        }

        if (_spriteRenderer != null && _originalMaterial != null)
        {
            _spriteRenderer.sharedMaterial = _originalMaterial;
        }
    }
}
