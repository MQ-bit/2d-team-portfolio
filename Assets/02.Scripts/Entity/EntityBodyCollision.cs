using System.Collections.Generic;
using UnityEngine;

// 플레이어/적 몸통은 서로 통과하며, 실제 몸통이 겹칠 때만 접촉 피해를 검사한다.
// 수집 범위, 투사체, 벽, 적끼리의 물리 충돌에는 영향을 주지 않는다.
internal sealed class EntityBodyCollision
{
    private static readonly List<EntityBodyCollision> Bodies = new();
    private readonly Rigidbody2D _rigid;
    private readonly CircleCollider2D _body;
    private readonly bool _isPlayer;
    private readonly EnemyStatus _enemyStatus;
    private readonly PlayerStatus _playerStatus;

    public EntityBodyCollision(Rigidbody2D rigid, bool isPlayer)
    {
        _rigid = rigid;
        _body = rigid.GetComponent<CircleCollider2D>();
        _isPlayer = isPlayer;
        _enemyStatus = rigid.GetComponent<EnemyStatus>();
        _playerStatus = rigid.GetComponent<PlayerStatus>();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry() => Bodies.Clear();

    private bool IsActive => _rigid != null && _rigid.simulated && _body != null
        && _body.isActiveAndEnabled;

    public void Register()
    {
        if (_body == null || Bodies.Contains(this)) return;
        Bodies.RemoveAll(body => body._rigid == null);
        foreach (EntityBodyCollision other in Bodies)
            SetIgnored(other, true);
        Bodies.Add(this);
    }

    public void Unregister()
    {
        foreach (EntityBodyCollision other in Bodies)
            SetIgnored(other, false);
        Bodies.Remove(this);
    }

    private void SetIgnored(EntityBodyCollision other, bool ignored)
    {
        if (_isPlayer != other._isPlayer && _body != null && other._body != null
            && _body.isActiveAndEnabled && other._body.isActiveAndEnabled)
        {
            Physics2D.IgnoreCollision(_body, other._body, ignored);
        }
    }

    private Vector2 Center => _rigid.position
        + (Vector2)_body.transform.TransformVector(_body.offset);
    private float Radius => _body.radius * Mathf.Max(Mathf.Abs(_body.transform.lossyScale.x),
        Mathf.Abs(_body.transform.lossyScale.y));

    public void MoveTo(Vector2 destination) => _rigid.MovePosition(destination);

    // 입력이 없거나 공격/대시 중이어도 매 물리 프레임 검사한다.
    public void UpdateContactDamage()
    {
        if (!_isPlayer || !IsActive || _playerStatus == null) return;
        Vector2 center = Center;
        float radius = Radius;
        float damage = 0f;
        Vector2 direction = Vector2.zero;

        foreach (EntityBodyCollision other in Bodies)
        {
            if (other._isPlayer || !other.IsActive) continue;
            // 풀링/콜라이더 재활성화로 풀린 충돌 제외도 복원한다.
            SetIgnored(other, true);
            if (other._enemyStatus == null) continue;
            float contactDamage = other._enemyStatus.ContactDamage;
            if (contactDamage <= damage) continue;
            Vector2 away = center - other.Center;
            float contactRadius = radius + other.Radius;
            if (away.sqrMagnitude > contactRadius * contactRadius) continue;
            damage = contactDamage;
            direction = away.sqrMagnitude > 0.000001f ? away.normalized : Vector2.up;
        }

        // 여러 적이 겹쳤다면 가장 큰 접촉 피해 한 번만 적용한다.
        if (damage > 0f) _playerStatus.TryTakeContactDamage(damage, direction);
    }
}