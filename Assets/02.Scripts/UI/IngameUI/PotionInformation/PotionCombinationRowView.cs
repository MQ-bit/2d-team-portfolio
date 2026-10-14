using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PotionCombinationRowView : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private Image _firstIcon;
    [SerializeField] private Image _secondIcon;
    [SerializeField] private Image _resultIcon;
    [SerializeField] private TMP_Text _label;
    [SerializeField] private Outline _selectionOutline;

    public Button Button => _button;

    public void Draw(string title, Sprite first, Sprite second, Sprite result, bool known, bool selected)
    {
        _button.interactable = known;
        _label.text = title;
        SetIcon(_firstIcon, first);
        SetIcon(_secondIcon, second);
        SetIcon(_resultIcon, result);
        _button.targetGraphic.color = selected
            ? new Color(1f, 0.88f, 0.55f) : new Color(1f, 0.80f, 0.54f);
        _selectionOutline.enabled = selected;
    }

    private static void SetIcon(Image image, Sprite sprite)
    {
        image.sprite = sprite;
        image.color = sprite != null ? Color.white : Color.clear;
    }
}
