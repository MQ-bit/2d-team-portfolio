using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PotionAreaEffect : MonoBehaviour
{
    private readonly Dictionary<IPotionEffectTarget, int> _overlapCounts = new();
    private readonly Dictionary<IPotionEffectTarget, Coroutine> _tickRoutines = new();
    private PotionData _data;

    public void Initialize(PotionData data)
    {
        _data = data;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        IPotionEffectTarget target = PotionEffectResolver.FindTarget(other);
        if (_data == null || target == null)
        {
            return;
        }

        _overlapCounts.TryGetValue(target, out int overlapCount);
        _overlapCounts[target] = overlapCount + 1;
        if (overlapCount > 0)
        {
            return;
        }

        PotionEffectResolver.ApplyAreaEffect(_data, target, GetInstanceID());
        if (_data.TickInterval > 0f)
        {
            _tickRoutines[target] = StartCoroutine(ApplyPeriodicEffect(target));
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        IPotionEffectTarget target = PotionEffectResolver.FindTarget(other);

        if (target == null || !_overlapCounts.TryGetValue(target, out int overlapCount))
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
        WaitForSeconds wait = new(Mathf.Max(0.01f, _data.TickInterval));
        while (_overlapCounts.ContainsKey(target))
        {
            yield return wait;

            if (_overlapCounts.ContainsKey(target))
            {
                PotionEffectResolver.ApplyAreaEffect(_data, target, GetInstanceID());
            }
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        _overlapCounts.Clear();
        _tickRoutines.Clear();
    }
}
