using UnityEngine;
using Entity.Inventory;
using System;

[RequireComponent(typeof(EntityInventory))]
public class PlayerInventoryModule : MonoBehaviour
{
    [SerializeField] private EntityInventory _inventory;
    private int _selectedSlotIndex = -1;
    private PotionData _selctedPotionData;

    public event Action<int> OnSelectedSlotChanged; // TODO : UI 연결 
    public int SelectedSlotIndex => _selectedSlotIndex;
    public PotionData SelectedPotion => _selctedPotionData;

    private void Awake()
    {
        _inventory = GetComponent<EntityInventory>();
    }

    private void Start()
    {
        SelectSlot(0);
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= _inventory.MaxSlotSize) return;
        if (_selectedSlotIndex == index) return;

        _selectedSlotIndex = index;
        _selctedPotionData = _inventory.GetPotionData(index);
        OnSelectedSlotChanged?.Invoke(index);
    }
}
