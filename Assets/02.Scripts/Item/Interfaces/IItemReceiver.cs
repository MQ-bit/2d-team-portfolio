using UnityEngine;

public interface IItemReceiver
{
    bool TryReceive(PotionData data, int quantity);
}
