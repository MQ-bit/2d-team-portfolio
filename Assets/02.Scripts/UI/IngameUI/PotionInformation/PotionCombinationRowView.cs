using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class PotionCombinationRowView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Button _button;
    [SerializeField] private Image _firstIcon;
    [SerializeField] private Image _secondIcon;
    [SerializeField] private Image _resultIcon;
    [SerializeField] private TMP_Text _label;
    [SerializeField] private Outline _selectionOutline;
    [Header("Highlight")]
    [SerializeField] private Color _hoverColor = new Color(0.72f, 0.51f, 0.18f, 0.18f);
    [SerializeField] private Color _selectedColor = new Color(0.72f, 0.51f, 0.18f, 0.28f);
    [SerializeField] private Color _hoverOutlineColor = new Color(0.52f, 0.36f, 0.15f, 0.65f);
    [SerializeField] private Color _selectedOutlineColor = new Color(1f, 0.78f, 0.25f, 1f);

    private bool _known;
    private bool _selected;
    private bool _hovered;

    public Button Button => _button;
    public event Action Hovered;

    private void Awake()
    {
        _button.enabled = true;
        _button.transition = Selectable.Transition.None;
        foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
        if (_button.targetGraphic != null) _button.targetGraphic.raycastTarget = true;
        _selectionOutline.useGraphicAlpha = false;
        RefreshHighlight();
    }

    public void Draw(string title, Sprite first, Sprite second, Sprite result, bool known, bool selected)
    {
        _button.interactable = known;
        _known = known;
        _selected = known && selected;
        _label.text = title;
        SetIcon(_firstIcon, first);
        SetIcon(_secondIcon, second);
        SetIcon(_resultIcon, result);
        RefreshHighlight();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_hovered && _known && _button != null && _button.IsInteractable()) Hovered?.Invoke();
        _hovered = true;
        RefreshHighlight();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hovered = false;
        RefreshHighlight();
    }

    private void OnDisable()
    {
        _hovered = false;
        RefreshHighlight();
    }

    private void RefreshHighlight()
    {
        bool highlighted = _known && (_selected || _hovered);
        if (_button != null && _button.targetGraphic != null)
        {
            _button.targetGraphic.color = highlighted
                ? _selected ? _selectedColor : _hoverColor
                : Color.clear;
        }

        if (_selectionOutline == null) return;
        _selectionOutline.enabled = highlighted;
        _selectionOutline.effectColor = _selected ? _selectedOutlineColor : _hoverOutlineColor;
    }

    private static void SetIcon(Image image, Sprite sprite)
    {
        image.sprite = sprite;
        image.color = sprite != null ? Color.white : Color.clear;
    }
}
