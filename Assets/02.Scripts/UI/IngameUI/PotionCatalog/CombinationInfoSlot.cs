using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CombinationInfoSlot : MonoBehaviour
{
    [Header("Combination Icons")]
    [SerializeField] private Image _elementAIcon;
    [SerializeField] private Image _elementBIcon;
    [SerializeField] private Image _resultIcon;
    [Header("Combination Details")]
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _descriptionText;

    public void DrawPendingSelection(Sprite firstIcon, Sprite secondIcon)
    {
        DrawIcon(firstIcon, secondIcon, null);
        if (_nameText != null) _nameText.text = "포션 조합";
        if (_descriptionText != null)
            _descriptionText.text = string.Empty;
    }

    public void DrawSelection(Sprite firstIcon, Sprite secondIcon, PotionCombinationData combination)
    {
        DrawIcon(firstIcon, secondIcon, combination != null ? combination.VisualData.IconSprite : null);
        if (_nameText != null)
            _nameText.text = combination != null ? combination.Name : "포션 조합";
        if (_descriptionText != null)
        {
            _descriptionText.text = combination != null ? combination.Description
                : "이 두 포션의 조합 효과는 없습니다.";
        }
    }

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
        if (_nameText != null) _nameText.text = string.Empty;
        if (_descriptionText != null) _descriptionText.text = string.Empty;
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
