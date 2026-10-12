using System.Collections.Generic;
using UnityEngine;

// One bottle shares raw damage and healing budgets across all consumed surfaces.
public sealed class PotionVaporizationBatch
{
    private readonly struct Application
    {
        internal readonly PotionEffectDefinition Effect;
        internal readonly IPotionEffectData Profile;
        internal readonly PotionEffectSource Source;

        internal Application(PotionEffectDefinition effect, IPotionEffectData profile,
            PotionEffectSource source)
        {
            Effect = effect;
            Profile = profile;
            Source = source;
        }
    }

    private readonly Dictionary<IPotionEffectTarget, List<Application>> _applications = new();
    private readonly Dictionary<IDamageable, float> _healing = new();
    private readonly float _damageCap;
    private readonly float _healingCap;
    private readonly float _freezeDuration;
    private readonly float _paralysisDuration;

    internal PotionVaporizationBatch(PotionVaporizationSettings settings)
    {
        _damageCap = settings != null ? settings.DamageCapPerTarget : 240f;
        _healingCap = settings != null ? settings.HealingCapPerTarget : 120f;
        _freezeDuration = settings != null ? settings.FreezeDuration : 2.5f;
        _paralysisDuration = settings != null ? settings.ParalysisDuration : 0.6f;
    }

    internal void Queue(IPotionEffectTarget target, IPotionEffectData profile,
        PotionEffectSource source, IReadOnlyList<PotionEffectDefinition> effects)
    {
        if (!_applications.TryGetValue(target, out List<Application> applications))
            _applications.Add(target, applications = new List<Application>());
        foreach (PotionEffectDefinition effect in effects)
            applications.Add(new Application(effect, profile, source));

        // Read periodic elements, including Frost stacks without Frost damage (FrostPoison).
        // Formation impact and residual statuses do not contribute to this reaction.
        bool frost = false, lightning = false;
        foreach (PotionEffectDefinition effect in profile.GetEffects(PotionEffectPhase.FloorTick))
        {
            frost |= effect.Element == PotionElement.Ice;
            lightning |= effect.Element == PotionElement.Lightning;
        }
        if (frost)
        {
            QueueControl(applications, PotionStatusEffectTracker.Frozen,
            PotionElement.Ice, _freezeDuration, profile, source);
        }

        if (lightning)
        {
            QueueControl(applications, PotionStatusEffectTracker.Paralyzed,
            PotionElement.Lightning, _paralysisDuration, profile, source);
        }
    }

    private static void QueueControl(List<Application> applications, PotionStatusEffectTracker status,
        PotionElement element, float duration, IPotionEffectData profile, PotionEffectSource source)
    {
        foreach (Application application in applications)
        {
            if (application.Effect.Operation == PotionEffectOperation.ApplyStatus
                && application.Effect.Status == status)
            {
                return;
            }
        }

        applications.Add(new Application(
            PotionEffectDefinition.CreateInstantStatus(status, element, duration), profile, source));
    }

    internal void AddHealing(IDamageable target, float amount)
    {
        if (!PotionStatApplier.IsAlive(target)) return;
        amount = PotionDataValidation.NonNegative(amount);
        _healing.TryGetValue(target, out float total);
        _healing[target] = Mathf.Min(_healingCap, total + amount);
    }

    internal void Apply()
    {
        foreach (var pair in _applications)
        {
            // Calculate the scale before any HP callbacks so every element keeps its share.
            float totalDamage = 0f;
            foreach (Application application in pair.Value)
            {
                if (application.Effect.Operation == PotionEffectOperation.Damage)
                    totalDamage += application.Effect.Amount;
            }

            float damageScale = totalDamage > _damageCap ? _damageCap / totalDamage : 1f;
            foreach (Application application in pair.Value)
            {
                if (!PotionEffectApplier.CanAffect(application.Profile.TargetType, pair.Key)) continue;
                PotionEffectDefinition effect = application.Effect;
                if (effect.Operation == PotionEffectOperation.Damage)
                {
                    effect = PotionEffectDefinition.CreateInstant(effect.Operation, effect.Element,
                        effect.Amount * damageScale);
                }

                pair.Key.ApplyEffect(effect, application.Profile, application.Source);
            }
        }

        // Direct healing and life steal share the recipient's cap; damage never offsets healing.
        foreach (var pair in _healing) PotionStatApplier.Heal(pair.Key, pair.Value);
        _applications.Clear();
        _healing.Clear();
    }
}
