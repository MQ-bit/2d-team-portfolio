using UnityEngine;
using System.Collections.Generic;

namespace Entity.Inventory
{
    public class EntityInventory : MonoBehaviour
    {
        [Header("Inventory Settings")]
        [SerializeField] private ItemSlot[] _inventorySlots;
        [SerializeField] private Dictionary<GameObject, int> _itemFinder; // 아이템 / 슬롯 인덱스
        [SerializeField] private int _maxOccupiedSlots;
        private int _occupiedSlots = 0;

        private void Awake()
        {
            InitializeInventory();
        }

        private void InitializeInventory()
        {
            _itemFinder = new Dictionary<GameObject, int>();
            if(_inventorySlots != null)
            {   
                _maxOccupiedSlots = _inventorySlots.Length;
                return;
            }

            _inventorySlots = new ItemSlot[_maxOccupiedSlots];
            for (int i = 0; i < _maxOccupiedSlots; i++)
            {
                _inventorySlots[i] = new ItemSlot();
            }

        }

        public void AddItem(GameObject item, int quantity)
        {
            if(item == null || quantity <= 0)
            {
                return;
            }

            if (CheckItemExists(item))
            {
                int index = _itemFinder[item];
                _inventorySlots[index].AddQuantity(quantity);
            }
            else
            {
                if(_occupiedSlots >= _maxOccupiedSlots)
                {
                    Debug.LogWarning("Inventory is full. Cannot add more items.");
                    return;
                }

                for(int i = 0 ; i < _maxOccupiedSlots; i++)
                {
                    if(_inventorySlots[i].IsDataEmpty())
                    {
                        _inventorySlots[i].SetItem(item, quantity);
                        _itemFinder[item] = i;
                        _occupiedSlots++;

                        break;
                    }
                }
            }
        }

        public void UseItem(GameObject item, int quantity)
        {
            if(!CheckItemExists(item) || quantity <= 0)
            {
                return;
            }

            int index = _itemFinder[item];
            int currentQuantity = _inventorySlots[index].Quantity;
            if(currentQuantity < quantity)
            {
                Debug.LogWarning($"Not enough quantity of {item.name} to use. Current: {currentQuantity}, Requested: {quantity}");
                return;
            }

            _inventorySlots[index].RemoveQuantity(quantity);
            if(_inventorySlots[index].Item == null || _inventorySlots[index].Quantity == 0)
            {
                _itemFinder.Remove(item);
                _occupiedSlots--;
                _inventorySlots[index].ClearSlot();
            }
        }

        public void RemoveItem(GameObject item)
        {
            if(!CheckItemExists(item))
            {
                return;
            }

            int index = _itemFinder[item];
            _inventorySlots[index].ClearSlot();
            _itemFinder.Remove(item);
            _occupiedSlots--;
        }

        private bool CheckItemExists(GameObject item)
        {   
            if(item == null)
            {
                Debug.LogWarning("Item should not be null.");
                return false;
            }

            return _itemFinder.ContainsKey(item);
        }
    }
}
