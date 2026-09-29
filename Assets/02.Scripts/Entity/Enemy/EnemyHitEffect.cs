using UnityEngine;

// 적이 피해를 입으면(EnemyStatus.OnDamaged) 그 자리에 피격 이펙트를 생성한다.
// 이펙트는 적과 별개 오브젝트로 만들어 적이 움직이거나 파괴돼도 끝까지 재생되고, 일정 시간 후 스스로 정리된다.
public class EnemyHitEffect : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyStatus _status;

    [Header("Effect")]
    [SerializeField] private GameObject _effectPrefab;
    [SerializeField, Min(0.1f)] private float _effectLifetime = 1f;

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
        if (_effectPrefab == null)
        {
            Debug.LogError($"{gameObject.name} : 피격 이펙트 프리팹이 할당 안됨");
        }
    }

    private void OnEnable()
    {
        if (_status != null)
        {
            _status.OnDamaged += HandleDamaged;
        }
    }

    private void OnDisable()
    {
        if (_status != null)
        {
            _status.OnDamaged -= HandleDamaged;
        }
    }

    // 죽는 타격에서는 OnDamaged 대신 OnDeath가 불리므로 사망 이펙트(EnemyDeathEffect)만 재생된다.
    private void HandleDamaged()
    {
        if (_effectPrefab == null)
        {
            return;
        }

        GameObject effect = Instantiate(_effectPrefab, transform.position, Quaternion.identity);
        Destroy(effect, _effectLifetime); // TODO : 풀링 적용 시 반환으로 변경
    }
}
