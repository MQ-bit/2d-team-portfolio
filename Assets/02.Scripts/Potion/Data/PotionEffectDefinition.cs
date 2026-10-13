using System;
using System.Collections.Generic;
using UnityEngine;

// 수신자의 PotionTargetKind와 분리한다. Both는 수신자 종류가 아닌 선택 조건이다.
public enum PotionTargetType
{
    Player = 0,
    Enemy = 1,
    Both = 2
}

public enum PotionEffectPhase
{
    Impact = 0,
    FloorTick = 1,
    FloorExit = 2,
    Drink = 3
}

public enum PotionEffectOperation
{
    Damage = 0,
    Heal = 1,
    ApplyStatus = 2,
    AddFrostStacks = 3
}

/// 한 시점에 실행할 수치 설정. 대상, 남은 시간, 현재 스택은 저장하지 않는다
[Serializable]
public sealed class PotionEffectDefinition
{
    [SerializeField] private PotionEffectPhase _phase;
    [SerializeField] private PotionEffectOperation _operation;
    [SerializeField] private PotionElement _element;
    [Tooltip("Damage/Heal: 즉시량. 화상/중독 ApplyStatus: 상태 Tick당 피해량.")]
    [SerializeField, Min(0f)] private float _amount;

    [Header("Apply Status")]
    [SerializeField] private PotionStatusEffectTracker _status;
    [Tooltip("상태 전체 유지시간. 장판 수명과 별개로 대상에서 관리한다.")]
    [SerializeField, Min(0f)] private float _duration;
    [Tooltip("상태 내부 반복 간격. 0이면 반복 없음. 장판 Tick 간격과 별개다.")]
    [SerializeField, Min(0f)] private float _tickInterval;
    [Tooltip("반복 상태의 첫 효과를 즉시 실행할지 여부. false이면 한 주기 후 실행.")]
    [SerializeField] private bool _applyImmediately;
    [Tooltip("상태의 일반 이동 배율. 대시는 둔화를 무시하며 빙결/마비만 적용한다.")]
    [SerializeField, Range(0f, 1f)] private float _moveSpeedMultiplier = 1f;
    [Tooltip("마비 1회 정지 시간. 예: 장판 0.1초, 섭취는 1초 동안 즉시 및 0.5초 뒤에 각 0.05초.")]
    [SerializeField, Min(0f)] private float _immobilizeDuration;

    [Header("Add Frost Stacks")]
    [SerializeField, Min(1)] private int _frostStackAmount = 1;

    public PotionEffectPhase Phase => _phase;
    public PotionEffectOperation Operation => _operation;
    public PotionElement Element => _element;
    public float Amount => _amount;
    public PotionStatusEffectTracker Status => _status;
    public float Duration => _duration;
    public float TickInterval => _tickInterval;
    public bool ApplyImmediately => _applyImmediately;
    public float MoveSpeedMultiplier => _moveSpeedMultiplier;
    public float ImmobilizeDuration => _immobilizeDuration;
    public int FrostStackAmount => _frostStackAmount;

    public static PotionEffectDefinition CreateInstant(PotionEffectOperation operation,
        PotionElement element, float amount) => new PotionEffectDefinition
        {
            _phase = PotionEffectPhase.Impact,
            _operation = operation,
            _element = element,
            _amount = PotionDataValidation.NonNegative(amount)
        };

    internal static PotionEffectDefinition CreateInstantStatus(PotionStatusEffectTracker status,
        PotionElement element, float duration) => new()
        {
            _phase = PotionEffectPhase.Impact,
            _operation = PotionEffectOperation.ApplyStatus,
            _element = element,
            _status = status,
            _duration = PotionDataValidation.NonNegative(duration),
            _immobilizeDuration = PotionDataValidation.NonNegative(duration)
        };

    internal void Validate()
    {
        _amount = PotionDataValidation.NonNegative(_amount);
        _duration = PotionDataValidation.NonNegative(_duration);
        _tickInterval = PotionDataValidation.NonNegative(_tickInterval);
        _immobilizeDuration = PotionDataValidation.NonNegative(_immobilizeDuration);
        _moveSpeedMultiplier = PotionDataValidation.Multiplier(_moveSpeedMultiplier);
        _frostStackAmount = Mathf.Max(1, _frostStackAmount);
    }
}

/// 서리의 공유 설정. 스택 초기화 및 빙결 중 누적 금지
[Serializable]
public sealed class PotionFrostSettings
{
    [SerializeField, Min(1)] private int _freezeThreshold = 3;
    [Tooltip("1스택부터 빙결 직전까지의 일반 이동 배율. 임계 스택 변경 시 목록도 맞춘다.")]
    [SerializeField] private List<float> _moveSpeedMultipliers = new() { 0.85f, 0.70f };
    [SerializeField, Min(0f)] private float _freezeDuration = 2f;

    public int FreezeThreshold => _freezeThreshold;
    public IReadOnlyList<float> MoveSpeedMultipliers => _moveSpeedMultipliers;
    public float FreezeDuration => _freezeDuration;

    internal void Validate()
    {
        _freezeThreshold = Mathf.Max(1, _freezeThreshold);
        _freezeDuration = PotionDataValidation.NonNegative(_freezeDuration);
        _moveSpeedMultipliers ??= new List<float>();
        // 목록 길이와 임계값이 불일치하면 기존 값을 유지하며 누락 단계는 정상 배율로 채운다.
        int stageCount = _freezeThreshold - 1;
        if (_moveSpeedMultipliers.Count > stageCount)
            _moveSpeedMultipliers.RemoveRange(stageCount, _moveSpeedMultipliers.Count - stageCount);
        while (_moveSpeedMultipliers.Count < stageCount) _moveSpeedMultipliers.Add(1f);
        for (int i = 0; i < _moveSpeedMultipliers.Count; i++)
            _moveSpeedMultipliers[i] = PotionDataValidation.Multiplier(_moveSpeedMultipliers[i]);
    }
}

internal static class PotionDataValidation
{
    internal static float Finite(float value) =>
        float.IsNaN(value) || float.IsInfinity(value) ? 0f : value;

    internal static float NonNegative(float value) => Mathf.Max(0f, Finite(value));

    internal static float Multiplier(float value) =>
        float.IsNaN(value) || float.IsInfinity(value) ? 1f : Mathf.Clamp01(value);
}
