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

            if(_inventorySlots != null)
            {   
                _maxSlotSize = _inventorySlots.Length;

                for(int i = 0 ; i < _inventorySlots.Length; i++)
                {
                    _itemFinder[_inventorySlots[i].Data] = i;
                }
            }
            else
            {
                _inventorySlots = new ItemSlot[_maxSlotSize];
                for (int i = 0; i < _maxSlotSize; i++)
                {
                    _inventorySlots[i] = new ItemSlot();
                }
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

        public void ConsumeItem(PotionData data, int quantity)
        {
            if(!CheckItemExists(data) || quantity <= 0)
            {
                return;
            }

            int index = _itemFinder[data];
            int currentQuantity = _inventorySlots[index].Quantity;
            if(currentQuantity < quantity)
            {
                Debug.LogWarning($"Not enough quantity of {data.Name} to use. Current: {currentQuantity}, Requested: {quantity}");
                return;
            }

            _inventorySlots[index].RemoveQuantity(quantity);
            if(_inventorySlots[index].IsDataEmpty() || _inventorySlots[index].Quantity == 0)
            {
                _itemFinder.Remove(data);
                _occupiedSlots--;
                _inventorySlots[index].ClearSlot();
            }
        }

        public void RemoveItem(PotionData data)
        {
            if(!CheckItemExists(data))
            {
                return;
            }

            int index = _itemFinder[data];
            _inventorySlots[index].ClearSlot();
            _itemFinder.Remove(data);
            _occupiedSlots--;
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

            return _inventorySlots[index].Data;
        }
    }
}
