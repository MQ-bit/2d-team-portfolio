using System.Collections.Generic;
using UnityEngine;

// 적이 체력이 다해 죽었을 때(EnemyStatus.OnDeath) 일정 확률로 포션을 필드에 떨어뜨린다.
// 자폭처럼 OnDeath를 거치지 않는 소멸에서는 드랍하지 않는다.
// 포션 종류는 PotionData가 미리 지정된 필드 아이템 프리팹 목록 중에서 무작위로 고른다.
public class EnemyPotionDropper : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyStatus _status;

    [Header("Drop Table")]
    [SerializeField] private List<PotionFieldItem> _dropPrefabs = new List<PotionFieldItem>();
    [SerializeField, Range(0f, 1f)] private float _dropChance = 0.3f;

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
        if (_dropPrefabs.Count == 0)
        {
            Debug.LogError($"{gameObject.name} : 드랍할 PotionFieldItem 프리팹이 할당 안됨");
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
        // Random.value는 1을 포함하므로 >로 비교해야 확률이 1일 때 항상 드랍된다.
        if (_dropPrefabs.Count == 0 || _dropChance <= 0f || Random.value > _dropChance)
        {
            return;
        }

        PotionFieldItem prefab = _dropPrefabs[Random.Range(0, _dropPrefabs.Count)];
        if (prefab == null)
        {
            return;
        }

        Instantiate(prefab, transform.position, Quaternion.identity);
    }
}
