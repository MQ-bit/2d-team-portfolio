using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 공통 수명/탐지 처리. 공격 선택과 실행은 파생 컨트롤러가 담당한다.
[RequireComponent(typeof(EnemyStatus), typeof(EnemyMovementModule), typeof(Rigidbody2D))]
public abstract class EnemyControllerBase : MonoBehaviour, IPoolMember
{
    [SerializeField] private EnemyData _data;
    protected EnemyData Data => _data;
    protected EnemyStatus Status { get; private set; }
    protected EnemyMovementModule Movement { get; private set; }
    protected Rigidbody2D Body { get; private set; }
    protected bool IsDead { get; private set; }
    protected bool CanAct => !IsDead && Status.CanMove;
    protected virtual float DeathDuration => 0f;
    private Collider2D _collider;
    private bool _colliderEnabled;
    private readonly List<Collider2D> _targets = new();

    protected virtual void Awake()
    {
        Status = GetComponent<EnemyStatus>();
        Movement = GetComponent<EnemyMovementModule>();
        Body = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();
        _colliderEnabled = _collider != null && _collider.enabled;
        if (_data != null)
        {
            Status.Initialize(_data);
        }
        else { Debug.LogError($"{name}: EnemyData가 필요합니다.", this); enabled = false; }
    }

    protected virtual void OnEnable() => Status.OnDeath += HandleDeath;
    protected virtual void OnDisable()
    {
        Status.OnDeath -= HandleDeath;
        StopAllCoroutines();
    }

    protected Transform FindPlayer()
    {
        Physics2D.OverlapCircle(Body.position, Data.DetectRange, ContactFilter2D.noFilter, _targets);
        foreach (Collider2D hit in _targets)
        {
            PotionEffectReceiver receiver = hit.GetComponentInParent<PotionEffectReceiver>();
            if (receiver != null && receiver.TargetKind == PotionTargetKind.Player
                && receiver.TryGetComponent(out PlayerStatus player) && !player.IsDead)
            {
                return receiver.transform;
            }
        }
        return null;
    }

    private void HandleDeath()
    {
        if (IsDead) return;
        IsDead = true;
        Movement.ResetState();
        if (_collider != null) _collider.enabled = false;
        OnDied();
        StartCoroutine(ReleaseAfterDeath());
    }

    protected virtual void OnDied() { }
    private IEnumerator ReleaseAfterDeath()
    {
        yield return new WaitForSeconds(DeathDuration);
        if (TryGetComponent(out PoolMember member) && member.IsRented) member.ReturnToPool();
        else Destroy(gameObject);
    }

    public void OnSpawned() => ResetForPool();
    public void OnDespawned() => ResetForPool();
    private void ResetForPool()
    {
        StopAllCoroutines();
        IsDead = false;
        if (_collider != null) _collider.enabled = _colliderEnabled;
        if (_data != null) Status.Initialize(_data);
        Movement.ResetState();
        ResetBehaviour();
    }
    protected abstract void ResetBehaviour();
}
