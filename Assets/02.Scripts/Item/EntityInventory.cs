using System;
using UnityEngine;

namespace Entity.Inventory
{
    public class EntityInventory : MonoBehaviour
    {
        [Header("Inventory Settings")]
        [SerializeField] private ItemSlot[] _inventorySlots;
        [SerializeField] private int _maxSlotSize = 6;

        private int _occupiedSlots;

        public event Action<int> OnSlotChanged;

        public int MaxSlotSize => _maxSlotSize;

        private void Awake()
        {
            InitializeInventory();
        }

        private void InitializeInventory()
        {
            _occupiedSlots = 0;
            _inventorySlots = new ItemSlot[_maxSlotSize];

            for (int i = 0; i < _maxSlotSize; i++)
            {
                _inventorySlots[i] = new ItemSlot();
            }
        }

        public bool TryAddItem(PotionItem item, int quantity)
        {
            if (item.IsEmpty || quantity <= 0)
            {
                return false;
            }

            for (int i = 0; i < _inventorySlots.Length; i++)
            {
                ItemSlot slot = _inventorySlots[i];

                if (!slot.CanStackItem(item))
                {
                    continue;
                }

                slot.AddQuantity(quantity);
                OnSlotChanged?.Invoke(i);

                return true;
            }

            if (_occupiedSlots >= _maxSlotSize)
            {
                return false;
            }

            // 합칠 슬롯이 없으면 첫 번째 빈 슬롯에 저장한다.
            for (int i = 0; i < _inventorySlots.Length; i++)
            {
                ItemSlot slot = _inventorySlots[i];

                if (!slot.IsEmpty())
                {
                    continue;
                }

                slot.SetItem(item, quantity);
                _occupiedSlots++;

                OnSlotChanged?.Invoke(i);

                return true;
            }

            return false;
        }

        public bool TryConsumeItem(int slotIndex, int quantity, out PotionItem consumedItem)
        {
            consumedItem = default;

            if (!IsValidSlotIndex(slotIndex) || quantity <= 0)
            {
                return false;
            }

            ItemSlot slot = _inventorySlots[slotIndex];

            if (slot == null || slot.IsEmpty() || slot.Quantity < quantity)
            {
                return false;
            }

            consumedItem = slot.Item;

            slot.RemoveQuantity(quantity);

            if (slot.Quantity == 0)
            {
                slot.ClearSlot();
                _occupiedSlots--;
            }

            OnSlotChanged?.Invoke(slotIndex);

            return true;
        }

        public PotionItem GetPotionItem(int index)
        {
            if (!IsValidSlotIndex(index))
            {
                return default;
            }

            ItemSlot slot = _inventorySlots[index];

            if (slot == null || slot.IsEmpty())
            {
                return default;
            }

            return slot.Item;
        }

        // 기존 게임플레이 코드와의 호환용이다.
        public PotionData GetPotionData(int index)
        {
            return GetPotionItem(index).PotionData;
        }

        private bool IsValidSlotIndex(int index)
        {
            return index >= 0 &&
                   index < _inventorySlots.Length;
        }

        public int GetItemCount(int index)
        {
            if (!IsValidSlotIndex(index))
            {
                return 0;
            }

            ItemSlot slot = _inventorySlots[index];
            if (slot == null || slot.IsEmpty())
            {
                return 0;
            }

            return slot.Quantity;
        }
    }
}