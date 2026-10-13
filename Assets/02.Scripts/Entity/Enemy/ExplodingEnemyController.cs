using System;
using System.Collections.Generic;
using UnityEngine;

public class ExplodingEnemyController : MonoBehaviour, IPoolMember
{
    private enum State
    {
        Idle,
        Chase,
        Fuse,
        Exploded
    }

    [Header("Data")]
    [SerializeField] private ExplodingEnemyData _data;

    [Header("Module References")]
    [SerializeField] private EnemyMovementModule _movementModule;
    [SerializeField] private PotionEffectReceiver _potionEffectReceiver;
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private EnemyStatus _status;

    [Header("Fuse Visual")]
    [SerializeField] private Color _fuseBlinkColor = Color.white;
    [SerializeField, Min(0.1f)] private float _fuseBlinkSpeed = 12f;

    private readonly List<Collider2D> _overlapResults = new List<Collider2D>();
    private readonly List<PotionEffectReceiver> _damagedTargets = new List<PotionEffectReceiver>();

    private State _state = State.Idle;
    private Transform _target;
    private float _fuseElapsed;
    private Color _originalColor;
    private bool _isWindingUp; // 돌진 전에 멈춰 있는 중
    private float _windupElapsed;
    private bool _isRushing;
    private float _damageMultiplier = 1f; // 분열체는 엘리트 데이터의 SplitDamageMultiplier로 낮아진다

    private bool CanMove => _potionEffectReceiver == null || _potionEffectReceiver.CanMove;

    // 실제 피해 판정 반경. 폭발 이펙트 크기를 여기에 맞춘다.
    public float ExplosionRadius => _data != null ? _data.ExplosionRadius : 0f;

    // 예열이 끝나 자폭하는 순간. 체력으로 죽는 EnemyStatus.OnDeath와는 별개다.
    public event Action OnExploded;

    private void Awake()
    {
        GetModules();

        if (_data != null) _status.Initialize(_data);
        else _status.Initialize(100f, 2.5f);

        if (_spriteRenderer != null)
        {
            if (_data != null)
            {
                _data.ApplyVisual(_spriteRenderer);
            }

            _originalColor = _spriteRenderer.color;
        }
    }

    private void GetModules()
    {
        if (_data == null)
        {
            Debug.LogError($"{gameObject.name} : ExplodingEnemyData가 할당 안됨");
        }
        if (_movementModule == null)
        {
            _movementModule = GetComponent<EnemyMovementModule>();
            if (_movementModule == null)
            {
                Debug.LogError($"{gameObject.name} : EnemyMovementModule 이 할당 안됨");
            }
        }
        if (_potionEffectReceiver == null)
        {
            _potionEffectReceiver = GetComponent<PotionEffectReceiver>();
        }
        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        if (_status == null)
        {
            _status = GetComponent<EnemyStatus>();
            if (_status == null)
            {
                Debug.LogError($"{gameObject.name} : EnemyStatus 가 할당 안됨");
            }
        }
    }

    private void ResetState()
    {
        _state = State.Idle;
        _target = null;
        _fuseElapsed = 0f;
        _isWindingUp = false;
        _windupElapsed = 0f;
        _isRushing = false;
        _damageMultiplier = 1f;

        _overlapResults.Clear();
        _damagedTargets.Clear();

        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = _originalColor;
        }

        if (_status != null && _data != null)
        {
            _status.Initialize(_data);
        }

        if (_movementModule != null)
        {
            _movementModule.ResetState();
        }
    }

    private void OnEnable()
    {
        _status.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        _status.OnDeath -= HandleDeath;
    }

    // 포션 등으로 죽으면 폭발하지 않고 그냥 사라진다.
    private void HandleDeath()
    {
        if (_state == State.Exploded)
        {
            return;
        }

        _state = State.Exploded;
        Release();
    }

    private void FixedUpdate()
    {
        if (_state == State.Exploded || _data == null)
        {
            return;
        }

        // 얼음/마비 중에는 이동과 예열 모두 멈춘다.
        if (!CanMove)
        {
            _movementModule.Stop();
            return;
        }

        switch (_state)
        {
            case State.Idle:
            case State.Chase:
                UpdateChase();
                break;
            case State.Fuse:
                UpdateFuse();
                break;
        }
    }

    private void Update()
    {
        UpdateFuseBlink();
    }

    private void UpdateChase()
    {
        _target = FindPlayerInRange(_data.DetectRange);
        if (_target == null)
        {
            // 멈춰서 붉어지던 중에 대상을 놓치면 취소하고 색을 되돌린다.
            if (_isWindingUp)
            {
                _isWindingUp = false;
                SetBodyColor(_originalColor);
            }

            _state = State.Idle;
            _movementModule.Stop();
            return;
        }

        _state = State.Chase;

        Vector2 targetPosition = _target.position;
        float distance = Vector2.Distance(transform.position, targetPosition);
        // 벽 너머에서 예열하지 않도록 직접 보일 때만 예열을 시작한다.
        if (distance <= _data.FuseTriggerRange && EnemyPathfinder.Instance.HasLineOfSight(transform.position, targetPosition))
        {
            StartFuse();
            return;
        }

        // 처음 범위 안에 들어오면 RushWindupDuration 동안 멈춰서 붉어진 뒤, 평소 속도의 RushSpeedMultiplier배로 돌진한다.
        // 한 번 멈추기 시작하면 플레이어가 범위를 벗어나도 끝까지 멈췄다가 돌진하고,
        // 돌진을 시작하면 계속 돌진한다(멈칫하는 연출이 반복되지 않게).
        bool canStartWindup = _data.RushSpeedMultiplier > 1f && distance <= _data.RushTriggerRange;
        if (!_isRushing && (_isWindingUp || canStartWindup))
        {
            if (!_isWindingUp)
            {
                _isWindingUp = true;
                _windupElapsed = 0f;
                SetBodyColor(_data.RushWindupColor);
            }

            _windupElapsed += Time.fixedDeltaTime;
            if (_windupElapsed >= _data.RushWindupDuration)
            {
                _isWindingUp = false;
                _isRushing = true;
                SetBodyColor(_originalColor);
            }
            else
            {
                _movementModule.Stop();
                return;
            }
        }

        float speed = _isRushing ? _status.Speed * _data.RushSpeedMultiplier : _status.Speed;
        _movementModule.ChaseTarget(targetPosition, speed);
    }

    private void SetBodyColor(Color color)
    {
        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = color;
        }
    }

    // 분열해서 생긴 적이 쫓아오지 않고 바로 예열부터 시작하게 한다.
    public void BeginFuse(float damageMultiplier = 1f)
    {
        _damageMultiplier = damageMultiplier;
        StartFuse();
    }

    private void StartFuse()
    {
        _state = State.Fuse;
        _fuseElapsed = 0f;
        _isWindingUp = false;
        _movementModule.Stop();
    }

    private void UpdateFuse()
    {
        _movementModule.Stop();

        _fuseElapsed += Time.fixedDeltaTime;
        if (_fuseElapsed >= _data.FuseDuration)
        {
            Explode();
        }
    }

    private void UpdateFuseBlink()
    {
        if (_spriteRenderer == null || _state != State.Fuse)
        {
            return;
        }

        // 예열이 멈춘 동안에는 깜빡임도 멈춘다.
        if (!CanMove)
        {
            return;
        }

        float t = Mathf.PingPong(Time.time * _fuseBlinkSpeed, 1f);
        _spriteRenderer.color = Color.Lerp(_originalColor, _fuseBlinkColor, t);
    }

    private void Explode()
    {
        if (_data.SplitPrefab != null)
        {
            Split();
            return;
        }

        _state = State.Exploded;
        DealExplosion();
        Release();
    }

    // 폭발 이펙트/사운드를 내고 반경 안의 플레이어에게 피해를 준다.
    private void DealExplosion()
    {
        OnExploded?.Invoke();
        _damagedTargets.Clear();

        Physics2D.OverlapCircle(transform.position, _data.ExplosionRadius, ContactFilter2D.noFilter, _overlapResults);
        foreach (Collider2D hit in _overlapResults)
        {
            PotionEffectReceiver receiver = hit.GetComponentInParent<PotionEffectReceiver>();
            if (receiver == null || receiver.TargetKind != PotionTargetKind.Player || _damagedTargets.Contains(receiver))
            {
                continue;
            }

            _damagedTargets.Add(receiver);

            // 폭발 중심에서 바깥으로 맞은 방향을 넘겨 피격 연출이 폭발 반대쪽으로 튀게 한다.
            if (hit.GetComponentInParent<IDirectionalDamageable>() is IDirectionalDamageable directional)
            {
                directional.DecreaseHealth(_data.ExplosionDamage * _damageMultiplier, (Vector2)(hit.bounds.center - transform.position));
            }
            else
            {
                hit.GetComponentInParent<IDamageable>()?.DecreaseHealth(_data.ExplosionDamage * _damageMultiplier);
            }
        }
    }

    // 엘리트 폭발형: 예열이 끝나면 먼저 한 번 터진 뒤(이펙트/피해), SplitPrefab 적 여러 마리로 갈라져 각자 다시 터진다.
    private void Split()
    {
        _state = State.Exploded;
        DealExplosion();

        // 분열체를 좌우로 균등하게 벌려 배치한다. (2마리면 정확히 왼쪽/오른쪽)
        int count = _data.SplitCount;
        for (int i = 0; i < count; i++)
        {
            float t = count > 1 ? (float)i / (count - 1) * 2f - 1f : 1f; // -1 ~ 1
            Vector2 offset = Vector2.right * (t * _data.SplitSpawnDistance);
            // 몬스터는 SpawnManager의 풀에서 빌려 쓴다.
            Vector3 position = (Vector2)transform.position + offset;
            GameObject split = SpawnManager.Instance.Spawn(_data.SplitPrefab, position, transform.rotation);
            if (split == null)
            {
                continue;
            }

            split.transform.SetParent(transform.parent, true);
            if (split.TryGetComponent(out ExplodingEnemyController controller))
            {
                controller.BeginFuse(_data.SplitDamageMultiplier);
            }
        }

        Release();
    }

    // 플레이어는 태그/레이어가 없어서 TargetKind가 Player인 PotionEffectReceiver로 찾는다.
    private Transform FindPlayerInRange(float range)
    {
        Physics2D.OverlapCircle(transform.position, range, ContactFilter2D.noFilter, _overlapResults);
        foreach (Collider2D hit in _overlapResults)
        {
            PotionEffectReceiver receiver = hit.GetComponentInParent<PotionEffectReceiver>();
            if (receiver != null && receiver.TargetKind == PotionTargetKind.Player)
            {
                return receiver.transform;
            }
        }

        return null;
    }

    public void OnSpawned()
    {
        ResetState();
    }

    public void OnDespawned()
    {
        ResetState();
    }

    /// <summary>
    /// 풀에서 반환되거나, 풀에 속하지 않은 경우에는 그냥 파괴한다.
    /// </summary>
    private void Release()
    {
        if (TryGetComponent(out PoolMember member) && member.IsRented)
        {
            member.ReturnToPool();
            return;
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        if (_data == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _data.DetectRange);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, _data.FuseTriggerRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _data.ExplosionRadius);
    }
}
