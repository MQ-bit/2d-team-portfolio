using System;
using UnityEngine;
using UnityEngine.UI;

public class CombinationCatalogPresenter : MonoBehaviour
{
    [Header("Models")]
    [SerializeField] private PotionDatabase _potionDatabase;
    [SerializeField] private PotionCombinationDatabase _combinationDatabase;

    [Header("Views")]
    [SerializeField] private CombinationInfoSlot[] _slots;
    [SerializeField] private DictionaryCardSlotView[] _cards;
    [SerializeField] private Button _confirmButton;
    [Header("Fallback Sprite")]
    [SerializeField] private Sprite _missingIcon;

    private bool _isInitialized;
    private DictionaryCardSlotView _firstCard;
    private DictionaryCardSlotView _secondCard;
    private bool _hasConfirmed;
    private PotionCombinationData _confirmedCombination;
    private PotionCombinationData _tableSelection;

    private void Awake()
    {
        CacheViews();
        CheckReferences();
        _isInitialized = true;
    }

    private void OnEnable()
    {
        if (_isInitialized)
        {
            foreach (DictionaryCardSlotView card in _cards) card.OnCardClicked += HandleCardClicked;
            _confirmButton.onClick.AddListener(ConfirmCombination);
            Refresh();
        }
    }

    private void Start()
    {
        Refresh();
    }

    private void OnDisable()
    {
        if (!_isInitialized) return;
        _confirmButton.onClick.RemoveListener(ConfirmCombination);
        _confirmButton.interactable = false;
        foreach (DictionaryCardSlotView card in _cards)
        {
            card.OnCardClicked -= HandleCardClicked;
            card.SetSelected(false);
        }
        _firstCard = _secondCard = null;
        _hasConfirmed = false;
        _confirmedCombination = null;
        _tableSelection = null;
    }

    private void HandleCardClicked(DictionaryCardSlotView card)
    {
        if (card.Potion == null || card.IsRevealing) return;
        _tableSelection = null;
        if (card == _firstCard)
        {
            _firstCard = _secondCard;
            _secondCard = null;
        }
        else if (card == _secondCard)
        {
            _secondCard = null;
        }
        else if (_firstCard == null)
        {
            _firstCard = card;
        }
        else
        {
            _secondCard = card;
        }

        _hasConfirmed = false;
        _confirmedCombination = null;
        Refresh();
    }

    private void ConfirmCombination()
    {
        if (_firstCard == null || _secondCard == null ||
            _firstCard.Potion == null || _secondCard.Potion == null)
        {
            return;
        }

        _combinationDatabase.TryGetCombination(
            _firstCard.Potion.Element, _secondCard.Potion.Element, out _confirmedCombination);
        _hasConfirmed = true;
        Refresh();
    }

    public void ShowTableCombination(PotionCombinationData combination)
    {
        if (!_isInitialized) CacheViews();
        _firstCard = _secondCard = null;
        _tableSelection = combination;
        _confirmedCombination = combination;
        _hasConfirmed = combination != null;
        Refresh();
    }

    public void Refresh()
    {
        CheckReferences();

        foreach (DictionaryCardSlotView card in _cards)
            card.SetSelected(card == _firstCard || card == _secondCard);
        _confirmButton.interactable = _firstCard != null && _secondCard != null;
        Sprite firstIcon = _firstCard != null ? GetPotionIcon(_firstCard.Potion.Element) : null;
        Sprite secondIcon = _secondCard != null ? GetPotionIcon(_secondCard.Potion.Element) : null;
        if (_tableSelection != null)
        {
            firstIcon = GetPotionIcon(_tableSelection.ElementA);
            secondIcon = GetPotionIcon(_tableSelection.ElementB);
        }
        _slots[0].SetVisible(true);
        if (!_hasConfirmed)
        {
            _slots[0].DrawPendingSelection(firstIcon, secondIcon);
            return;
        }

        _slots[0].DrawSelection(firstIcon, secondIcon, _confirmedCombination);
        if (_confirmedCombination != null && _confirmedCombination.VisualData.IconSprite == null)
            _slots[0].DrawIcon(firstIcon, secondIcon, _missingIcon);
    }

    private Sprite GetPotionIcon(PotionElement element)
    {
        if (!_potionDatabase.TryGetByElement(element, out PotionData data) || data == null)
        {
            Debug.LogWarning($"{name} : {element}에 해당하는 PotionData가 없음.");
            return _missingIcon;
        }
        Sprite icon = data.VisualData.IconSprite;
        return icon != null ? icon : _missingIcon;
    }

    private void CacheViews()
    {
        if (_slots == null || _slots.Length == 0)
        {
            _slots = GetComponentsInChildren<CombinationInfoSlot>(true);
        }
        if (_cards == null || _cards.Length == 0)
            _cards = GetComponentsInChildren<DictionaryCardSlotView>(true);
    }

    private void CheckReferences()
    {
        if (_confirmButton == null)
        {
            throw new InvalidOperationException($"{name}: 조합 확인 버튼이 연결되지 않았습니다.");
        }

        if (_potionDatabase == null)
        {
            throw new InvalidOperationException($"{name}: PotionDatabase가 연결되지 않음");
        }

        if (_combinationDatabase == null)
        {
            throw new InvalidOperationException($"{name} : PotionCombinationDatabase가 연결되지 않음.");
        }

        if (_slots == null)
        {
            throw new InvalidOperationException($"{name} : CombinationInfoSlot 배열이 없음.");
        }

        if (_slots.Length != 1)
        {
            throw new InvalidOperationException($"{name}: 조합 결과 슬롯은 하나만 있어야 합니다. 현재 {_slots.Length}개입니다.");
        }
        if (_cards == null || _cards.Length == 0)
            throw new InvalidOperationException($"{name}: 선택할 포션 카드가 없습니다.");

        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] == null)
            {
                throw new InvalidOperationException($"{name}: {i}번 CombinationInfoSlot이 비어있음.");
            }
        }
    }
}
