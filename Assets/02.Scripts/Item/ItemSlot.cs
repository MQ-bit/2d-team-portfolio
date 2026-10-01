using UnityEngine;

namespace Entity.Inventory
{
    [System.Serializable]
    public class ItemSlot
    {
        [SerializeField] private PotionItem _item;
        [SerializeField] private int _quantity;

        public PotionItem Item => _item;
        public PotionData PotionData => _item.PotionData;
        public Sprite Sprite => _item.BottleSprite;
        public int Quantity => _quantity;

        internal void SetItem(PotionItem item, int quantity)
        {
            _item = item;
            _quantity = quantity;
        }

        internal void ClearSlot()
        {
            _item = default;
            _quantity = 0;
        }

        internal void AddQuantity(int amount)
        {
            _quantity += amount;
        }

        internal void RemoveQuantity(int amount)
        {
            if (amount < 0)
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

        internal bool CanStackItem(PotionItem item)
        {
            return !IsEmpty() && _item.IsSameItem(item);
        }

        internal bool IsEmpty()
        {
            return _item.IsEmpty || _quantity <= 0;
        }
    }
}
