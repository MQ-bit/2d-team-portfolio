using System;
using UnityEngine;

public enum FloatingNumberKind
{
    EnemyDamage,
    PlayerDamage,
    Healing,
    ThirstChange
}

// 실제 수치 변화를 알린다. 사망/풀 반환 전에 위치를 복사하므로 표시가 대상의 수명에 묶이지 않는다.
public static class DamageFeedback
{
    public static event Action<Vector3, float, FloatingNumberKind> OnValueChanged;

    public static void Report(Vector3 position, float amount, bool isPlayer)
    {
        if (amount > 0f)
        {
            Publish(position, amount, isPlayer ? FloatingNumberKind.PlayerDamage : FloatingNumberKind.EnemyDamage);
        }
    }

    public static void ReportHealing(Vector3 position, float amount)
    {
        if (amount > 0f) Publish(position, amount, FloatingNumberKind.Healing);
    }

    // 금단증상(내부 이름 Thirst)은 증가/감소 방향을 그대로 전달한다.
    public static void ReportThirstChange(Vector3 position, float delta)
        => Publish(position, delta, FloatingNumberKind.ThirstChange);

    private static void Publish(Vector3 position, float amount, FloatingNumberKind kind)
    {
        if (amount == 0f || float.IsNaN(amount) || float.IsInfinity(amount)) return;
        OnValueChanged?.Invoke(position, amount, kind);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => OnValueChanged = null;
}
