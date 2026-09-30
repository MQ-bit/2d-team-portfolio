using UnityEngine;

public interface IItemReceiver
{
    bool TryTakeItem(PotionItem item, int quantity);
}
