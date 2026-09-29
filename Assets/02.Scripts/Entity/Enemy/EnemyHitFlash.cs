using System.Collections;
using UnityEngine;

// 적이 피해를 입으면(EnemyStatus.OnDamaged) 스프라이트를 잠깐 번쩍이게 한다.
// SpriteRenderer.color는 곱연산이라 흰색으로 만들 수 없어서, 번쩍이는 동안만 단색 머티리얼로 교체한다.
public class EnemyHitFlash : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyStatus _status;
    [SerializeField] private SpriteRenderer _spriteRenderer;

    [Header("Flash")]
    [SerializeField] private Material _flashMaterial;
    [SerializeField, Min(0.01f)] private float _flashDuration = 0.1f;

    private Material _originalMaterial;
    private WaitForSeconds _waitForFlash;
    private Coroutine _flashRoutine;

    private void Awake()
    {
        if (_status == null)
        {
            _status = GetComponent<EnemyStatus>();
            if (_status == null)
            {
                Debug.LogError($"{gameObject.name} : EnemyStatus 가 할당 안됨");
            }
        }
        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (_spriteRenderer == null)
            {
                Debug.LogError($"{gameObject.name} : SpriteRenderer 가 할당 안됨");
            }
        }
        if (_flashMaterial == null)
        {
            Debug.LogError($"{gameObject.name} : Flash Material 이 할당 안됨");
        }

        if (_spriteRenderer != null)
        {
            _originalMaterial = _spriteRenderer.sharedMaterial;
        }
        _waitForFlash = new WaitForSeconds(_flashDuration);
    }

    private void OnEnable()
    {
        if (_status != null)
        {
            _status.OnDamaged += Flash;
        }
    }

    private void OnDisable()
    {
        if (_status != null)
        {
            _status.OnDamaged -= Flash;
        }

        StopFlash();
    }

    private void Flash()
    {
        if (_spriteRenderer == null || _flashMaterial == null)
        {
            return;
        }

        if (_flashRoutine != null)
        {
            StopCoroutine(_flashRoutine);
        }

        _flashRoutine = StartCoroutine(FlashCoroutine());
    }

    private IEnumerator FlashCoroutine()
    {
        _spriteRenderer.sharedMaterial = _flashMaterial;

        yield return _waitForFlash;

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
