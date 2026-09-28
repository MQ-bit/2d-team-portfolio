using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PotionCombinationDatabase", menuName = "Potion/Potion Combination Database")]
public sealed class PotionCombinationDatabase : ScriptableObject
{
    [SerializeField]
    private List<PotionCombinationData> _combinations = new();

    public IReadOnlyList<PotionCombinationData> Combinations => _combinations;

    public bool TryGetCombination(PotionElement a, PotionElement b, out PotionCombinationData result)
    {
        if (_combinations != null)
        {
            foreach (PotionCombinationData combination in _combinations)
            {
                if (combination != null && combination.IsMatch(a, b))
                {
                    result = combination;
                    return true;
                }
            }
        }

        result = null;
        return false;
    }

    public PotionData Resolve(PotionElement a, PotionElement b)
    {
        return TryGetCombination(a, b, out PotionCombinationData combination)
            ? combination.ResultPotion
            : null;
    }
}
