using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

internal static class Program
{
    private static int _passed;
    private static void Main()
    {
        Run("DoT final complete tick and fractional duration", DotBoundaries);
        Run("Independent burning and poison", DifferentStatuses);
        Run("Refresh preserves cadence without stacking", RefreshCadence);
        Run("Stronger damage retains source, equal damage changes owner", SourceRefresh);
        Run("Area exit preserves residual damage", ResidualLifetime);
        Run("Frost buildup, overlap, freeze, thaw", Frost);
        Run("Single and periodic paralysis", Paralysis);
        Run("Strongest slow and source removal", Slow);
        Run("Receiver death stops queued ticks and ignores new status", Death);
        Run("Receiver damage attribution and actual damage", Attribution);
        Run("Disable clears statuses for reuse", Disable);
        Run("Damage callback may clear or refresh safely", Reentrant);
        Run("Player walk slows, dash ignores slow and respects freeze", PlayerMovement);
        Run("Interface dispatch directions and finite inputs", Dispatch);
        Console.WriteLine($"PASS: {_passed} scenarios");
    }
    private static void Run(string name, Action action)
    {
        Time.timeAsDouble = 0;
        action();
        _passed++;
        Console.WriteLine("PASS " + name);
    }
    private static void Equal(float expected, float actual)
    {
        if (Math.Abs(expected - actual) > 0.0001f) throw new Exception($"Expected {expected}, got {actual}");
    }
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
    private static PotionStatusApplication Dot(PotionStatusEffect status = PotionStatusEffect.Burning,
        float duration = 2, float amount = 4, int source = 1, IDamageable owner = null) =>
        new(status, duration, new PotionEffectSource(source, owner: owner),
            status == PotionStatusEffect.Poisoned ? PotionElement.Poison : PotionElement.Fire,
            amount, 1);
    private static void Advance(PotionStatusEffectState state, double time, Action<PotionStatusDamage> callback)
    {
        Time.timeAsDouble = time;
        state.Tick(callback);
    }
    private static void DotBoundaries()
    {
        var state = new PotionStatusEffectState();
        float damage = 0;
        state.Apply(Dot());
        Advance(state, 0.99, tick => damage += tick.Amount);
        Equal(0, damage);
        Advance(state, 1, tick => damage += tick.Amount);
        Equal(4, damage);
        Time.timeAsDouble = 2;
        Check(!state.IsActive(PotionStatusEffect.Burning), "expired status must not appear active");
        state.Tick(tick => damage += tick.Amount);
        Equal(8, damage);
        Advance(state, 10, tick => damage += tick.Amount);
        Equal(8, damage);
        state.Apply(Dot(duration: 1.9f));
        Advance(state, 12, tick => damage += tick.Amount);
        Equal(12, damage);
    }
    private static void DifferentStatuses()
    {
        var state = new PotionStatusEffectState();
        float damage = 0;
        state.Apply(Dot());
        state.Apply(Dot(PotionStatusEffect.Poisoned, 3, 5));
        Advance(state, 3, tick => damage += tick.Amount);
        Equal(23, damage);
    }
    private static void RefreshCadence()
    {
        var state = new PotionStatusEffectState();
        int count = 0;
        state.Apply(Dot());
        Time.timeAsDouble = 0.5;
        state.Apply(Dot(source: 2));
        Equal(1, state.GetStackCount(PotionStatusEffect.Burning));
        Advance(state, 1, _ => count++);
        Advance(state, 2.5, _ => count++);
        Equal(2, count);
        Check(!state.IsActive(PotionStatusEffect.Burning), "refresh expires at latest application plus duration");
    }
    private static void SourceRefresh()
    {
        var state = new PotionStatusEffectState();
        var owner = new FakeHealth();
        state.Apply(Dot(amount: 6, owner: owner));
        Time.timeAsDouble = 0.2;
        state.Apply(Dot(amount: 4, source: 2));
        Advance(state, 1, tick =>
        {
            Equal(6, tick.Amount);
            Equal(1, tick.Source.SourceId);
            Check(ReferenceEquals(owner, tick.Source.Owner), "strong source lost");
        });
        state.Apply(Dot(amount: 6, source: 3));
        Advance(state, 2, tick => Equal(3, tick.Source.SourceId));
    }
    private static void ResidualLifetime()
    {
        var state = new PotionStatusEffectState();
        state.Apply(Dot());
        state.SetAreaSlow(1, 0.6f);
        state.RemoveAreaSource(1);
        Equal(1, state.MoveSpeedMultiplier);
        float damage = 0;
        Advance(state, 2, tick => damage += tick.Amount);
        Equal(8, damage);
    }
    private static void Frost()
    {
        var state = new PotionStatusEffectState();
        var settings = new PotionFrostSettings();
        var a = new PotionEffectSource(1);
        var b = new PotionEffectSource(2);
        state.AddFrostStacks(settings, a);
        state.AddFrostStacks(settings, b);
        Equal(1, state.FrostStackCount);
        Equal(0.85f, state.MoveSpeedMultiplier);
        state.ExitFrostArea(1);
        Equal(1, state.FrostStackCount);
        Time.timeAsDouble = 1;
        state.AddFrostStacks(settings, b);
        Equal(0.7f, state.MoveSpeedMultiplier);
        Check(state.CanMove, "slow must allow dash");
        Time.timeAsDouble = 2;
        state.AddFrostStacks(settings, b);
        Equal(0, state.FrostStackCount);
        Check(!state.CanMove, "third stack must freeze");
        Time.timeAsDouble = 3;
        state.AddFrostStacks(settings, b);
        Equal(0, state.FrostStackCount);
        state.ExitFrostArea(2);
        Check(!state.CanMove, "area exit must not cancel triggered freeze");
        Time.timeAsDouble = 4;
        Check(state.CanMove, "freeze must end after two seconds");
        state.AddFrostStacks(settings, a);
        Equal(1, state.FrostStackCount);
        state.ExitFrostArea(1);
        Equal(0, state.FrostStackCount);
        Equal(1, state.MoveSpeedMultiplier);
    }
    private static void Paralysis()
    {
        var state = new PotionStatusEffectState();
        state.Apply(new PotionStatusApplication(PotionStatusEffect.Paralyzed, 0.1f, default));
        Check(!state.CanMove, "single pulse missing");
        Time.timeAsDouble = 0.11;
        Check(state.CanMove, "single pulse stuck");
        Time.timeAsDouble = 1;
        state.Apply(new PotionStatusApplication(PotionStatusEffect.Paralyzed, 2, default,
            tickInterval: 0.4f, applyImmediately: true, immobilizeDuration: 0.1f));
        Check(!state.CanMove, "initial drink pulse missing");
        Time.timeAsDouble = 1.11;
        Check(state.CanMove, "pulse never releases");
        Time.timeAsDouble = 1.401;
        Check(!state.CanMove, "repeat pulse missing");
        state.Apply(new PotionStatusApplication(PotionStatusEffect.Frozen, 0.2f, default));
        Time.timeAsDouble = 1.55;
        Check(!state.CanMove, "freeze should outlast short pulse");
        Time.timeAsDouble = 3;
        Check(state.CanMove, "paralysis must end at duration boundary");
        state.Apply(new PotionStatusApplication(PotionStatusEffect.Paralyzed, 2, default,
            tickInterval: 0.4f, immobilizeDuration: 0.1f));
        Check(state.CanMove, "delayed pulse applied immediately");
        Time.timeAsDouble = 3.401;
        Check(!state.CanMove, "delayed first pulse missing");
    }
    private static void Slow()
    {
        var state = new PotionStatusEffectState();
        state.SetAreaSlow(1, 0.6f);
        state.SetAreaSlow(2, 0.7f);
        Equal(0.6f, state.MoveSpeedMultiplier);
        state.RemoveAreaSource(1);
        Equal(0.7f, state.MoveSpeedMultiplier);
        state.Apply(new PotionStatusApplication(PotionStatusEffect.Slowed, 1, default,
            moveSpeedMultiplier: 0.5f));
        Equal(0.5f, state.MoveSpeedMultiplier);
        Check(state.CanMove, "slow blocks dash");
        Time.timeAsDouble = 1;
        Equal(0.7f, state.MoveSpeedMultiplier);
        state.RemoveAreaSource(2);
        Equal(1, state.MoveSpeedMultiplier);
    }
    private static PotionEffectReceiver Receiver(FakeHealth health)
    {
        var receiver = new PotionEffectReceiver();
        receiver.Components.Add(health);
        Call(receiver, "Awake");
        return receiver;
    }
    private static void Death()
    {
        var health = new FakeHealth(5);
        var receiver = Receiver(health);
        receiver.ApplyStatus(Dot(duration: 10));
        Time.timeAsDouble = 10;
        Call(receiver, "Update");
        Equal(0, health.CurrentHealth);
        Equal(2, health.Hits);
        receiver.ApplyStatus(Dot());
        Equal(0, receiver.GetStatusStackCount(PotionStatusEffect.Burning));
    }
    private static void Attribution()
    {
        var health = new FakeHealth(3);
        var owner = new FakeHealth(20);
        var potion = new PotionData();
        var combination = new PotionCombinationData();
        var receiver = Receiver(health);
        PotionDamageResult actual = default;
        receiver.StatusDamageApplied += result => actual = result;
        receiver.ApplyStatus(new PotionStatusApplication(PotionStatusEffect.Burning, 2,
            new PotionEffectSource(73, potion, combination, owner, 0.2f), PotionElement.Fire, 8, 1));
        Time.timeAsDouble = 1;
        Call(receiver, "Update");
        Equal(3, actual.ActualDamage);
        Equal(0.6f, actual.LifeStealAmount);
        Check(ReferenceEquals(potion, actual.Source.Potion), "potion attribution lost");
        Check(ReferenceEquals(combination, actual.Source.Combination), "combination attribution lost");
        Check(ReferenceEquals(owner, actual.Source.Owner), "owner attribution lost");
        Equal(20, owner.CurrentHealth); // 지급은 7단계에서 연결한다.
    }
    private static void Disable()
    {
        var health = new FakeHealth();
        var receiver = Receiver(health);
        receiver.ApplyStatus(Dot());
        receiver.SetAreaSlow(1, 0.6f);
        receiver.isActiveAndEnabled = false;
        Call(receiver, "OnDisable");
        receiver.isActiveAndEnabled = true;
        Time.timeAsDouble = 5;
        Call(receiver, "Update");
        Equal(0, health.Hits);
        Equal(1, receiver.MoveSpeedMultiplier);
    }
    private static void Reentrant()
    {
        var health = new FakeHealth();
        var receiver = Receiver(health);
        receiver.StatusDamageApplied += _ => receiver.ClearStatuses();
        receiver.ApplyStatus(Dot(duration: 10));
        Time.timeAsDouble = 10;
        Call(receiver, "Update");
        Equal(1, health.Hits);
        Time.timeAsDouble = 0;
        health = new FakeHealth();
        receiver = Receiver(health);
        receiver.StatusDamageApplied += _ => receiver.ApplyStatus(Dot());
        receiver.ApplyStatus(Dot());
        Time.timeAsDouble = 1;
        Call(receiver, "Update");
        Call(receiver, "Update");
        Equal(1, health.Hits);
    }
    private static void PlayerMovement()
    {
        var receiver = new PotionEffectReceiver();
        var player = new PlayerStatus();
        receiver.Components.Add(player);
        player.Components.Add(receiver);
        Call(player, "Awake");
        Call(receiver, "Awake");
        receiver.SetAreaSlow(8, 0.7f);
        Equal(3.5f, player.Speed);
        Equal(15, player.DashSpeed);
        receiver.ApplyStatus(new PotionStatusApplication(PotionStatusEffect.Frozen, 2, default));
        Equal(0, player.Speed);
        Equal(0, player.DashSpeed);
        Time.timeAsDouble = 2;
        Equal(3.5f, player.Speed);
        Equal(15, player.DashSpeed);
        receiver.RemoveAreaEffectsFromSource(8);
        Equal(5, player.Speed);
        Equal(15, player.DashSpeed);
    }
    private static void Dispatch()
    {
        var health = new FakeHealth(10);
        PotionStatDispatcher.Damage(health, float.NaN, default);
        PotionStatDispatcher.Damage(health, -1, default);
        Equal(0, health.Hits);
        PotionStatDispatcher.Heal(health, 2);
        Equal(12, health.CurrentHealth);
        PotionStatDispatcher.ChangeThirst(health, -30);
        Equal(20, health.Thirst);
        PotionStatDispatcher.ChangeThirst(health, 30);
        Equal(50, health.Thirst);
        PotionStatDispatcher.ChangeThirst(health, float.PositiveInfinity);
        Equal(50, health.Thirst);
    }
    private static void Call(object target, string name) => target.GetType()
        .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);

    private sealed class FakeHealth : IDamageable, IThirstable
    {
        public FakeHealth(float hp = 100) => CurrentHealth = hp;
        public float CurrentHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0;
        public int Hits;
        public float Thirst = 50;
        public event Action<float, float> OnHealthChanged;
        public event Action OnDeath;
        public event Action<float, float> OnThirstChanged;
        public void RestoreHealth(float amount) => CurrentHealth += amount;
        public void DecreaseHealth(float amount) { Hits++; CurrentHealth = Math.Max(0, CurrentHealth - amount); }
        public void RestoreThirst(float amount) => Thirst += amount;
        public void DecreaseThirst(float amount) => Thirst -= amount;
    }
}
