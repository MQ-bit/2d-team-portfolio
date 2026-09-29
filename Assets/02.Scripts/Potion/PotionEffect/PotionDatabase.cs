using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PotionDatabase", menuName = "Potion/Potion Database")]
public sealed class PotionDatabase : ScriptableObject
{
    [SerializeField] private List<PotionData> _potions = new();

    private readonly Dictionary<string, PotionData> _potionsById = new(StringComparer.Ordinal);

    public IReadOnlyList<PotionData> Potions => _potions;
    public int Count => _potions.Count;

    private void OnEnable()
    {
        RebuildLookup();
    }

    private void OnValidate()
    {
        RebuildLookup();
    }

    public PotionData GetById(string id)
    {
        return TryGetById(id, out PotionData potion) ? potion : null;
    }

    public bool TryGetById(string id, out PotionData potion)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            potion = null;
            return false;
        }

        return _potionsById.TryGetValue(id.Trim(), out potion);
    }

    private void RebuildLookup()
    {
        _potionsById.Clear();

        for (int i = 0; i < _potions.Count; i++)
        {
            PotionData potion = _potions[i];
            if (potion == null || string.IsNullOrWhiteSpace(potion.Id))
            {
                continue;
            }

            if (!_potionsById.TryAdd(potion.Id, potion))
            {
                Debug.LogError($"Duplicate potion id '{potion.Id}' in {name}.", this);
            }
        }
    }
}
