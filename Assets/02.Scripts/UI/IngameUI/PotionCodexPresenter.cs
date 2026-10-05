using System;
using UnityEngine;

public sealed class PotionCodexPresenter : MonoBehaviour
{
    [Header("Slot Views")]
    [SerializeField] private PotionCodexView[] _slotViews;

    [Header("Selection Box")]
    [SerializeField] private RectTransform _selectionBox;

    private PlayerInventoryModule _inventory;
    private bool _isSubscribed;

    private void Awake()
    {
        if (_slotViews == null || _slotViews.Length == 0)
        {
            _slotViews = GetComponentsInChildren<PotionCodexView>(true);
        }
    }

    public void Initialize(PlayerInventoryModule inventory)
    {
        if (inventory == null)
        {   
            throw new ArgumentNullException(nameof(inventory));
        }

        CheckReferences(inventory);
        UnlinkEvents();

        _inventory = inventory;

        LinkEvents();
        RefreshAllSlots();

        if(_inventory.SelectedSlotIndex  >= 0)
        {
            MoveSelectionBox(_inventory.SelectedSlotIndex);
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

    private void CheckReferences(PlayerInventoryModule inventory)
    {
        if(_slotViews == null || _slotViews.Length == 0)
        {
            throw new InvalidOperationException($"{name} : PotionCodexView references are not assigned.");
        }
        if(_slotViews.Length != inventory.SlotCount)
        {
            throw new InvalidOperationException($"{name} : PotionCodexView count ({_slotViews.Length}) does not match inventory slot count ({inventory.SlotCount}).");
        }

        for(int i = 0 ; i< _slotViews.Length; i++)
        {
            if(_slotViews[i] == null || !_slotViews[i].IsElementOkay)
            {
                throw new InvalidOperationException($"{name} : PotionCodexView at index {i} is not properly configured.");
            }
        }

        if(_selectionBox == null)
        {
            throw new InvalidOperationException($"{name} : SelectionBox reference is not assigned.");
        }
    }

    private void RefreshAllSlots()
    {
        for (int i = 0; i < _slotViews.Length; i++)
        {
            RefreshSlot(i);
        }
    }

    private void RefreshSlot(int slotIndex)
    {
        if(!CheckSlotIndex(slotIndex))
        {
            return;
        }

        PotionItem item = _inventory.GetItemAt(slotIndex);
        Sprite potionSprite = item.IsEmpty ? null : item.BottleSprite;

        int itemCount = _inventory.GetItemCountAt(slotIndex);
        _slotViews[slotIndex].SetPotionSlot(potionSprite, itemCount);
    }

    private void HandleSlotChanged(int slotIndex)
    {
        RefreshSlot(slotIndex);
    }

    private void HandleSelectedSlotChanged(int slotIndex)
    {
        MoveSelectionBox(slotIndex);
    }

    private void MoveSelectionBox(int nextSlotIndex)
    {
        if(!CheckSlotIndex(nextSlotIndex))
        {
            return;
        }
        
        Canvas.ForceUpdateCanvases();

        RectTransform selectedSlot = _slotViews[nextSlotIndex].RectTransform;
        Vector3 slotCenterWorldPos = selectedSlot.TransformPoint(selectedSlot.rect.center);
        _selectionBox.position = slotCenterWorldPos;
        _selectionBox.SetAsLastSibling();
    }

    private bool CheckSlotIndex(int slotIndex)
    {
        return _slotViews != null
            && ( 0 <= slotIndex  && slotIndex < _slotViews.Length)
            && _slotViews[slotIndex] != null;
    }

    private void OnDestroy()
    {
        UnlinkEvents();
        _inventory = null;
    }
}