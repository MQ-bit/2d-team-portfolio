using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PotionAreaEffect : MonoBehaviour
{
    [SerializeField] private bool _enableDebugLogs = true;

    private readonly Dictionary<IPotionEffectTarget, int> _overlapCounts = new();
    private readonly Dictionary<IPotionEffectTarget, Coroutine> _tickRoutines = new();
    private PotionData _data;

    public void Initialize(PotionData data)
    {
        _data = data;

        if (_enableDebugLogs)
        {
            Debug.Log(
                $"[PotionAreaEffect] Initialized: potion={GetPotionName()}, " +
                $"amount={_data?.AreaEffectAmount ?? 0f}, tick={_data?.TickInterval ?? 0f}",
                this);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        IPotionEffectTarget target = PotionEffectResolver.FindTarget(other);
        if (_data == null || target == null)
        {
            if (_enableDebugLogs)
            {
                Debug.LogWarning(
                    $"[PotionAreaEffect] Target rejected: collider={other.name}, " +
                    $"potion={GetPotionName()}, dataMissing={_data == null}, targetMissing={target == null}",
                    this);
            }

            return;
        }

        _overlapCounts.TryGetValue(target, out int overlapCount);
        _overlapCounts[target] = overlapCount + 1;
        if (overlapCount > 0)
        {
            return;
        }

        LogEffectApplication("Enter", target);
        PotionEffectResolver.ApplyAreaEffect(_data, target);
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
                LogEffectApplication("Tick", target);
                PotionEffectResolver.ApplyAreaEffect(_data, target);
            }
        }
    }

    private void LogEffectApplication(string phase, IPotionEffectTarget target)
    {
        if (!_enableDebugLogs)
        {
            return;
        }

        string targetName = target is Component component ? component.name : target.ToString();
        Debug.Log(
            $"[PotionAreaEffect] {phase}: potion={GetPotionName()}, " +
            $"target={targetName}, amount={_data.AreaEffectAmount}",
            this);
    }

    private string GetPotionName() => _data == null ? "None" : _data.Name;

    private void OnDisable()
    {
        _overlapCounts.Clear();
        _tickRoutines.Clear();
    }
}
