using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PotionCodexView : MonoBehaviour
{
    [SerializeField] private Image _potionImage;
    [SerializeField] private TMP_Text _itemCountText;

    public RectTransform RectTransform => transform as RectTransform;

    public bool IsElementOkay => _potionImage != null && _itemCountText != null && RectTransform != null;

    private void Awake()
    {
        if (_potionImage == null)
        {
            _potionImage = GetComponent<Image>();
        }

        if (_itemCountText == null)
        {
            _itemCountText = GetComponentInChildren<TMP_Text>(true);
        }
    }

    public void SetPotionSlot(Sprite potionSprite, int itemCount)
    {
        SetPotionSprite(potionSprite);
        SetItemCount(itemCount);
    }

    private void SetPotionSprite(Sprite potionSprite)
    {
        _potionImage.sprite = potionSprite;
        _potionImage.preserveAspect = true;
        _potionImage.color = potionSprite == null ? Color.clear : Color.white;
    }

    private void SetItemCount(int itemCount)
    {
        bool shouldShow = itemCount > 0;

        _itemCountText.text = shouldShow ? itemCount.ToString() : string.Empty;

        _itemCountText.gameObject.SetActive(shouldShow);
    }
}