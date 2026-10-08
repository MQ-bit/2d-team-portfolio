using System.Collections.Generic;
using UnityEngine;

// 여러 몸체 콜라이더를 가진 대상도 장판 진입·이탈은 한 번씩만 처리한다.
internal sealed class PotionAreaContacts
{
    private readonly Dictionary<IPotionEffectTarget, int> _overlapCounts = new();
    private readonly Dictionary<Collider2D, IPotionEffectTarget> _targetsByCollider = new();

    internal IEnumerable<IPotionEffectTarget> Targets => _overlapCounts.Keys;

    internal bool Enter(Collider2D collider)
    {
        if (_targetsByCollider.ContainsKey(collider)) return false;
        IPotionEffectTarget target = PotionEffectApplier.FindTarget(collider);
        if (target == null) return false;
        _targetsByCollider.Add(collider, target);
        _overlapCounts.TryGetValue(target, out int count);
        _overlapCounts[target] = count + 1;
        return count == 0;
    }

    internal bool Exit(Collider2D collider)
    {
        if (!_targetsByCollider.Remove(collider, out IPotionEffectTarget target) ||
            !_overlapCounts.TryGetValue(target, out int count))
        {
            return false;
        }

        if (count > 1)
        {
            _overlapCounts[target] = count - 1;
            return false;
        }
        _overlapCounts.Remove(target);
        return true;
    }

    internal void Rebuild(IReadOnlyList<Collider2D> hits)
    {
        Clear();
        foreach (Collider2D hit in hits)
            if (hit != null) Enter(hit);
    }

    internal void Clear()
    {
        _overlapCounts.Clear();
        _targetsByCollider.Clear();
    }
}