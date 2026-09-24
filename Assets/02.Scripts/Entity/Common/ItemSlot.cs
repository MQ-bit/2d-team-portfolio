using UnityEngine;

namespace Entity.Inventory
{
    [System.Serializable]
    public class ItemSlot
    {
        private GameObject _item; //아이템 데이터, 아마 Potion으로 바꿀 듯(26.09.24)
        private int _quantity;

        public GameObject Item => _item;
        public int Quantity => _quantity;

        internal void SetItem(GameObject item, int quantity)
        {
            _item = item;
            _quantity = quantity;
        }

        internal void ClearSlot()
        {
            _item = null;
            _quantity = 0;
        }

        internal void AddQuantity(int amount)
        {
            _quantity += amount;
        }

        internal void RemoveQuantity(int amount)
        {
            if(amount < 0)
            {
                Debug.LogWarning("Cannot remove a negative quantity.");
                return;
            }
            
            _quantity -= amount;
            if (_quantity < 0)
            {
                _quantity = 0;
            }
        }
        internal bool IsDataEmpty()
        {
            return _item == null || _quantity <= 0;
        }
    }
}
