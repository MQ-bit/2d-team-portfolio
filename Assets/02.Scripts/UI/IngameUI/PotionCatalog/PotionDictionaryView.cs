using Coffee.UIExtensions;
using UnityEngine;
using UnityEngine.UI;

public class PotionDictionaryView : MonoBehaviour
{
    [Header("Slot Images")]
    [SerializeField] private Image _potionIcon;
    [SerializeField] private Image _potionTypeIcon;
    [SerializeField] private UIParticle _identifyEffect;

    private void Awake()
    {
        if (_identifyEffect == null)
        {
            _identifyEffect = GetComponentInChildren<UIParticle>(true);
        }
    }

    public void SetPotion(PotionItem item, bool isIdentified, Sprite unidentifiedIcon)
    {
        if (item.IsEmpty)
        {
            Clear();
            return;
        }

        SetImage(_potionIcon, item.BottleSprite);

        Sprite typeIcon = isIdentified ? item.PotionData.VisualData.IconSprite : unidentifiedIcon;

        if (typeIcon == null)
        {
            typeIcon = unidentifiedIcon;
        }
        if (isIdentified == true)
        {
            _identifyEffect.Play();
        }

        SetImage(_potionTypeIcon, typeIcon);
    }

    public void Clear()
    {
        SetImage(_potionIcon, null);
        SetImage(_potionTypeIcon, null);
    }

    private static void SetImage(Image image, Sprite sprite)
    {
        if (image == null)
        {
            return;
        }

        image.sprite = sprite;
        image.preserveAspect = true;
        image.color = sprite == null ? Color.clear : Color.white;
    }
}
