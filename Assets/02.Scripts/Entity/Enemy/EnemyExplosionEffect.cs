using UnityEngine;

// 폭발형 적이 자폭할 때(ExplodingEnemyController.OnExploded) 그 자리에 폭발 이펙트를 생성한다.
// 적 오브젝트는 폭발 즉시 파괴되므로 이펙트는 별도 오브젝트로 만들고 일정 시간 후 스스로 정리된다.
public class EnemyExplosionEffect : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ExplodingEnemyController _controller;

    [Header("Effect")]
    [SerializeField] private GameObject _effectPrefab;
    [SerializeField, Min(0.1f)] private float _effectLifetime = 2f;

    private void Awake()
    {
        if (_controller == null)
        {
            _controller = GetComponent<ExplodingEnemyController>();
            if (_controller == null)
            {
                Debug.LogError($"{gameObject.name} : ExplodingEnemyController 가 할당 안됨");
            }
        }
        if (_effectPrefab == null)
        {
            Debug.LogError($"{gameObject.name} : 폭발 이펙트 프리팹이 할당 안됨");
        }
    }

    private void OnEnable()
    {
        if (_controller != null)
        {
            _controller.OnExploded += HandleExploded;
        }
    }

    private void OnDisable()
    {
        if (_controller != null)
        {
            _controller.OnExploded -= HandleExploded;
        }
    }

    private void HandleExploded()
    {
        SoundManager.Play(GameSoundId.EnemyExplosion);

        if (_effectPrefab == null)
        {
            return;
        }

        GameObject effect = Instantiate(_effectPrefab, transform.position, Quaternion.identity);
        Destroy(effect, _effectLifetime); // TODO : 풀링 적용 시 반환으로 변경
    }
}
