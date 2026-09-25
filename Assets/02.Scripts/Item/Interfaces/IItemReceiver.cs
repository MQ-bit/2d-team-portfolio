using UnityEngine;

public interface IItemReceiver
{
    bool TryTakeItem(PotionData data, int quantity);
}
