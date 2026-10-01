using System;
using System.Collections.Generic;

// Collider 대신 대상 단위로 시간/진입/이탈을 관리한다. 물리 조회는 MonoBehaviour의 책임이다.
public sealed class PotionAreaTimeline<T>
{
    private readonly Dictionary<T, double> _nextTicks = new();
    private readonly Func<T, bool> _isValid;
    private readonly Action<T> _enter;
    private readonly Action<T> _tick;
    private readonly Action<T, bool> _exit;
    private readonly double _expiresAt;
    private readonly double _interval;
    private readonly bool _applyOnEnter;
    private bool _ended;

    public PotionAreaTimeline(double expiresAt, double interval, bool applyOnEnter,
        Func<T, bool> isValid, Action<T> enter, Action<T> tick, Action<T, bool> exit)
    {
        _expiresAt = expiresAt;
        _interval = interval > 0 ? Math.Max(0.01, interval) : double.PositiveInfinity;
        _applyOnEnter = applyOnEnter;
        _isValid = isValid;
        _enter = enter;
        _tick = tick;
        _exit = exit;
    }

    public void Synchronize(IEnumerable<T> overlaps, double now)
    {
        if (_ended) return;
        if (now >= _expiresAt) { End(true); return; }
        var targets = new HashSet<T>();
        foreach (T target in overlaps) if (_isValid(target)) targets.Add(target);

        foreach (T target in new List<T>(_nextTicks.Keys))
        {
            if (targets.Contains(target) && _isValid(target)) continue;
            _nextTicks.Remove(target);
            _exit(target, true);
            if (_ended) return;
        }
        foreach (T target in targets)
        {
            if (_ended) return;
            if (!_nextTicks.ContainsKey(target))
            {
                _nextTicks[target] = now + _interval;
                _enter(target);
                if (_ended) return;
                if (_applyOnEnter && _isValid(target)) _tick(target);
            }
            else if (now >= _nextTicks[target])
            {
                // 物理 조회 사이의 체류를 추정해 과거 피해를 몰아 지급하지 않는다.
                _nextTicks[target] = now + _interval;
                if (_isValid(target)) _tick(target);
            }
        }
    }

    public void End(bool applyResidual)
    {
        if (_ended) return;
        _ended = true;
        var targets = new List<T>(_nextTicks.Keys);
        _nextTicks.Clear();
        foreach (T target in targets) _exit(target, applyResidual);
    }
}
