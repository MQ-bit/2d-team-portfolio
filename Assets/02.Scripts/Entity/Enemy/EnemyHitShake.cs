using System.Collections;
using UnityEngine;

// 적이 피해를 입으면(EnemyStatus.OnDamaged) 외형을 잠깐 흔든다.
// 루트에는 Rigidbody2D가 있어 직접 흔들면 물리 이동과 충돌하므로, 스프라이트가 붙은 자식(Visual)만 흔든다.
public class EnemyHitShake : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyStatus _status;
    [SerializeField] private Transform _visual;

    [Header("Shake")]
    [SerializeField, Min(0.01f)] private float _shakeDuration = 0.15f;
    [SerializeField, Min(0f)] private float _shakeStrength = 0.08f;

    private Vector3 _originalLocalPosition;
    private Coroutine _shakeRoutine;

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
        if (_visual == null || _visual == transform)
        {
            Debug.LogError($"{gameObject.name} : 흔들 Visual(자식 Transform)이 할당 안됨");
            _visual = null;
        }

        if (_visual != null)
        {
            _originalLocalPosition = _visual.localPosition;
        }
    }

    private void OnEnable()
    {
        if (_status != null)
        {
            _status.OnDamaged += Shake;
        }
    }

    private void OnDisable()
    {
        if (_status != null)
        {
            _status.OnDamaged -= Shake;
        }

        StopShake();
    }

    private void Shake()
    {
        if (_visual == null)
        {
            return;
        }

        StopShake();
        _shakeRoutine = StartCoroutine(ShakeCoroutine());
    }

    private IEnumerator ShakeCoroutine()
    {
        float elapsedTime = 0f;

        while (elapsedTime < _shakeDuration)
        {
            elapsedTime += Time.deltaTime;

            // 시간이 지날수록 흔들림 폭이 줄어든다.
            float strength = _shakeStrength * (1f - Mathf.Clamp01(elapsedTime / _shakeDuration));
            _visual.localPosition = _originalLocalPosition + (Vector3)(Random.insideUnitCircle * strength);

            yield return null;
        }

        _visual.localPosition = _originalLocalPosition;
        _shakeRoutine = null;
    }

    private void StopShake()
    {
        if (_shakeRoutine != null)
        {
            StopCoroutine(_shakeRoutine);
            _shakeRoutine = null;
        }

        if (_visual != null)
        {
            _visual.localPosition = _originalLocalPosition;
        }
    }
}
