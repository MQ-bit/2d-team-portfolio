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
            _singleTimeline = new PotionAreaTimeline<IPotionEffectTarget>(expiresAt,
                _singleProfile.TickInterval, _singleProfile.ApplyOnEnter,
                IsValidSingleTarget, EnterSingleFloor, PulseSingleFloor, ExitSingleFloor);
            SynchronizeSingleTimeline();
        }
    }

    public void ApplySingleImpact(Collider2D effectCollider = null)
    {
        if (_singleProfile == null) return;

        Physics2D.SyncTransforms();
        Collider2D areaCollider = effectCollider != null ? effectCollider : GetComponent<Collider2D>();
        if (areaCollider == null) return;

        var hits = new List<Collider2D>();
        var targets = new HashSet<IPotionEffectTarget>();
        ContactFilter2D filter = new() { useTriggers = true };
        areaCollider.Overlap(filter, hits);
        foreach (Collider2D hit in hits)
        {
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
        EndSingleTimeline(false);

        _hasCombination = true;
        _singleProfile = profile;
        if (_singleProfile == null) return;
        _singleSource = new PotionEffectSource(_sourceId, combination: combination,
            owner: owner, lifeStealRatio: lifeStealRatio);
        _singleTimeline = new PotionAreaTimeline<IPotionEffectTarget>(expiresAt,
            _singleProfile.TickInterval, _singleProfile.ApplyOnEnter,
            IsValidSingleTarget, EnterSingleFloor, PulseSingleFloor, ExitSingleFloor);
        SynchronizeSingleTimeline();
        ApplySingleImpact(effectCollider);
    }

    public void DisableForCombination()
    {
        EndSingleTimeline(false);
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
        _singleTimeline?.Synchronize(_overlapCounts.Keys, Time.timeAsDouble);
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
        _hasCombination = false;
        _overlapCounts.Clear();
        _targetsByCollider.Clear();
    }
}
