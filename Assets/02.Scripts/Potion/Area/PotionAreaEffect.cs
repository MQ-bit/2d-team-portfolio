using System.Collections.Generic;
using UnityEngine;

public class PotionAreaEffect : MonoBehaviour  // 계산된 결과로 피해·상태이상 적용
{
    private readonly Dictionary<IPotionEffectTarget, int> _overlapCounts = new();
    private readonly Dictionary<Collider2D, IPotionEffectTarget> _targetsByCollider = new();
    private PotionData _data;
    private int _sourceId;
    private bool _hasCombination;
    private IPotionEffectData _singleProfile;
    private PotionEffectSource _singleSource;
    private PotionAreaTimeline<IPotionEffectTarget> _singleTimeline;
    private PotionAreaTimeline<IPotionEffectTarget> _originalTimeline;
    private PotionCombinationSpread _spread;
    private bool _originalFirstLobe;
    private readonly HashSet<IPotionEffectTarget> _reachedTargets = new();
    private readonly HashSet<IPotionEffectTarget> _originalTargets = new();
    private readonly HashSet<IPotionEffectTarget> _pendingCombinationImpact = new();

    public bool HasCombination => _hasCombination;

    private void Awake()
    {
        _sourceId = GetInstanceID();
    }

    public void Initialize(PotionData data)
    {
        _data = data;
    }

    public void ActivateSingleProfile(IDamageable owner, float expiresAt)
    {
        _singleProfile = _data;
        if (_singleProfile == null) return;

        _singleSource = new PotionEffectSource(_sourceId, _data, owner: owner,
            lifeStealRatio: _singleProfile.LifeStealRatio);
        if (_singleProfile.CreatesFloor)
        {
            // Capture the original profile/source: this timeline survives while the
            // reaction front replaces only the reached part with a new profile.
            IPotionEffectData profile = _singleProfile;
            PotionEffectSource source = _singleSource;
            _singleTimeline = new PotionAreaTimeline<IPotionEffectTarget>(expiresAt,
                profile.TickInterval, profile.ApplyOnEnter,
                target => PotionEffectApplier.CanAffect(profile.TargetType, target),
                target => target.EnterFloor(profile, source),
                target => target.PulseFloor(source.SourceId),
                (target, residual) => target.ExitFloor(source.SourceId,
                    residual && !(_spread != null && _reachedTargets.Contains(target))));
            SynchronizeSingleTimeline();
        }
    }

    public void ApplySingleImpact(Collider2D effectCollider = null)
    {
        if (_singleProfile == null) return;

        Physics2D.SyncTransforms();
        Collider2D areaCollider = effectCollider != null ? effectCollider : GetComponent<PotionAreaController>()?.AreaCollider;
        if (areaCollider == null) return;

        var hits = new List<Collider2D>();
        var targets = new HashSet<IPotionEffectTarget>();
        ContactFilter2D filter = new() { useTriggers = true };
        areaCollider.Overlap(filter, hits);
        foreach (Collider2D hit in hits)
        {
            if (_hasCombination && _spread != null && !_spread.HasReached(hit)) continue;
            IPotionEffectTarget target = PotionEffectApplier.FindTarget(hit);
            if (!IsValidSingleTarget(target) || !targets.Add(target)) continue;
            PotionEffectApplier.ApplyProfilePhase(_singleProfile, PotionEffectPhase.Impact,
                target, _singleSource);
        }
    }

    public void ReplaceWithCombinationProfile(PotionCombinationData combination,
        IPotionEffectData profile, IDamageable owner, float lifeStealRatio,
        float expiresAt, Collider2D effectCollider)
    {
        if (_spread != null)
        {
            _originalTimeline = _singleTimeline;
            _singleTimeline = null;
            _singleProfile = null;
        }
        else
        {
            EndSingleTimeline(false);
        }

        _hasCombination = true;
        _singleProfile = profile;
        if (_singleProfile == null) return;
        // Keep the old source independent until its part of the floor is converted.
        if (effectCollider != null) _sourceId = effectCollider.GetInstanceID();
        _singleSource = new PotionEffectSource(_sourceId, combination: combination,
            owner: owner, lifeStealRatio: lifeStealRatio);
        _singleTimeline = new PotionAreaTimeline<IPotionEffectTarget>(expiresAt,
            _singleProfile.TickInterval, _singleProfile.ApplyOnEnter,
            IsValidSingleTarget, EnterSingleFloor, PulseSingleFloor, ExitSingleFloor);
        if (_spread != null)
        {
            foreach (Collider2D hit in _spread.Hits)
            {
                IPotionEffectTarget target = PotionEffectApplier.FindTarget(hit);
                if (IsValidSingleTarget(target)) _pendingCombinationImpact.Add(target);
            }
            SynchronizeSingleTimeline();
        }
        else
        {
            SynchronizeSingleTimeline();
            ApplySingleImpact(effectCollider);
        }
    }

    internal void PrepareOriginalForSpread(PotionCombinationSpread spread, bool firstLobe, IDamageable owner)
    {
        _spread = spread;
        _originalFirstLobe = firstLobe;
        if (_singleProfile == null && _data != null)
            ActivateSingleProfile(owner, Time.time + _data.FloorDuration);
        else SynchronizeSingleTimeline();
    }

    public void DisableForCombination()
    {
        EndSingleTimeline(false);
        _originalTimeline?.End(false);
        _originalTimeline = null;
        _spread = null;
        _pendingCombinationImpact.Clear();
        _overlapCounts.Clear();
        _targetsByCollider.Clear();
        enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_data == null || _targetsByCollider.ContainsKey(other)) return;

        IPotionEffectTarget target = PotionEffectApplier.FindTarget(other);
        if (target == null)
        {
            return;
        }

        _targetsByCollider.Add(other, target);
        _overlapCounts.TryGetValue(target, out int overlapCount);
        _overlapCounts[target] = overlapCount + 1;
        if (overlapCount > 0)
        {
            return;
        }

        if (_singleTimeline != null)
        {
            SynchronizeSingleTimeline();
            return;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!_targetsByCollider.Remove(other, out IPotionEffectTarget target) ||
            !_overlapCounts.TryGetValue(target, out int overlapCount))
        {
            return;
        }

        if (overlapCount > 1)
        {
            _overlapCounts[target] = overlapCount - 1;
            return;
        }

        _overlapCounts.Remove(target);
        if (_singleTimeline != null)
        {
            SynchronizeSingleTimeline();
            return;
        }
    }

    private void Update()
    {
        SynchronizeSingleTimeline();
    }

    private bool IsValidSingleTarget(IPotionEffectTarget target) =>
        _singleProfile != null && PotionEffectApplier.CanAffect(_singleProfile.TargetType, target);

    private void EnterSingleFloor(IPotionEffectTarget target) =>
        target.EnterFloor(_singleProfile, _singleSource);

    private void PulseSingleFloor(IPotionEffectTarget target) => target.PulseFloor(_sourceId);

    private void ExitSingleFloor(IPotionEffectTarget target, bool applyResidual) =>
        target.ExitFloor(_sourceId, applyResidual);

    private void SynchronizeSingleTimeline()
    {
        if (_spread != null)
        {
            SynchronizeSpread();
            return;
        }
        _singleTimeline?.Synchronize(_overlapCounts.Keys, Time.timeAsDouble);
    }

    private void SynchronizeSpread()
    {
        _reachedTargets.Clear();
        _originalTargets.Clear();
        IReadOnlyList<Collider2D> hits = _spread.Hits;
        foreach (Collider2D hit in hits)
        {
            if (hit == null) continue;
            IPotionEffectTarget target = PotionEffectApplier.FindTarget(hit);
            if (target == null) continue;
            if (_spread.HasReached(hit)) _reachedTargets.Add(target);
            Vector2 center = _originalFirstLobe ? _spread.Shape.FirstCenter : _spread.Shape.SecondCenter;
            if (_spread.Shape.ContainsLobe(hit.ClosestPoint(center), _originalFirstLobe))
                _originalTargets.Add(target);
        }
        // A multi-collider target switches profiles once, as soon as the front reaches it.
        _originalTargets.ExceptWith(_reachedTargets);
        double now = Time.timeAsDouble;
        if (_hasCombination)
        {
            _originalTimeline?.Synchronize(_originalTargets, now);
            foreach (IPotionEffectTarget target in _reachedTargets)
            {
                if (_pendingCombinationImpact.Remove(target) && IsValidSingleTarget(target))
                {
                    PotionEffectApplier.ApplyProfilePhase(_singleProfile, PotionEffectPhase.Impact,
                        target, _singleSource);
                }
            }

            _singleTimeline?.Synchronize(_reachedTargets, now);
        }
        else
        {
            _singleTimeline?.Synchronize(_originalTargets, now);
        }

        if (!_spread.IsComplete) return;
        if (!_hasCombination)
        {
            DisableForCombination();
            return;
        }
        _originalTimeline?.End(false);
        _originalTimeline = null;
        _pendingCombinationImpact.Clear();
        // Once the front has finished, return to normal trigger tracking; no ongoing
        // full-area physics queries are needed for the remaining floor lifetime.
        _overlapCounts.Clear();
        _targetsByCollider.Clear();
        foreach (Collider2D hit in hits)
        {
            if (hit == null) continue;
            IPotionEffectTarget target = PotionEffectApplier.FindTarget(hit);
            if (target == null || _targetsByCollider.ContainsKey(hit)) continue;
            _targetsByCollider.Add(hit, target);
            _overlapCounts.TryGetValue(target, out int count);
            _overlapCounts[target] = count + 1;
        }
        _spread = null;
    }

    private void EndSingleTimeline(bool applyResidual)
    {
        _singleTimeline?.End(applyResidual);
        _singleTimeline = null;
        _singleProfile = null;
    }

    private void OnDisable()
    {
        // 조합 전환은 DisableForCombination에서 false로 먼저 끝낸다. 자연 종료는 잔류 효과를 남긴다.
        EndSingleTimeline(true);
        _originalTimeline?.End(false);
        _originalTimeline = null;
        _spread = null;
        _pendingCombinationImpact.Clear();
        _hasCombination = false;
        _overlapCounts.Clear();
        _targetsByCollider.Clear();
    }
}
