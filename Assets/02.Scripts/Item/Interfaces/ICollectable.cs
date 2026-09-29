using UnityEngine;

public interface IItemCollectable
{
    bool TryCollect(IItemReceiver receiver, Transform collectTarget, float collectRadius);
}
