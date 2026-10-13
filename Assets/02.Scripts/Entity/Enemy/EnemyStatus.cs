using System;
using UnityEngine;

// PlayerStatus와 같은 역할: 체력(EntityHealth)을 소유하고 IDamageable을 구현하는 상태 허브.
// 최대 체력 수치 자체는 EnemyData가 갖고 있고, 컨트롤러가 Awake에서 Initialize()로 넘겨준다.
public class EnemyStatus : MonoBehaviour, IDamageable
{
    [SerializeField] private EntityHealth _health;
    [SerializeField] private EnemyKind _type;

    [Header("Component References")]
    [SerializeField] private PotionEffectReceiver _potionEffectReceiver;

    private float _moveSpeed;
    private float _contactDamage;
    public float ContactDamage => _health != null && !_health.IsDead ? _contactDamage : 0f;
    public float CurrentHealth => _health.Current;
    public float MaxHealth => _health.Max;
    public bool IsDead => _health.IsDead;

    public event Action<float, float> OnHealthChanged;
    public event Action OnDeath;
    // 피해를 입었지만 죽지는 않았을 때. 회복(RestoreHealth)이나 죽는 타격에서는 호출되지 않는다.
    public event Action OnDamaged;

    // Receiver는 상태값을 제공하고, 플레이어의 최종 이동 속도는 여기서 결정한다.
    private bool HasPotionMovementEffect => _potionEffectReceiver != null && _potionEffectReceiver.isActiveAndEnabled;
    public bool CanMove => !HasPotionMovementEffect || _potionEffectReceiver.CanMove;
    public float Speed => !CanMove ? 0f : _moveSpeed * (HasPotionMovementEffect ? _potionEffectReceiver.MoveSpeedMultiplier : 1f);
    public EnemyKind EnemyType => _type;

    private void Awake()
    {
        if (_potionEffectReceiver == null)
        {
            _potionEffectReceiver = GetComponent<PotionEffectReceiver>();
        }
    }

    public void Initialize(EnemyData data)
    {
        // 층이 올라갈수록 StageManager의 배율만큼 최대 체력이 늘어난다. (풀에서 꺼낼 때마다 다시 계산)
        float healthMultiplier = StageManager.Instance != null ? StageManager.Instance.EnemyHealthMultiplier : 1f;
        Initialize(data.MaxHealth * healthMultiplier, data.MoveSpeed);
        _contactDamage = data.ContactDamage;
    }
    public void Initialize(float maxHealth, float moveSpeed)
    {
        _health = new EntityHealth(maxHealth, maxHealth);
        _moveSpeed = moveSpeed;
        _contactDamage = 10f;
    }


    public void RestoreHealth(float amount)
    {
        float previousHealth = _health.Current;
        _health.RestoreHealth(amount);
        DamageFeedback.ReportHealing(transform.position, _health.Current - previousHealth);
        OnHealthChanged?.Invoke(_health.Current, _health.Max);
    }

    public void DecreaseHealth(float amount)
    {
        if (IsDead)
        {
            return;
        }

        float previousHealth = _health.Current;
        _health.DecreaseHealth(amount);
        DamageFeedback.Report(transform.position, previousHealth - _health.Current, false);
        OnHealthChanged?.Invoke(_health.Current, _health.Max);

        if (_health.IsDead)
        {
            // 사망 이벤트에서 풀로 반환되기 전에 재생한다. 소리는 SoundManager가 끝까지 관리한다.
            SoundManager.Play(GameSoundData.GetEnemyDeathSoundId(_type));
            OnDeath?.Invoke();
        }
        else if (amount > 0f)
        {
            OnDamaged?.Invoke();
        }
    }
}
