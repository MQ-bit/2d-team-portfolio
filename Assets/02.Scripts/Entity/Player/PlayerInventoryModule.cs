using UnityEngine;
using Entity.Inventory;
using System;

[RequireComponent(typeof(EntityInventory))]
public class PlayerInventoryModule : MonoBehaviour
{
    [SerializeField] private EntityInventory _inventory;
    private int _selectedSlotIndex = 0;
    //private PotionData _selctedPotionData;

    public event Action<int> OnSelectedSlotChanged; // TODO : UI 연결 
    public int SelectedSlotIndex => _selectedSlotIndex;
    //public PotionData SelectedPotion => _selctedPotionData;

    private void Awake()
    {
        _inventory = GetComponent<EntityInventory>();
        _selectedSlotIndex = 0;
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= _inventory.MaxSlotSize) return;
        if (_selectedSlotIndex == index) return;

        _selectedSlotIndex = index;
        OnSelectedSlotChanged?.Invoke(index);
    }

    // private void RefreshSelectedPotion()
    // {
    //     PotionData previousPotion = SelectedPotion;
    //     SelectedPotion = null;

    //     if (_inventory.TryGetSlot(SelectedSlotIndex, out PotionData data))
    //     {
    //         SelectedPotion = data;
    //     }

    //     if (previousPotion != SelectedPotion)
    //     {
    //         SelectedPotionChanged?.Invoke(SelectedPotion);
    //     }
    // }
}
