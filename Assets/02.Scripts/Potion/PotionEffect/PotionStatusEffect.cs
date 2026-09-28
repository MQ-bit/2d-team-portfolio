using System.Collections.Generic;
using UnityEngine;

public enum PotionStatusEffect // 상태이상 종류와 유지 시간, 행동 가능 여부만 관리
{
    Burning,
    Poisoned,
    Frozen,
    Paralyzed
}

public sealed class PotionStatusEffectState
{
    private static readonly Dictionary<PotionElement, PotionStatusEffect> StatusByElement = new()
    {
        { PotionElement.Fire, PotionStatusEffect.Burning },
        { PotionElement.Poison, PotionStatusEffect.Poisoned },
        { PotionElement.Ice, PotionStatusEffect.Frozen },
        { PotionElement.Lightning, PotionStatusEffect.Paralyzed }
    };

    // 각 장판은 독립적으로 중첩된다. 같은 장판의 재적용은 해당 중첩만 갱신한다.
    private readonly Dictionary<PotionStatusEffect, Dictionary<int, float>> _expiresAtByStatus = new(4);
    private readonly List<int> _expiredSources = new();

    public bool CanAct => !IsActive(PotionStatusEffect.Frozen) && !IsActive(PotionStatusEffect.Paralyzed);

    public static bool TryGetStatus(PotionElement element, out PotionStatusEffect status)
    {
        return StatusByElement.TryGetValue(element, out status);
    }

    public bool IsActive(PotionStatusEffect status)
    {
        return GetStackCount(status) > 0;
    }

    public int GetStackCount(PotionStatusEffect status)
    {
        if (!_expiresAtByStatus.TryGetValue(status, out var stacks)) return 0;

        _expiredSources.Clear();
        foreach (var stack in stacks)
        {
            if (Time.time >= stack.Value)
            {
                _expiredSources.Add(stack.Key);
            }
        }

        foreach (int sourceId in _expiredSources)
        {
            stacks.Remove(sourceId);
        }

        if (stacks.Count == 0)
        {
            _expiresAtByStatus.Remove(status);
        }

        return stacks.Count;
    }

    public void Clear()
    {
        _expiresAtByStatus.Clear();
        _expiredSources.Clear();
    }

    public void Apply(PotionStatusEffect status, float duration)
    {
        Apply(status, duration, 0);
    }

    public void Apply(PotionStatusEffect status, float duration, int sourceId)
    {
        if (duration <= 0f)
        {
            return;
        }

        GetStackCount(status);
        if (!_expiresAtByStatus.TryGetValue(status, out var stacks))
        {
            stacks = new Dictionary<int, float>();
            _expiresAtByStatus.Add(status, stacks);
        }

        stacks.TryGetValue(sourceId, out float previousExpiresAt);
        stacks[sourceId] = Mathf.Max(previousExpiresAt, Time.time + duration);
    }
}
