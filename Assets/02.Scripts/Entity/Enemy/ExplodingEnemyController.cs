using System.Collections.Generic;
using UnityEngine;

public class ExplodingEnemyController : MonoBehaviour
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

    private bool CanAct => _potionEffectReceiver == null || _potionEffectReceiver.CanAct;

    private void Awake()
    {
        GetModules();

        _status.Initialize(_data != null ? _data.MaxHealth : 100f);

        if (_spriteRenderer != null)
        {
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
        Destroy(gameObject);
    }

    private void FixedUpdate()
    {
        if (_state == State.Exploded || _data == null)
        {
            return;
        }

        // 얼음/마비 중에는 이동과 예열 모두 멈춘다.
        if (!CanAct)
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
            _state = State.Idle;
            _movementModule.Stop();
            return;
        }

        _state = State.Chase;

        Vector2 targetPosition = _target.position;
        float distance = Vector2.Distance(transform.position, targetPosition);
        if (distance <= _data.FuseTriggerRange)
        {
            StartFuse();
            return;
        }

        _movementModule.MoveTowards(targetPosition, _data.MoveSpeed);
    }

    private void StartFuse()
    {
        _state = State.Fuse;
        _fuseElapsed = 0f;
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
        if (!CanAct)
        {
            return;
        }

        float t = Mathf.PingPong(Time.time * _fuseBlinkSpeed, 1f);
        _spriteRenderer.color = Color.Lerp(_originalColor, _fuseBlinkColor, t);
    }

    private void Explode()
    {
        _state = State.Exploded;
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

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            damageable?.DecreaseHealth(_data.ExplosionDamage);
        }

        Destroy(gameObject);
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
