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
    [SerializeField] private TMP_Text _statusLabel;
    [Header("Empty slot marks")]
    [SerializeField] private GameObject _firstPlaceholder;
    [SerializeField] private GameObject _secondPlaceholder;
    [SerializeField] private GameObject _resultPlaceholder;

    public void DrawPendingSelection(Sprite firstIcon, Sprite secondIcon)
    {
        DrawIcon(firstIcon, secondIcon, null);
        int selectedCount = (firstIcon != null ? 1 : 0) + (secondIcon != null ? 1 : 0);
        if (_statusLabel != null)
        {
            _statusLabel.text = selectedCount == 2 ? "아직 남겨진 기록이 없다"
                : selectedCount == 1 ? "재료 하나가 더 필요하다" : "아직 밝혀진 것은 없다";
        }

        if (_nameText != null) _nameText.text = "조합 결과";
        if (_descriptionText != null)
        {
            _descriptionText.text = firstIcon == null && secondIcon == null
                ? "왼쪽에서 포션 두 개를 고르자.\n직접 확인한 반응만 이곳에 남는다."
                : secondIcon == null
                    ? "한 가지 재료로는 부족하다.\n함께 쓸 포션을 하나 더 고르자."
                    : "이 조합의 반응은 아직 알려지지 않았다.\n현장에서 장판이 겹치도록 포션을 던져보자.";
        }
    }

    public void DrawRecordedSelectionPending(Sprite firstIcon, Sprite secondIcon)
    {
        DrawPendingSelection(firstIcon, secondIcon);
        if (_statusLabel != null) _statusLabel.text = "기록이 남아 있다";
        if (_descriptionText != null)
            _descriptionText.text = "이미 확인한 반응이다.\n아래에서 남겨진 기록을 열람하자.";
    }

    public void DrawSelection(Sprite firstIcon, Sprite secondIcon, PotionCombinationData combination)
    {
        DrawIcon(firstIcon, secondIcon, combination != null ? combination.VisualData.IconSprite : null);
        if (_statusLabel != null)
            _statusLabel.text = combination != null ? "확인된 반응 기록" : "기록이 없다";
        if (_nameText != null)
            _nameText.text = combination != null ? combination.Name : "포션 조합";
        if (_descriptionText != null)
        {
            _descriptionText.text = combination != null ? combination.Description
                : "이 두 포션에는 조합 효과가 없다.\n다른 조합을 찾아보자.";
        }
    }

    public void DrawIcon(Sprite elementAIcon, Sprite elementBIcon, Sprite resultIcon)
    {
        SetImage(_elementAIcon, elementAIcon);
        SetImage(_elementBIcon, elementBIcon);
        SetImage(_resultIcon, resultIcon);
        if (_firstPlaceholder != null) _firstPlaceholder.SetActive(elementAIcon == null);
        if (_secondPlaceholder != null) _secondPlaceholder.SetActive(elementBIcon == null);
        if (_resultPlaceholder != null) _resultPlaceholder.SetActive(resultIcon == null);
    }

    public void Clear()
    {
        DrawIcon(null, null, null);
        if (_nameText != null) _nameText.text = string.Empty;
        if (_descriptionText != null) _descriptionText.text = string.Empty;
        if (_statusLabel != null) _statusLabel.text = string.Empty;
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
