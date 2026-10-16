using System;
using System.Collections.Generic;
using UnityEngine;

// 조합 확산 중 원본 장판과 조합 장판의 대상 및 효과 전환을 관리한다.
internal sealed class PotionAreaSpreadEffect
{
    private PotionCombinationSpread _spread;
    private bool _firstLobe;
    private ComparisonAreaTimeline<IPotionEffectTarget> _originalTimeline;
    private readonly HashSet<IPotionEffectTarget> _reachedTargets = new();
    private readonly HashSet<IPotionEffectTarget> _originalTargets = new();
    private readonly HashSet<IPotionEffectTarget> _pendingImpact = new();

    internal bool IsActive => _spread != null;
    internal IReadOnlyList<Collider2D> Hits => _spread.Hits;
    internal bool HasReached(Collider2D collider) => _spread.HasReached(collider);
    internal bool HasReached(IPotionEffectTarget target) =>
        IsActive && _reachedTargets.Contains(target);

    internal void Prepare(PotionCombinationSpread spread, bool firstLobe)
    {
        _spread = spread;
        _firstLobe = firstLobe;
    }

    internal void KeepOriginalTimeline(ComparisonAreaTimeline<IPotionEffectTarget> timeline) =>
        _originalTimeline = timeline;

    internal void QueueCombinationImpact(Func<IPotionEffectTarget, bool> isValid)
    {
        foreach (Collider2D hit in _spread.Hits)
        {
            IPotionEffectTarget target = PotionEffectApplier.FindTarget(hit);
            if (isValid(target)) _pendingImpact.Add(target);
        }
    }

    // true이면 확산이 끝났으며 호출자가 일반 장판 추적으로 전환할 수 있다.
    internal bool Synchronize(ComparisonAreaTimeline<IPotionEffectTarget> timeline,
        bool hasCombination, Action<IPotionEffectTarget> applyImpact)
    {
        PotionCombinationSpread spread = _spread;
        if (spread == null) return false;
        _reachedTargets.Clear();
        _originalTargets.Clear();
        foreach (Collider2D hit in _spread.Hits)
        {
            if (hit == null) continue;
            IPotionEffectTarget target = PotionEffectApplier.FindTarget(hit);
            if (target == null) continue;
            if (_spread.HasReached(hit)) _reachedTargets.Add(target);
            Vector2 center = _firstLobe ? _spread.Shape.FirstCenter : _spread.Shape.SecondCenter;
            if (_spread.Shape.ContainsLobe(hit.ClosestPoint(center), _firstLobe))
                _originalTargets.Add(target);
        }
        // 콜라이더가 여러 개인 대상도 확산이 닿는 순간 한 번만 전환한다.
        _originalTargets.ExceptWith(_reachedTargets);
        // Effect callbacks can disable the area and clear its live target sets.
        var reachedTargets = new List<IPotionEffectTarget>(_reachedTargets);
        var originalTargets = new List<IPotionEffectTarget>(_originalTargets);
        double now = Time.timeAsDouble;
        if (hasCombination)
        {
            _originalTimeline?.Synchronize(originalTargets, now);
            if (_spread != spread) return false;
            foreach (IPotionEffectTarget target in reachedTargets)
            {
                if (_pendingImpact.Remove(target)) applyImpact(target);
                if (_spread != spread) return false;
            }
            timeline?.Synchronize(reachedTargets, now);
        }
        else
        {
            timeline?.Synchronize(originalTargets, now);
        }

        if (_spread != spread || !spread.IsComplete) return false;
        if (hasCombination)
        {
            var originalTimeline = _originalTimeline;
            _originalTimeline = null;
            originalTimeline?.End(false);
            if (_spread != spread) return false;
            _pendingImpact.Clear();
        }
        return true;
    }

    internal void Clear()
    {
        var originalTimeline = _originalTimeline;
        _originalTimeline = null;
        _spread = null;
        _pendingImpact.Clear();
        _reachedTargets.Clear();
        _originalTargets.Clear();
        originalTimeline?.End(false);
    }
}
