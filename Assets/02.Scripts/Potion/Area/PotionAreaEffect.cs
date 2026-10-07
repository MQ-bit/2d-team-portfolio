using System.Collections.Generic;
using UnityEngine;

public class PotionAreaEffect : MonoBehaviour  // 계산된 결과로 피해·상태이상 적용
{
    private readonly PotionAreaContacts _contacts = new();
    private PotionData _data;
    private int _sourceId;
    private bool _hasCombination;
    private IPotionEffectData _singleProfile;
    private PotionEffectSource _singleSource;
    private PotionAreaTimeline<IPotionEffectTarget> _singleTimeline;
    private readonly PotionAreaSpreadEffect _spreadEffect = new();
    private readonly HashSet<IPotionEffectTarget> _affectedTargets = new();
    private bool _isSynchronizingTimeline;

    public bool HasCombination => _hasCombination;
    internal IPotionEffectData ActiveProfile => _singleProfile ?? _data;
    internal PotionEffectSource ActiveSource => _singleSource;

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
                target =>
                {
                    _affectedTargets.Add(target);
                    target.EnterFloor(profile, source);
                },
                target => target.PulseFloor(source.SourceId),
                (target, residual) => target.ExitFloor(source.SourceId,
                    residual && !_spreadEffect.HasReached(target)));
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
            if (_hasCombination && _spreadEffect.IsActive && !_spreadEffect.HasReached(hit)) continue;
            IPotionEffectTarget target = PotionEffectApplier.FindTarget(hit);
            if (!IsValidSingleTarget(target) || !targets.Add(target)) continue;
            _affectedTargets.Add(target);
            PotionEffectApplier.ApplyProfilePhase(_singleProfile, PotionEffectPhase.Impact,
                target, _singleSource);
        }
    }

    public void ReplaceWithCombinationProfile(PotionCombinationData combination,
        IPotionEffectData profile, IDamageable owner, float lifeStealRatio,
        float expiresAt, Collider2D effectCollider)
    {
        if (_spreadEffect.IsActive)
        {
            _spreadEffect.KeepOriginalTimeline(_singleTimeline);
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
        if (_spreadEffect.IsActive)
        {
            _spreadEffect.QueueCombinationImpact(IsValidSingleTarget);
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
        _spreadEffect.Prepare(spread, firstLobe);
        if (_singleProfile == null && _data != null)
            ActivateSingleProfile(owner, Time.time + _data.FloorDuration);
        else SynchronizeSingleTimeline();
    }

    public void DisableForCombination()
    {
        EndSingleTimeline(false);
        _spreadEffect.Clear();
        _contacts.Clear();
        enabled = false;
    }

    internal void StopForVaporization()
    {
        // Include departed targets with residual statuses, and the original source
        // that survived while the combination front was still spreading.
        int originalSourceId = GetInstanceID();
        foreach (IPotionEffectTarget target in _affectedTargets)
        {
            if (target == null || target is Object unityObject && unityObject == null) continue;
            target.RemoveStatusesFromSource(originalSourceId);
            target.RemoveStatusesFromSource(_sourceId);
            target.ExitFloor(originalSourceId, false);
            if (_sourceId != originalSourceId) target.ExitFloor(_sourceId, false);
        }
        _affectedTargets.Clear();
        DisableForCombination();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_data != null && _contacts.Enter(other) && _singleTimeline != null)
            SynchronizeSingleTimeline();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (_contacts.Exit(other) && _singleTimeline != null)
            SynchronizeSingleTimeline();
    }

    private void Update()
    {
        SynchronizeSingleTimeline();
    }

    private bool IsValidSingleTarget(IPotionEffectTarget target) =>
        _singleProfile != null && PotionEffectApplier.CanAffect(_singleProfile.TargetType, target);

    private void EnterSingleFloor(IPotionEffectTarget target)
    {
        _affectedTargets.Add(target);
        target.EnterFloor(_singleProfile, _singleSource);
    }

    private void PulseSingleFloor(IPotionEffectTarget target) => target.PulseFloor(_sourceId);

    private void ExitSingleFloor(IPotionEffectTarget target, bool applyResidual) =>
        target.ExitFloor(_sourceId, applyResidual);

    private void SynchronizeSingleTimeline()
    {
        // Trigger/impact callbacks may request another synchronization mid-update.
        // Contacts still update immediately; the next update reconciles them.
        if (_isSynchronizingTimeline) return;
        _isSynchronizingTimeline = true;
        try
        {
            if (_spreadEffect.IsActive)
            {
                if (!_spreadEffect.Synchronize(_singleTimeline, _hasCombination, ApplyCombinationImpact))
                    return;
                if (!_hasCombination)
                {
                    DisableForCombination();
                    return;
                }
                _contacts.Rebuild(_spreadEffect.Hits);
                _spreadEffect.Clear();
                return;
            }
            _singleTimeline?.Synchronize(_contacts.Targets, Time.timeAsDouble);
        }
        finally
        {
            _isSynchronizingTimeline = false;
        }
    }

    private void ApplyCombinationImpact(IPotionEffectTarget target)
    {
        if (!IsValidSingleTarget(target)) return;
        _affectedTargets.Add(target);
        PotionEffectApplier.ApplyProfilePhase(_singleProfile, PotionEffectPhase.Impact,
            target, _singleSource);
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
        _spreadEffect.Clear();
        _hasCombination = false;
        _contacts.Clear();
    }
}
