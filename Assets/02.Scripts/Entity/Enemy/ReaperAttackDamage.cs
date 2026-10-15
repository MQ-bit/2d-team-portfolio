using System.Collections.Generic;
using UnityEngine;

// 여러 플레이어 콜라이더를 한 번의 타격으로 묶는다. 획득 범위는 피해 판정에 쓰지 않는다.
internal static class ReaperAttackDamage
{
    public static void ApplyMelee(Vector2 origin, Vector2 center, Vector2 size, float damage,
        Vector2 direction, List<Collider2D> hits, HashSet<PlayerStatus> damaged)
    {
        if (damage <= 0f || size.x <= 0f || size.y <= 0f) return;
        Physics2D.OverlapBox(center, size, 0f, ContactFilter2D.noFilter, hits);
        foreach (Collider2D hit in hits)
        {
            PotionEffectReceiver receiver = hit.GetComponentInParent<PotionEffectReceiver>();
            if (receiver == null || receiver.TargetKind != PotionTargetKind.Player
                || !receiver.TryGetComponent(out PlayerStatus player) || player.IsDead || damaged.Contains(player))
            {
                continue;
            }

            CircleCollider2D body = player.GetComponent<CircleCollider2D>();
            // OverlapBox가 실제 몸 콜라이더와 겹칠 때만 유효. 획득/감지 트리거는 제외한다.
            if (body == null || hit != body || !body.isActiveAndEnabled || body.isTrigger) continue;
            Vector2 closest = body.ClosestPoint(origin);
            if (!EnemyPathfinder.Instance.HasLineOfSight(origin, closest)) continue;
            damaged.Add(player);
            Vector2 away = (Vector2)player.transform.position - origin;
            player.DecreaseHealth(damage, away.sqrMagnitude > 0.0001f ? away.normalized : direction);
        }
    }

    public static void Apply(Vector2 origin, float radius, float damage, Vector2 direction,
        float angle, List<Collider2D> hits, HashSet<PlayerStatus> damaged, bool checkWalls)
    {
        if (damage <= 0f || radius <= 0f) return;
        Physics2D.OverlapCircle(origin, radius, ContactFilter2D.noFilter, hits);
        foreach (Collider2D hit in hits)
        {
            PotionEffectReceiver receiver = hit.GetComponentInParent<PotionEffectReceiver>();
            if (receiver == null || receiver.TargetKind != PotionTargetKind.Player
                || !receiver.TryGetComponent(out PlayerStatus player) || player.IsDead || damaged.Contains(player))
            {
                continue;
            }

            CircleCollider2D body = player.GetComponent<CircleCollider2D>();
            if (body == null || !body.isActiveAndEnabled || body.isTrigger) continue;
            Vector2 closest = body.ClosestPoint(origin);
            Vector2 offset = closest - origin;
            if (offset.sqrMagnitude > radius * radius) continue;
            if (angle < 360f && offset.sqrMagnitude > 0.0001f
                && Vector2.Angle(direction, offset) > angle * 0.5f)
            {
                continue;
            }

            if (checkWalls && !EnemyPathfinder.Instance.HasLineOfSight(origin, closest)) continue;
            damaged.Add(player);
            Vector2 away = (Vector2)player.transform.position - origin;
            player.DecreaseHealth(damage, away.sqrMagnitude > 0.0001f ? away.normalized : direction);
        }
    }
}
