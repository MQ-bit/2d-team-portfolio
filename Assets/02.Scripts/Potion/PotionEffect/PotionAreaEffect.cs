using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class PotionAreaEffect : MonoBehaviour
{
    [SerializeField] private bool _enableDebugLogs = true;

    private readonly Dictionary<IPotionEffectTarget, int> _overlapCounts = new();
    private readonly Dictionary<IPotionEffectTarget, Coroutine> _tickRoutines = new();
    private PotionData _data;

    public void Initialize(PotionData data)
    {
        if (data == null)
        {
            Debug.LogError("PotionAreaEffect requires PotionData.", this);
            return;
        }

        _data = data;
        Log($"Initialized: potion={data.Name}, amount={data.AreaEffectAmount}, tick={data.TickInterval}");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!TryResolveTarget(other, out IPotionEffectTarget target))
        {
            return;
        }

        _overlapCounts.TryGetValue(target, out int overlapCount);
        _overlapCounts[target] = overlapCount + 1;

        if (overlapCount == 0)
        {
            StartEffect(target);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (_data == null ||
            !PotionEffectResolver.TryFindTarget(other, out IPotionEffectTarget target) ||
            _overlapCounts.ContainsKey(target))
        {
            return;
        }

        _overlapCounts[target] = 1;
        StartEffect(target);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!PotionEffectResolver.TryFindTarget(other, out IPotionEffectTarget target) ||
            !_overlapCounts.TryGetValue(target, out int overlapCount))
        {
            return;
        }

        if (overlapCount > 1)
        {
            _overlapCounts[target] = overlapCount - 1;
            return;
        }

        StopEffect(target);
    }

    private bool TryResolveTarget(Collider2D other, out IPotionEffectTarget target)
    {
        bool targetFound = PotionEffectResolver.TryFindTarget(other, out target);
        if (_data != null && targetFound)
        {
            return true;
        }

        LogWarning(
            $"Target rejected: collider={other.name}, potion={GetPotionName()}, " +
            $"dataMissing={_data == null}, targetMissing={!targetFound}");
        return false;
    }

    private void StartEffect(IPotionEffectTarget target)
    {
        LogApplication("Enter", target);
        PotionEffectResolver.ApplyAreaEffect(_data, target);

        if (_data.HasPeriodicAreaEffect)
        {
            _tickRoutines[target] = StartCoroutine(ApplyPeriodicEffect(target));
        }
    }

    private void StopEffect(IPotionEffectTarget target)
    {
        _overlapCounts.Remove(target);
        if (_tickRoutines.Remove(target, out Coroutine routine) && routine != null)
        {
            StopCoroutine(routine);
        }
    }

    private IEnumerator ApplyPeriodicEffect(IPotionEffectTarget target)
    {
        WaitForSeconds wait = new(Mathf.Max(0.01f, _data.TickInterval));

        while (_overlapCounts.ContainsKey(target) && IsTargetAlive(target))
        {
            yield return wait;

            if (_overlapCounts.ContainsKey(target) && IsTargetAlive(target))
            {
                LogApplication("Tick", target);
                PotionEffectResolver.ApplyAreaEffect(_data, target);
            }
        }

        _overlapCounts.Remove(target);
        _tickRoutines.Remove(target);
    }

    private static bool IsTargetAlive(IPotionEffectTarget target)
    {
        return target is not Object unityObject || unityObject != null;
    }

    private void LogApplication(string phase, IPotionEffectTarget target)
    {
        string targetName = target is Component component ? component.name : target.ToString();
        Log($"{phase}: potion={GetPotionName()}, target={targetName}, amount={_data.AreaEffectAmount}");
    }

    private string GetPotionName() => _data == null ? "None" : _data.Name;

    private void Log(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.Log($"[PotionAreaEffect] {message}", this);
        }
    }

    private void LogWarning(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.LogWarning($"[PotionAreaEffect] {message}", this);
        }
    }

    private void OnDisable()
    {
        foreach (Coroutine routine in _tickRoutines.Values)
        {
            if (routine != null)
            {
                StopCoroutine(routine);
            }
        }

        _overlapCounts.Clear();
        _tickRoutines.Clear();
    }
}
