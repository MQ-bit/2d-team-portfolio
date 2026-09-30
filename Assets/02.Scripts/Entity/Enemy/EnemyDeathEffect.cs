using UnityEngine;

// 적이 체력이 다해 죽었을 때(EnemyStatus.OnDeath) 그 자리에 사망 이펙트를 생성한다.
// 적 오브젝트는 죽는 즉시 파괴되므로 이펙트는 별도 오브젝트로 만들고 일정 시간 후 스스로 정리된다.
// 자폭처럼 OnDeath를 거치지 않는 소멸에서는 재생하지 않는다.
public class EnemyDeathEffect : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyStatus _status;

    [Header("Effect")]
    [SerializeField] private GameObject _effectPrefab;
    [SerializeField, Min(0.1f)] private float _effectLifetime = 2f;

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
            Debug.LogError($"{gameObject.name} : 사망 이펙트 프리팹이 할당 안됨");
        }
    }

    private void OnEnable()
    {
        if (_status != null)
        {
            _status.OnDeath += HandleDeath;
        }
    }

    private void OnDisable()
    {
        if (_status != null)
        {
            _status.OnDeath -= HandleDeath;
        }
    }

    private void HandleDeath()
    {
        if (_effectPrefab == null)
        {
            return;
        }

        GameObject effect = Instantiate(_effectPrefab, transform.position, Quaternion.identity);
        Destroy(effect, _effectLifetime); // TODO : 풀링 적용 시 반환으로 변경
    }
}
