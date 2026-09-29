using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PotionAreaEffect : MonoBehaviour
{
    private readonly Dictionary<IPotionEffectTarget, int> _overlapCounts = new();
    private readonly Dictionary<IPotionEffectTarget, Coroutine> _tickRoutines = new();
    private readonly Dictionary<Collider2D, IPotionEffectTarget> _targetsByCollider = new();
    private readonly HashSet<IPotionEffectTarget> _affectedTargets = new();
    private PotionData _data;
    private int _sourceId;
    private bool _hasCombination;
    private PotionCombinationEffectResolver.Result _combination;
    private IDamageable _combinationOwner;
    private float _combinationExpiresAt;
    private bool _removeCombinationStatusesOnDisable = true;

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

    public void ReplaceWithCombination(PotionCombinationEffectResolver.Result combination,
        IDamageable owner, float expiresAt, bool removeStatusesOnDisable = true)
    {
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
        StopAllCoroutines();
        RemoveAppliedStatuses();
        _overlapCounts.Clear();
        _tickRoutines.Clear();
        _targetsByCollider.Clear();
        enabled = false;
    }

    private void StartTargetEffect(IPotionEffectTarget target)
    {
        if (!PotionEffectResolver.IsTargetAlive(target)) return;
        ApplyToTarget(target);
        if (isActiveAndEnabled && _overlapCounts.ContainsKey(target) && TickInterval > 0f)
        {
            _tickRoutines[target] = StartCoroutine(ApplyPeriodicEffect(target));
        }
    }

    private void ApplyToTarget(IPotionEffectTarget target)
    {
        if (!PotionEffectResolver.IsTargetAlive(target)) return;
        _affectedTargets.Add(target);
        if (_hasCombination)
        {
            PotionEffectResolver.ApplyCombinationEffect(_combination, target,
                _combinationOwner, _sourceId, _combinationExpiresAt - Time.time);
        }
        else
        {
            PotionEffectResolver.ApplyAreaEffect(_data, target, _sourceId);
        }
    }

    private void RemoveAppliedStatuses()
    {
        foreach (IPotionEffectTarget target in _affectedTargets)
        {
            if (PotionEffectResolver.IsTargetAlive(target)) target.RemoveStatusesFromSource(_sourceId);
        }
        _affectedTargets.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_data == null || _targetsByCollider.ContainsKey(other)) return;

        IPotionEffectTarget target = PotionEffectResolver.FindTarget(other);
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
        if (_tickRoutines.Remove(target, out Coroutine routine))
        {
            StopCoroutine(routine);
        }
    }

    private IEnumerator ApplyPeriodicEffect(IPotionEffectTarget target)
    {
        WaitForSeconds wait = new(Mathf.Max(0.01f, TickInterval));
        while (_overlapCounts.ContainsKey(target) && PotionEffectResolver.IsTargetAlive(target))
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

    private void OnDisable()
    {
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
