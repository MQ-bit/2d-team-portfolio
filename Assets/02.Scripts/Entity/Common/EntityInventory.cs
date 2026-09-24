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
            AddItem(new PotionData("potion_Fire", "Fire Potion", PotionElement.Fire, 10, 5, 5, 5, 0.25f), 5);
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

        public void AddItem(PotionData data, int quantity)
        {
            if(data == null || quantity <= 0)
            {
                return;
            }

            if (CheckItemExists(data))
            {
                int index = _itemFinder[data];
                _inventorySlots[index].AddQuantity(quantity);
            }
            else
            {
                if(_occupiedSlots >= _maxSlotSize)
                {
                    Debug.LogWarning("Inventory is full. Cannot add more items.");
                    return;
                }

                for(int i = 0 ; i < _maxSlotSize; i++)
                {
                    if(_inventorySlots[i].IsDataEmpty())
                    {
                        _inventorySlots[i].SetItem(data, quantity);
                        _itemFinder[data] = i;
                        _occupiedSlots++;

                        break;
                    }
                }
            }
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
                _itemFinder.Remove(consumedPotion);
                _occupiedSlots--;
                slot.ClearSlot();
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
