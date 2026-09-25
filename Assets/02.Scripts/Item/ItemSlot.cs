using UnityEngine;

namespace Entity.Inventory
{
    [System.Serializable]
    public class ItemSlot
    {
        [SerializeField] private PotionData _data;
        [SerializeField] private int _quantity;

        public PotionData Data => _data;
        public int Quantity => _quantity;

        internal void SetItem(PotionData data, int quantity)
        {
            _data = data;
            _quantity = quantity;
        }

        internal void ClearSlot()
        {
            _data = null;
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
            return _data == null || _quantity <= 0;
        }
    }
}
