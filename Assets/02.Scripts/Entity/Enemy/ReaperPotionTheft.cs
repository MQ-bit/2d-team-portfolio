using UnityEngine;

internal static class ReaperPotionTheft
{
    private static bool IsCandidate(PlayerInventoryModule inventory, int slot)
    {
        if (inventory.GetItemCountAt(slot) <= 0) return false;
        return !inventory.GetItemAt(slot).IsEmpty;
    }

    public static bool HasCandidate(PlayerInventoryModule inventory)
    {
        if (inventory == null) return false;
        for (int i = 0; i < inventory.SlotCount; i++)
            if (IsCandidate(inventory, i)) return true;
        return false;
    }

    public static bool TrySteal(PlayerInventoryModule inventory, out PotionItem stolen)
    {
        stolen = default;
        if (inventory == null) return false;
        int selected = -1, count = 0;
        for (int i = 0; i < inventory.SlotCount; i++)
            if (IsCandidate(inventory, i) && UnityEngine.Random.Range(0, ++count) == 0) selected = i;
        return selected >= 0 && inventory.TryConsumeItemAt(selected, out stolen);
    }

    public static bool IsInsideWarning(Vector2 center, Vector2 playerPosition, float radius)
    {
        Vector2 offset = playerPosition - center;
        offset.y *= 2f;
        return offset.sqrMagnitude <= radius * radius;
    }
}
