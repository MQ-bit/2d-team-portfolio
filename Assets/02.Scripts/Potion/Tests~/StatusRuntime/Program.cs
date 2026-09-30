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
        Run("Single floor totals and lifetime boundary", SingleFloorTotals);
        Run("Area timeline deduplicates overlap and exits once", AreaTimelineBoundary);
        Run("Drink data, thirst direction, and entry point", DrinkProfiles);
        Run("Six persistent combination profiles", PersistentCombinations);
        Run("Vampiric inheritance, residual owner, and vapor impacts", StageSevenProfiles);
        Run("Merged data validation and combination effect dispatch", MergedData);
        Console.WriteLine($"PASS: {_passed} scenarios");
    }
    private static void MergedData()
    {
        var potion = Profile(0.5f);
        Set(potion, "_effectRadius", 3f);
        Equal(6f, potion.AreaWidth);
        Set(potion, "_thirstDelta", float.NaN);
        Set(potion, "_floorDuration", -1f);
        Call(potion, "OnValidate");
        Equal(0, potion.ThirstDelta);
        Equal(0, potion.FloorDuration);

        var combination = ScriptableObject.CreateInstance<PotionCombinationData>();
        Set(combination, "_targetType", PotionTargetType.Enemy);
        Set(combination, "_effects", new List<PotionEffectDefinition> {
            Definition(PotionEffectPhase.Impact, PotionEffectOperation.Damage, PotionElement.Fire, 30)
        });
        var health = new FakeHealth();
        PotionEffectApplier.ApplyProfilePhase(combination, PotionEffectPhase.Impact,
            Receiver(health), new PotionEffectSource(1, combination: combination));
        Equal(70, health.CurrentHealth);
        Set(combination, "_tickInterval", float.PositiveInfinity);
        Set(combination, "_lifeStealRatio", 2f);
        Call(combination, "OnValidate");
        Equal(0, combination.TickInterval);
        Equal(1, combination.LifeStealRatio);
        Check(typeof(PotionCombinationData).GetProperty("ThirstDelta") == null,
            "Combination data must not expose thirst");
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
        var potion = ScriptableObject.CreateInstance<PotionData>();
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
        Equal(20.6f, owner.CurrentHealth);
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

    private static void SingleFloorTotals()
    {
        Equal(124, RunFloor(PotionEffectOperation.Heal, PotionElement.Heal, 6, 4, 1));
        Equal(68, RunFloor(PotionEffectOperation.Damage, PotionElement.Fire, 8, 4, 1));
        Equal(75, RunFloor(PotionEffectOperation.Damage, PotionElement.Poison, 5, 5, 1));
        Equal(80, RunFloor(PotionEffectOperation.Damage, PotionElement.Ice, 5, 4, 1, true));
        Equal(76, RunFloor(PotionEffectOperation.Damage, PotionElement.Lightning, 6, 2, 0.5f));
    }

    private static float RunFloor(PotionEffectOperation operation, PotionElement element,
        float amount, float duration, float interval, bool frost = false)
    {
        Time.timeAsDouble = 0;
        var health = new FakeHealth();
        var receiver = Receiver(health);
        PotionData profile = Profile(interval,
            Definition(PotionEffectPhase.FloorTick, operation, element, amount));
        if (frost)
        {
            AddEffect(profile, Definition(PotionEffectPhase.FloorTick,
                PotionEffectOperation.AddFrostStacks, PotionElement.Ice, 0));
        }
        var source = new PotionEffectSource(41);
        var timeline = new PotionAreaTimeline<PotionEffectReceiver>(duration, interval, true,
            _ => true,
            target => target.EnterFloor(profile, source),
            target => target.PulseFloor(source.SourceId),
            (target, residual) => target.ExitFloor(source.SourceId, residual));
        for (double now = 0; now <= duration; now += interval)
        {
            Time.timeAsDouble = now;
            timeline.Synchronize(new[] { receiver, receiver }, now);
        }
        Check(!frost || receiver.FrostStackCount == 0, "frost stacks must reset when floor expires");
        return health.CurrentHealth;
    }

    private static void AreaTimelineBoundary()
    {
        int enters = 0, ticks = 0, exits = 0;
        var timeline = new PotionAreaTimeline<int>(2, 1, true, _ => true,
            _ => enters++, _ => ticks++, (_, residual) => { if (residual) exits++; });
        timeline.Synchronize(new[] { 7, 7 }, 0);
        timeline.Synchronize(new[] { 7, 7 }, 1);
        timeline.Synchronize(new[] { 7 }, 2);
        timeline.Synchronize(Array.Empty<int>(), 3);
        Equal(1, enters);
        Equal(2, ticks);
        Equal(1, exits);
    }

    private static void DrinkProfiles()
    {
        DrinkCase(130, 10, -40,
            Definition(PotionEffectPhase.Drink, PotionEffectOperation.Heal, PotionElement.Heal, 30));
        DrinkDotCase(PotionStatusEffect.Burning, PotionElement.Fire, 6, 4, 76);
        DrinkDotCase(PotionStatusEffect.Poisoned, PotionElement.Poison, 5, 5, 75);

        var frozen = Definition(PotionEffectPhase.Drink, PotionEffectOperation.ApplyStatus,
            PotionElement.Ice, 0, PotionStatusEffect.Frozen, 2, moveMultiplier: 0);
        var ice = DrinkCase(85, 20, -30,
            Definition(PotionEffectPhase.Drink, PotionEffectOperation.Damage, PotionElement.Ice, 15), frozen);
        Check(!ice.CanMove, "frost drink must freeze immediately");

        var paralysis = Definition(PotionEffectPhase.Drink, PotionEffectOperation.ApplyStatus,
            PotionElement.Lightning, 0, PotionStatusEffect.Paralyzed, 2, 0.4f, true, 0, 0.1f);
        var lightning = DrinkCase(80, 20, -30,
            Definition(PotionEffectPhase.Drink, PotionEffectOperation.Damage, PotionElement.Lightning, 20),
            paralysis);
        Check(!lightning.CanMove, "lightning drink must start paralysis immediately");

        var vaporHealth = new FakeHealth();
        var vaporReceiver = Receiver(vaporHealth);
        vaporReceiver.ApplyStatus(Dot(duration: 10));
        PotionData vaporProfile = Profile(1,
            Definition(PotionEffectPhase.Drink, PotionEffectOperation.Damage,
                PotionElement.Vaporization, 15));
        Set(vaporProfile, "_thirstDelta", 30f);
        PotionEffectApplier.ApplyDrinkEffect(
            vaporProfile, vaporReceiver);
        Equal(85, vaporHealth.CurrentHealth);
        Equal(80, vaporHealth.Thirst);
        Equal(1, vaporReceiver.GetStatusStackCount(PotionStatusEffect.Burning));
    }

    private static void DrinkDotCase(PotionStatusEffect status, PotionElement element,
        float amount, int duration, float expectedHealth)
    {
        var dot = Definition(PotionEffectPhase.Drink, PotionEffectOperation.ApplyStatus,
            element, amount, status, duration, 1);
        var receiver = DrinkCase(100, 20, -30, dot);
        var health = (FakeHealth)receiver.Damageable;
        Time.timeAsDouble = duration;
        Call(receiver, "Update");
        Equal(expectedHealth, health.CurrentHealth);
    }

    private static PotionEffectReceiver DrinkCase(float expectedHealth, float expectedThirst,
        float thirstDelta, params PotionEffectDefinition[] effects)
    {
        Time.timeAsDouble = 0;
        var health = new FakeHealth();
        var receiver = Receiver(health); // 기본 Enemy이므로 투척 TargetType 필터를 사용하면 실패한다.
        PotionData profile = Profile(1, effects);
        Set(profile, "_thirstDelta", thirstDelta);
        var potion = profile;
        PotionEffectApplier.ApplyDrinkEffect(potion, receiver);
        Equal(expectedHealth, health.CurrentHealth);
        Equal(expectedThirst, health.Thirst);
        return receiver;
    }

    private static void PersistentCombinations()
    {
        PotionEffectDefinition burnExit = Definition(PotionEffectPhase.FloorExit,
            PotionEffectOperation.ApplyStatus, PotionElement.Fire, 4,
            PotionStatusEffect.Burning, 2, 1);
        PotionEffectDefinition poisonExit = Definition(PotionEffectPhase.FloorExit,
            PotionEffectOperation.ApplyStatus, PotionElement.Poison, 5,
            PotionStatusEffect.Poisoned, 3, 1);
        PotionEffectDefinition frost = Definition(PotionEffectPhase.FloorTick,
            PotionEffectOperation.AddFrostStacks, PotionElement.Ice, 0);
        PotionEffectDefinition shortParalysis = Definition(PotionEffectPhase.FloorTick,
            PotionEffectOperation.ApplyStatus, PotionElement.Lightning, 0,
            PotionStatusEffect.Paralyzed, 0.15f, moveMultiplier: 0, immobilizeDuration: 0.15f);

        var toxicFlame = CombinationCase(5,
            new[] {
                Definition(PotionEffectPhase.FloorTick, PotionEffectOperation.Damage, PotionElement.Fire, 6),
                Definition(PotionEffectPhase.FloorTick, PotionEffectOperation.Damage, PotionElement.Poison, 4),
                burnExit, poisonExit
            }, 150);
        Equal(1, toxicFlame.GetStatusStackCount(PotionStatusEffect.Burning));
        Equal(1, toxicFlame.GetStatusStackCount(PotionStatusEffect.Poisoned));

        var thermal = CombinationCase(4,
            new[] {
                Definition(PotionEffectPhase.Impact, PotionEffectOperation.Damage, PotionElement.Fire, 25),
                Definition(PotionEffectPhase.FloorTick, PotionEffectOperation.Damage, PotionElement.Fire, 5),
                frost
            }, 155, expectFreezeOnThirdPulse: true);

        var blazing = CombinationCase(4,
            new[] {
                Definition(PotionEffectPhase.FloorTick, PotionEffectOperation.Damage, PotionElement.Fire, 6),
                Definition(PotionEffectPhase.FloorTick, PotionEffectOperation.Damage, PotionElement.Lightning, 12),
                Definition(PotionEffectPhase.FloorTick, PotionEffectOperation.ApplyStatus,
                    PotionElement.Lightning, 0, PotionStatusEffect.Paralyzed, 0.1f,
                    moveMultiplier: 0, immobilizeDuration: 0.1f),
                burnExit
            }, 128, expectFirstPulseStop: true);
        Equal(1, blazing.GetStatusStackCount(PotionStatusEffect.Burning));

        var frostPoison = CombinationCase(5,
            new[] {
                Definition(PotionEffectPhase.FloorTick, PotionEffectOperation.Damage, PotionElement.Poison, 5),
                frost, poisonExit
            }, 175, expectFreezeOnThirdPulse: true);

        var toxicCurrent = CombinationCase(5,
            new[] {
                Definition(PotionEffectPhase.FloorTick, PotionEffectOperation.Damage, PotionElement.Poison, 5),
                shortParalysis, poisonExit
            }, 175, expectFirstPulseStop: true);
        Equal(1, toxicCurrent.GetStatusStackCount(PotionStatusEffect.Poisoned));

        var frozenCurrent = CombinationCase(4,
            new[] {
                Definition(PotionEffectPhase.FloorTick, PotionEffectOperation.Damage, PotionElement.Fire, 4),
                Definition(PotionEffectPhase.FloorTick, PotionEffectOperation.Damage, PotionElement.Lightning, 8),
                frost, shortParalysis
            }, 152, expectFirstPulseStop: true, expectFreezeOnThirdPulse: true);
    }

    private static PotionEffectReceiver CombinationCase(float duration,
        PotionEffectDefinition[] effects, float expectedHealth, bool expectFirstPulseStop = false,
        bool expectFreezeOnThirdPulse = false)
    {
        Time.timeAsDouble = 0;
        var health = new FakeHealth(200);
        var receiver = Receiver(health);
        PotionData profile = Profile(1, effects);
        Set(profile, "_targetType", PotionTargetType.Enemy);
        var source = new PotionEffectSource(71, combination: new PotionCombinationData());
        PotionEffectApplier.ApplyProfilePhase(profile, PotionEffectPhase.Impact, receiver, source);
        var timeline = new PotionAreaTimeline<PotionEffectReceiver>(duration, 1, true,
            _ => true,
            target => target.EnterFloor(profile, source),
            target => target.PulseFloor(source.SourceId),
            (target, residual) => target.ExitFloor(source.SourceId, residual));
        for (int now = 0; now <= duration; now++)
        {
            Time.timeAsDouble = now;
            timeline.Synchronize(new[] { receiver }, now);
            if (now == 0 && expectFirstPulseStop)
                Check(!receiver.CanMove, "combination paralysis missing on first pulse");
            if (now == 2 && expectFreezeOnThirdPulse)
                Check(!receiver.CanMove, "combination frost did not freeze on third pulse");
        }
        Equal(expectedHealth, health.CurrentHealth);
        return receiver;
    }

    private static void StageSevenProfiles()
    {
        PotionEffectDefinition burnExit = Definition(PotionEffectPhase.FloorExit,
            PotionEffectOperation.ApplyStatus, PotionElement.Fire, 4,
            PotionStatusEffect.Burning, 2, 1);
        PotionEffectDefinition poisonExit = Definition(PotionEffectPhase.FloorExit,
            PotionEffectOperation.ApplyStatus, PotionElement.Poison, 5,
            PotionStatusEffect.Poisoned, 3, 1);

        Equal(18, VampiricCase(4, 1, 2,
            Definition(PotionEffectPhase.FloorTick, PotionEffectOperation.Damage,
                PotionElement.Fire, 8), burnExit));
        Equal(18, VampiricCase(5, 1, 3,
            Definition(PotionEffectPhase.FloorTick, PotionEffectOperation.Damage,
                PotionElement.Poison, 5), poisonExit));
        Equal(14, VampiricCase(4, 1, 0,
            Definition(PotionEffectPhase.FloorTick, PotionEffectOperation.Damage,
                PotionElement.Ice, 5),
            Definition(PotionEffectPhase.FloorTick, PotionEffectOperation.AddFrostStacks,
                PotionElement.Ice, 0)));
        Equal(14.8f, VampiricCase(2, 0.5f, 0,
            Definition(PotionEffectPhase.FloorTick, PotionEffectOperation.Damage,
                PotionElement.Lightning, 6),
            Definition(PotionEffectPhase.FloorTick, PotionEffectOperation.ApplyStatus,
                PotionElement.Lightning, 0, PotionStatusEffect.Paralyzed, 0.1f,
                moveMultiplier: 0, immobilizeDuration: 0.1f)));

        foreach (PotionElement element in new[] {
                     PotionElement.Fire, PotionElement.Poison,
                     PotionElement.Ice, PotionElement.Lightning })
        {
            Time.timeAsDouble = 0;
            var health = new FakeHealth();
            var receiver = Receiver(health);
            PotionData profile = Profile(1,
                Definition(PotionEffectPhase.Impact, PotionEffectOperation.Damage, element, 30));
            Set(profile, "_targetType", PotionTargetType.Enemy);
            PotionEffectApplier.ApplyProfilePhase(profile, PotionEffectPhase.Impact,
                receiver, new PotionEffectSource(99, combination: new PotionCombinationData()));
            Equal(70, health.CurrentHealth);
            Equal(0, receiver.GetStatusStackCount(PotionStatusEffect.Burning));
            Equal(0, receiver.GetStatusStackCount(PotionStatusEffect.Poisoned));
        }

        var cappedTarget = new FakeHealth(3);
        var cappedOwner = new FakeHealth(10);
        var cappedReceiver = Receiver(cappedTarget);
        PotionData cappedProfile = Profile(1,
            Definition(PotionEffectPhase.Impact, PotionEffectOperation.Damage,
                PotionElement.Fire, 30));
        Set(cappedProfile, "_targetType", PotionTargetType.Enemy);
        PotionEffectApplier.ApplyProfilePhase(cappedProfile, PotionEffectPhase.Impact,
            cappedReceiver, new PotionEffectSource(100, owner: cappedOwner, lifeStealRatio: 0.2f));
        Equal(10.6f, cappedOwner.CurrentHealth);
    }

    private static float VampiricCase(float duration, float interval, float residualDuration,
        params PotionEffectDefinition[] effects)
    {
        Time.timeAsDouble = 0;
        var targetHealth = new FakeHealth(200);
        var owner = new FakeHealth(10);
        var receiver = Receiver(targetHealth);
        PotionData profile = Profile(interval, effects);
        var source = new PotionEffectSource(81, combination: new PotionCombinationData(),
            owner: owner, lifeStealRatio: 0.2f);
        var timeline = new PotionAreaTimeline<PotionEffectReceiver>(duration, interval, true,
            _ => true,
            target => target.EnterFloor(profile, source),
            target => target.PulseFloor(source.SourceId),
            (target, residual) => target.ExitFloor(source.SourceId, residual));
        for (double now = 0; now <= duration; now += interval)
        {
            Time.timeAsDouble = now;
            timeline.Synchronize(new[] { receiver }, now);
        }
        if (residualDuration > 0)
        {
            Time.timeAsDouble = duration + residualDuration;
            Call(receiver, "Update");
        }
        return owner.CurrentHealth;
    }

    private static PotionData Profile(float interval, params PotionEffectDefinition[] effects)
    {
        var profile = ScriptableObject.CreateInstance<PotionData>();
        Set(profile, "_tickInterval", interval);
        Set(profile, "_applyOnEnter", true);
        Set(profile, "_effects", new List<PotionEffectDefinition>(effects));
        return profile;
    }

    private static PotionEffectDefinition Definition(PotionEffectPhase phase,
        PotionEffectOperation operation, PotionElement element, float amount,
        PotionStatusEffect status = default, float duration = 0, float tickInterval = 0,
        bool applyImmediately = false, float moveMultiplier = 1, float immobilizeDuration = 0)
    {
        var effect = new PotionEffectDefinition();
        Set(effect, "_phase", phase);
        Set(effect, "_operation", operation);
        Set(effect, "_element", element);
        Set(effect, "_amount", amount);
        Set(effect, "_status", status);
        Set(effect, "_duration", duration);
        Set(effect, "_tickInterval", tickInterval);
        Set(effect, "_applyImmediately", applyImmediately);
        Set(effect, "_moveSpeedMultiplier", moveMultiplier);
        Set(effect, "_immobilizeDuration", immobilizeDuration);
        return effect;
    }

    private static void AddEffect(PotionData profile, PotionEffectDefinition effect) =>
        ((List<PotionEffectDefinition>)Get(profile, "_effects")).Add(effect);

    private static object Get(object target, string name) => target.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

    private static void Set(object target, string name, object value) => target.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

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
