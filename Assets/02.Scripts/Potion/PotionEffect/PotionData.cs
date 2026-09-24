using System;

public enum PotionElement
{
    None,
    Fire,
    Poison,
    Ice,
    Heal
}

[Serializable]
public sealed class PotionData
{
    public string Id { get; }
    public string Name { get; }
    public PotionElement Element { get; }
    public float Damage { get; }
    public float TickInterval { get; }
    public float AreaDuration { get; }
    public float EffectDuration { get; }
    public float Radius { get; }

    public bool HasAreaEffect => AreaDuration > 0f && Radius > 0f;
    public bool HasPeriodicDamage => Damage > 0f && TickInterval > 0f;

    public PotionData(
        string id,
        string name,
        PotionElement element,
        float damage,
        float tickInterval,
        float areaDuration,
        float effectDuration,
        float radius)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Potion id cannot be empty.", nameof(id));
        }

        ValidateNonNegative(damage, nameof(damage));
        ValidateNonNegative(tickInterval, nameof(tickInterval));
        ValidateNonNegative(areaDuration, nameof(areaDuration));
        ValidateNonNegative(effectDuration, nameof(effectDuration));
        ValidateNonNegative(radius, nameof(radius));

        Id = id.Trim();
        Name = string.IsNullOrWhiteSpace(name) ? Id : name.Trim();
        Element = element;
        Damage = damage;
        TickInterval = tickInterval;
        AreaDuration = areaDuration;
        EffectDuration = effectDuration;
        Radius = radius;
    }

    private static void ValidateNonNegative(float value, string fieldName)
    {
        if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
        {
            throw new ArgumentOutOfRangeException(fieldName, value, "Value must be a finite number greater than or equal to zero.");
        }
    }
}
