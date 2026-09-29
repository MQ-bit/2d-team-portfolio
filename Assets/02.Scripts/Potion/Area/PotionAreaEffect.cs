using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PotionAreaEffect : MonoBehaviour  // 계산된 결과로 피해·상태이상 적용
{
    private readonly Dictionary<IPotionEffectTarget, int> _overlapCounts = new();
    private readonly Dictionary<IPotionEffectTarget, Coroutine> _tickRoutines = new();
    private readonly Dictionary<Collider2D, IPotionEffectTarget> _targetsByCollider = new();
    private readonly HashSet<IPotionEffectTarget> _affectedTargets = new();
    private PotionData _data;
    private int _sourceId;
    private bool _hasCombination;
    private PotionCombinationEffectCalculator.Result _combination;
    private IDamageable _combinationOwner;
    private float _combinationExpiresAt;
    private bool _removeCombinationStatusesOnDisable = true;
    private PotionEffectProfile _singleProfile;
    private PotionEffectSource _singleSource;
    private PotionAreaTimeline<IPotionEffectTarget> _singleTimeline;

    public bool HasCombination => _hasCombination;
    private float TickInterval => _hasCombination ? _combination.TickInterval : _data.TickInterval;

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
        _singleProfile = _data == null ? null : _data.ThrownProfile;
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
        PotionEffectProfile profile, IDamageable owner, float lifeStealRatio,
        float expiresAt, Collider2D effectCollider)
    {
        EndSingleTimeline(false);
        StopAllCoroutines();
        _tickRoutines.Clear();
        RemoveAppliedStatuses();

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

    public void ReplaceWithCombination(PotionCombinationEffectCalculator.Result combination,
        IDamageable owner, float expiresAt, bool removeStatusesOnDisable = true)
    {
        EndSingleTimeline(false);
        StopAllCoroutines();
        _tickRoutines.Clear();
        RemoveAppliedStatuses();
        _hasCombination = true;
        _combination = combination;
        _combinationOwner = owner;
        _combinationExpiresAt = expiresAt;
        _removeCombinationStatusesOnDisable = removeStatusesOnDisable;

        // 교체 순간 이미 장판 안에 있던 대상도 다시 진입할 필요 없이 적용된다.
        var targets = new List<IPotionEffectTarget>(_overlapCounts.Keys);
        foreach (IPotionEffectTarget target in targets)
        {
            if (isActiveAndEnabled && _overlapCounts.ContainsKey(target)) StartTargetEffect(target);
        }
    }

    public void DisableForCombination()
    {
        EndSingleTimeline(false);
        StopAllCoroutines();
        RemoveAppliedStatuses();
        _overlapCounts.Clear();
        _tickRoutines.Clear();
        _targetsByCollider.Clear();
        enabled = false;
    }

    private void StartTargetEffect(IPotionEffectTarget target)
    {
        if (!PotionEffectApplier.IsTargetAlive(target)) return;
        ApplyToTarget(target);
        if (isActiveAndEnabled && _overlapCounts.ContainsKey(target) && TickInterval > 0f)
        {
            _tickRoutines[target] = StartCoroutine(ApplyPeriodicEffect(target));
        }
    }

    private void ApplyToTarget(IPotionEffectTarget target)
    {
        if (!PotionEffectApplier.IsTargetAlive(target)) return;
        _affectedTargets.Add(target);
        if (_hasCombination)
        {
            PotionEffectApplier.ApplyCombinationEffect(_combination, target,
                _combinationOwner, _sourceId, _combinationExpiresAt - Time.time);
        }
        else
        {
            PotionEffectApplier.ApplyAreaEffect(_data, target, _sourceId);
        }
    }

    private void RemoveAppliedStatuses()
    {
        foreach (IPotionEffectTarget target in _affectedTargets)
        {
            if (PotionEffectApplier.IsTargetAlive(target)) target.RemoveStatusesFromSource(_sourceId);
        }
        _affectedTargets.Clear();
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
        // Initialize와 Activate 사이에 물리 콜백이 먼저 와도 새 프로필을 레거시 수치로 실행하지 않는다.
        if (!_hasCombination && _data.ThrownProfile != null)
            return;

        StartTargetEffect(target);
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
        if (!_hasCombination && _data != null && _data.ThrownProfile != null)
            return;
        if (_tickRoutines.Remove(target, out Coroutine routine))
        {
            StopCoroutine(routine);
        }
    }

    private IEnumerator ApplyPeriodicEffect(IPotionEffectTarget target)
    {
        WaitForSeconds wait = new(Mathf.Max(0.01f, TickInterval));
        while (_overlapCounts.ContainsKey(target) && PotionEffectApplier.IsTargetAlive(target))
        {
            yield return wait;

            if (_overlapCounts.ContainsKey(target))
            {
                if (_hasCombination && Time.time >= _combinationExpiresAt) break;
                ApplyToTarget(target);
            }
        }
        _tickRoutines.Remove(target);
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
        StopAllCoroutines();
        if (_hasCombination && _removeCombinationStatusesOnDisable) RemoveAppliedStatuses();
        _affectedTargets.Clear();
        _hasCombination = false;
        _combinationOwner = null;
        _removeCombinationStatusesOnDisable = true;
        _overlapCounts.Clear();
        _tickRoutines.Clear();
        _targetsByCollider.Clear();
    }
}
