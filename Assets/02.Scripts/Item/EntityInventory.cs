using UnityEngine;
using System.Collections.Generic;

namespace Entity.Inventory
{
    public class EntityInventory : MonoBehaviour
    {
        [Header("Inventory Settings")]
        [SerializeField] private ItemSlot[] _inventorySlots;
        [SerializeField] private int _maxSlotSize = 10;

        private Dictionary<PotionData, int> _itemFinder; // (아이템 / 슬롯 인덱스)
        private int _occupiedSlots = 0;

        public int MaxSlotSize => _maxSlotSize;

        private void Awake()
        {
            InitializeInventory();
        }

        private void InitializeInventory()
        {
            _itemFinder = new Dictionary<PotionData, int>();
            _inventorySlots = new ItemSlot[_maxSlotSize];
            
            for (int i = 0; i < _maxSlotSize; i++)
            {
                _inventorySlots[i] = new ItemSlot();
                //_inventorySlots[i].ClearSlot();
            }
        }

        public bool TryAddItem(PotionData data, int quantity)
        {
            if (data == null || quantity <= 0)
            {
                return false;
            }

            if (CheckItemExists(data))
            {
                _inventorySlots[_itemFinder[data]].AddQuantity(quantity);
                return true;
            }

            if (_occupiedSlots >= _maxSlotSize)
            {
                return false;
            }

            for (int i = 0; i < _inventorySlots.Length; i++)
            {
                if (!_inventorySlots[i].IsDataEmpty())
                {
                    continue;
                }

                _inventorySlots[i].SetItem(data, quantity);
                _itemFinder.Add(data, i);
                _occupiedSlots++;

                return true;
            }

            return false;
        }

        public bool TryConsumeItem(int slotIndex, int quantity, out PotionData consumedPotion)
        {
            consumedPotion = null;

            if (slotIndex < 0 || slotIndex >= _maxSlotSize || quantity <= 0)
            {
                return false;
            }

            ItemSlot slot = _inventorySlots[slotIndex];

            if (slot == null || slot.Data == null || slot.Quantity < quantity)
            {
                return false;
            }

            consumedPotion = slot.Data;
            slot.RemoveQuantity(quantity);

            if (slot.Quantity == 0)
            {
                slot.ClearSlot();
                _itemFinder.Remove(consumedPotion);
                _occupiedSlots--;
            }

            return true;
        }
        
        private bool CheckItemExists(PotionData data)
        {   
            if(data == null)
            {
                Debug.LogWarning("Item Data should not be null.");
                return false;
            }

            return _itemFinder.ContainsKey(data);
        }

        public PotionData GetPotionData(int index)
        {
            if(index < 0 || index >= _maxSlotSize)
            {
                Debug.LogWarning($"{index} is out of bounds for inventory slots.");
                return null;
            }

            if (_inventorySlots[index].IsDataEmpty())
            {
                return null;
            }

            return _inventorySlots[index].Data;
        }
    }
}
