using System;
using UnityEngine;

public sealed class PotionCodexPresenter : MonoBehaviour
{
    [SerializeField] private PotionCodexView _view;

    private PlayerInventoryModule _inventory;
    private bool _isSubscribed;

    private void Awake()
    {
        if (_view == null)
        {
            _view = GetComponentInChildren<PotionCodexView>();
        }
    }

    public void Initialize(PlayerInventoryModule inventory)
    {
        if (inventory == null)
        {
            throw new ArgumentNullException(nameof(inventory));
        }

        if (_view == null)
        {
            throw new InvalidOperationException($"{name}: PotionCodexView가 연결되지 않았습니다.");
        }

        if (_view.SlotCount != inventory.SlotCount)
        {
            throw new InvalidOperationException($"UI 슬롯 수({_view.SlotCount})와 인벤토리 슬롯 수({inventory.SlotCount})가 다릅니다.");
        }

        UnlinkEvents();

        _inventory = inventory;

        LinkEvents();
        RefreshAllSlots();

        if (_inventory.SelectedSlotIndex >= 0)
        {
            _view.SetSelectedSlot(_inventory.SelectedSlotIndex);
        }
    }

    private void LinkEvents()
    {
        if (_inventory == null || _isSubscribed)
        {
            return;
        }

        _inventory.OnSlotChanged += HandleSlotChanged;
        _inventory.OnSelectedSlotChanged += HandleSelectedSlotChanged;

        _isSubscribed = true;
    }

    private void UnlinkEvents()
    {
        if (_inventory == null || !_isSubscribed)
        {
            return;
        }

        _inventory.OnSlotChanged -= HandleSlotChanged;
        _inventory.OnSelectedSlotChanged -= HandleSelectedSlotChanged;

        _isSubscribed = false;
    }

    private void RefreshAllSlots()
    {
        for (int i = 0; i < _view.SlotCount; i++)
        {
            _view.SetSlot(i, _inventory.GetItemAt(i));
        }
    }

    private void HandleSlotChanged(int slotIndex)
    {
        _view.SetSlot(slotIndex, _inventory.GetItemAt(slotIndex));
    }

    private void HandleSelectedSlotChanged(int slotIndex)
    {
        _view.SetSelectedSlot(slotIndex);
    }

    private void OnDestroy()
    {
        UnlinkEvents();
        _inventory = null;
    }
}