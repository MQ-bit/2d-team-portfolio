using System;
using System.Collections.Generic;

// Run the production spread/timeline code without Unity; only physics is stubbed.
internal static class Program
{
    private static void Main()
    {
        var effect = Create(out var spread);
        int impacts = 0;
        bool complete = effect.Synchronize(null, true, _ =>
        {
            impacts++;
            effect.Clear();
        });
        Check(!complete && !effect.IsActive && impacts == 1,
            "Clearing during impact must stop the batch without invalidating enumeration.");

        effect = Create(out spread);
        impacts = 0;
        effect.Synchronize(null, true, _ => impacts++);
        effect.Synchronize(null, true, _ => impacts++);
        Check(impacts == 2, "Each reached target must receive exactly one queued impact.");
        spread.IsComplete = true;
        Check(effect.Synchronize(null, true, _ => impacts++), "Normal spread must complete.");

        effect = Create(out spread);
        var target = spread.Hits[0].Target;
        var original = new PotionAreaTimeline<IPotionEffectTarget>(100, 1, false,
            _ => true, _ => { }, _ => { }, (_, _) => effect.Clear());
        original.Synchronize(new[] { target }, 0);
        effect.KeepOriginalTimeline(original);
        impacts = 0;
        Check(!effect.Synchronize(null, true, _ => impacts++) && impacts == 0 && !effect.IsActive,
            "Clearing from original floor exit must cancel combination impacts.");
        Console.WriteLine("PASS: impact cancellation, one-shot impacts, completion, original exit cancellation.");
    }

    private static PotionAreaSpreadEffect Create(out PotionCombinationSpread spread)
    {
        spread = new PotionCombinationSpread();
        spread.Hits.Add(new UnityEngine.Collider2D { Target = new Target() });
        spread.Hits.Add(new UnityEngine.Collider2D { Target = new Target() });
        var effect = new PotionAreaSpreadEffect();
        effect.Prepare(spread, true);
        effect.QueueCombinationImpact(_ => true);
        return effect;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private sealed class Target : IPotionEffectTarget { }
}

internal interface IPotionEffectTarget { }
internal static class PotionEffectApplier
{
    internal static IPotionEffectTarget FindTarget(UnityEngine.Collider2D hit) => hit.Target;
}
internal sealed class PotionCombinationSpread
{
    internal readonly List<UnityEngine.Collider2D> Hits = new();
    internal readonly ShapeStub Shape = new();
    internal bool IsComplete;
    internal bool HasReached(UnityEngine.Collider2D hit) => true;
}
internal sealed class ShapeStub
{
    internal UnityEngine.Vector2 FirstCenter => default;
    internal UnityEngine.Vector2 SecondCenter => default;
    internal bool ContainsLobe(UnityEngine.Vector2 point, bool firstLobe) => true;
}
namespace UnityEngine
{
    internal struct Vector2 { }
    internal sealed class Collider2D
    {
        internal IPotionEffectTarget Target;
        internal Vector2 ClosestPoint(Vector2 point) => point;
    }
    internal static class Time
    {
        internal static double timeAsDouble => 0;
    }
}
