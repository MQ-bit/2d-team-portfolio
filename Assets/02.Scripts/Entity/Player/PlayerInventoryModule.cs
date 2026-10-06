using System;
using Entity.Inventory;
using UnityEngine;

[RequireComponent(typeof(EntityInventory))]
public class PlayerInventoryModule : MonoBehaviour, IItemReceiver
{
    [SerializeField] private EntityInventory _inventory;

    private int _selectedSlotIndex = -1;
    private bool _isSubscribed;

    public event Action<int> OnSelectedSlotChanged;
    public event Action<int> OnSlotChanged;

    public int SelectedSlotIndex => _selectedSlotIndex;
    public int SlotCount => _inventory.MaxSlotSize;

    public PotionItem SelectedPotionItem => _selectedSlotIndex >= 0 ? _inventory.GetPotionItem(_selectedSlotIndex) : default;
    public PotionData SelectedPotion => SelectedPotionItem.PotionData;

    private void Awake()
    {
        if (_inventory == null)
        {
            _inventory = GetComponent<EntityInventory>();
        }
    }

    private void OnEnable()
    {
        LinkEvents();
    }

    private void Start()
    {
        SelectSlot(0);
    }

    private void OnDisable()
    {
        UnlinkEvents();
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= _inventory.MaxSlotSize)
        {
            return;
        }

        if (_selectedSlotIndex == index)
        {
            return;
        }

        _selectedSlotIndex = index;
        OnSelectedSlotChanged?.Invoke(index);
    }

    public void SelectRelativeSlot(int direction)
    {
        int slotCount = SlotCount;
        if (slotCount <= 0 || direction == 0) return;

        if (_selectedSlotIndex < 0)
        {
            SelectSlot(0);
            return;
        }

        int step = direction > 0 ? 1 : -1;
        int nextIndex = (_selectedSlotIndex + step + slotCount) % slotCount;
        SelectSlot(nextIndex);
    }

    public bool TryTakeItem(PotionItem item, int quantity)
    {
        return _inventory.TryAddItem(item, quantity);
    }

    public bool TryConsumeSelectedPotion(out PotionData consumedPotion)
    {
        consumedPotion = null;

        if (!TryConsumeSelectedItem(out PotionItem consumedItem))
        {
            return false;
        }

        consumedPotion = consumedItem.PotionData;
        return true;
    }

    public bool TryConsumeSelectedItem(out PotionItem consumedItem)
    {
        return _inventory.TryConsumeItem(_selectedSlotIndex, 1, out consumedItem);
    }

    public PotionItem GetItemAt(int index)
    {
        return _inventory.GetPotionItem(index);
    }

    public int GetItemCountAt(int index)
    {
        return _inventory.GetItemCount(index);
    }

    private void LinkEvents()
    {
        if (_inventory == null || _isSubscribed)
        {
            return;
        }

        _inventory.OnSlotChanged += HandleSlotChanged;
        _isSubscribed = true;
    }

    private void UnlinkEvents()
    {
        if (_inventory == null || !_isSubscribed)
        {
            return;
        }

        _inventory.OnSlotChanged -= HandleSlotChanged;
        _isSubscribed = false;
    }

    private void HandleSlotChanged(int slotIndex)
    {
        OnSlotChanged?.Invoke(slotIndex);
    }
}
