using UnityEngine;
using UnityEngine.UI;

public sealed class PotionCodexView : MonoBehaviour
{
    [Header("Potion Slots")]
    [SerializeField] private Image[] _slotImages;

    [Header("Selection")]
    [SerializeField] private RectTransform _potionSelection;
    private int _selectedSlotIndex = -1;


    public int SlotCount => _slotImages != null ? _slotImages.Length : 0;

    public void SetSlot(int index, PotionItem item)
    {
        if (!IsValidSlotIndex(index))
        {
            return;
        }

        Image slotImage = _slotImages[index];
        Sprite potionSprite = item.Sprite;

        slotImage.sprite = potionSprite;
        slotImage.preserveAspect = true;
        slotImage.color = potionSprite == null ? Color.clear : Color.white;
    }

    public void SetSelectedSlot(int index)
    {
        if (!IsValidSlotIndex(index) || _potionSelection == null)
        {
            return;
        }

        _selectedSlotIndex = index;
        Canvas.ForceUpdateCanvases();

        AlignSelection();
    }

    private void AlignSelection()
    {
        if (!IsValidSlotIndex(_selectedSlotIndex))
        {
            return;
        }

        RectTransform selectedSlot = _slotImages[_selectedSlotIndex].rectTransform;
        Vector3 slotCenterWorldPosition = selectedSlot.TransformPoint(selectedSlot.rect.center);
        _potionSelection.position = slotCenterWorldPosition;

        _potionSelection.SetAsLastSibling();
    }

    private bool IsValidSlotIndex(int index)
    {
        return _slotImages != null
            && index >= 0 && index < _slotImages.Length
            && _slotImages[index] != null;
    }
}