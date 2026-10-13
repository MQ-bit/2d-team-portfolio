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
    [Tooltip("이펙트 프리팹을 스케일 1로 생성했을 때 눈에 보이는 폭발 반경(월드 단위). 이펙트는 실제 피해 반경(ExplosionRadius) / 이 값 만큼 확대된다. 이펙트가 판정보다 작아 보이면 이 값을 줄이고, 너무 크면 늘린다.")]
    [SerializeField, Min(0.1f)] private float _effectBaseRadius = 2f;

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
        FitEffectToRadius(effect);
        Destroy(effect, _effectLifetime); // TODO : 풀링 적용 시 반환으로 변경
    }

    // 눈에 보이는 폭발 범위가 실제 피해 반경보다 작지 않도록 이펙트를 확대한다.
    private void FitEffectToRadius(GameObject effect)
    {
        if (_controller == null || _controller.ExplosionRadius <= 0f)
        {
            return;
        }

        float scale = _controller.ExplosionRadius / _effectBaseRadius;
        effect.transform.localScale = Vector3.one * scale;

        // 파티클이 Local 스케일 모드면 부모 스케일이 무시되므로 계층 스케일을 따르게 한다.
        foreach (ParticleSystem particles in effect.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.MainModule main = particles.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        }
    }
}
