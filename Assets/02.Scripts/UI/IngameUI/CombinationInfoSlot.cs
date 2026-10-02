using UnityEngine;
using UnityEngine.UI;

public sealed class CombinationInfoSlot : MonoBehaviour
{
    [Header("Combination Icons")]
    [SerializeField] private Image _elementAIcon;
    [SerializeField] private Image _elementBIcon;
    [SerializeField] private Image _resultIcon;

    public void DrawIcon(Sprite elementAIcon, Sprite elementBIcon, Sprite resultIcon)
    {
        SetImage(_elementAIcon, elementAIcon);
        SetImage(_elementBIcon, elementBIcon);
        SetImage(_resultIcon, resultIcon);
    }

    public void Clear()
    {
        SetImage(_elementAIcon, null);
        SetImage(_elementBIcon, null);
        SetImage(_resultIcon, null);
    }

    public void SetVisible(bool isVisible)
    {
        gameObject.SetActive(isVisible);
    }

    private static void SetImage(Image targetIcon, Sprite sprite)
    {
        if (targetIcon == null)
        {
            return;
        }

        targetIcon.sprite = sprite;
        targetIcon.preserveAspect = true;
        targetIcon.color = sprite == null ? Color.clear : Color.white;
    }

}