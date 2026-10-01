using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PotionCombinationDatabase", menuName = "Potion/Potion Combination Database")]
public sealed class PotionCombinationDatabase : ScriptableObject
{
    [SerializeField] private List<PotionCombinationData> _combinations = new();
    public IReadOnlyList<PotionCombinationData> Combinations => _combinations;

    public bool TryGetCombination(PotionElement a, PotionElement b, out PotionCombinationData result)
    {
        if (a == PotionElement.None ||
            b == PotionElement.None)
        {
            result = null;
            return false;
        }

        foreach (PotionCombinationData combination in _combinations)
        {
            if (combination == null)
                continue;

            if (!combination.IsMatch(a, b))
                continue;

            result = combination;
            return true;
        }

        result = null;
        return false;
    }

    public PotionCombinationData FindCombination(PotionElement a, PotionElement b)
    {
        return TryGetCombination(a, b, out PotionCombinationData result)
            ? result
            : null;
    }

    private void OnValidate()
    {
        if (_combinations == null)
            return;

        for (int i = 0; i < _combinations.Count; i++)
        {
            PotionCombinationData current = _combinations[i];

            if (current == null)
                continue;

            for (int j = i + 1; j < _combinations.Count; j++)
            {
                PotionCombinationData other = _combinations[j];

                if (other == null)
                    continue;

                if (!current.IsMatch(other.ElementA, other.ElementB))
                    continue;

                Debug.LogWarning($"Duplicate potion combination: " + $"{current.ElementA} + {current.ElementB}", this);
            }
        }
    }
}