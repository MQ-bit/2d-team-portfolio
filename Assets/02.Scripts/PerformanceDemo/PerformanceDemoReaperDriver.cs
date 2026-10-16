using System;
using UnityEngine;

public sealed class PerformanceDemoReaperDriver : MonoBehaviour
{
    private ReaperController _reaper;
    private ReaperController.ActionState _previous;
    private PlayerInventoryModule _inventory;
    private PerformanceDemoKind _kind;
    private int _initialPotions;
    public int SkillStarts { get; private set; }
    public int SkillCompletions { get; private set; }
    public void SetSimulationEnabled(bool value) { if (_reaper != null) _reaper.enabled = value; }
    public bool TheftSucceeded => CountPotions() < _initialPotions;
    public void Setup(PerformanceDemoScenario scenario, PlayerStatus player, PotionRunCatalog catalog)
    {
        _kind = scenario.Kind;
        SkillStarts = SkillCompletions = 0;
        _inventory = player.GetComponent<PlayerInventoryModule>();
        if (_kind == PerformanceDemoKind.Steal && !_inventory.TryTakeItem(catalog.GetPotionItemByData(scenario.Potion), 1))
            throw new InvalidOperationException("Could not prepare theft inventory.");
        _initialPotions = CountPotions();
        GameObject enemy = Instantiate(scenario.ReaperPrefab, new Vector3(-4, 0, 0), Quaternion.identity);
        _reaper = enemy.GetComponent<ReaperController>();
        if (_reaper == null || !_reaper.enabled) throw new InvalidOperationException("Reaper initialization failed.");
        _previous = _reaper.Action;
    }
    public string Observe()
    {
        if (_reaper == null) throw new InvalidOperationException("Reaper disappeared.");
        ReaperController.ActionState current = _reaper.Action;
        if (current == _previous) return null;
        var expected = _kind == PerformanceDemoKind.Dash ? ReaperController.ActionState.Charge :
            _kind == PerformanceDemoKind.Steal ? ReaperController.ActionState.Steal : ReaperController.ActionState.Cast;
        if (current == expected) SkillStarts++;
        if (current == ReaperController.ActionState.Recovery &&
            (_reaper.RecoveryFrom == expected || (_kind == PerformanceDemoKind.Dash && _reaper.RecoveryFrom == ReaperController.ActionState.Dash)))
        {
            SkillCompletions++;
        }

        string transition = _previous + " -> " + current;
        _previous = current;
        return transition;
    }
    private int CountPotions()
    {
        int count = 0;
        if (_inventory != null)
            for (int i = 0; i < _inventory.SlotCount; i++) count += _inventory.GetItemCountAt(i);
        return count;
    }
    public void Clear() { _reaper = null; _inventory = null; }
}
