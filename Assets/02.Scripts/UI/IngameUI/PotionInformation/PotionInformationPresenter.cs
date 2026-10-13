using System;
using System.Collections.Generic;
using UnityEngine;

public class PotionInformationPresenter : MonoBehaviour
{
    [Serializable]
    private struct BottleSpriteBiding //TODO : PotionItem의 bottleSprite가 참조가 너무 많아 bottleSprite를 묶지 않음(작은 모양, 큰모양). 따라서 이걸 여기에 넣지말고 Sprite를 쌍으로 묶은 ScriptableObject 같은걸 만들기!
    {
        [SerializeField] private Sprite _smallBottleImage;
        [SerializeField] private Sprite _bigBottleImage;

        public Sprite SmallBottleImage => _smallBottleImage;
        public Sprite BigBottleImage => _bigBottleImage;
    }


    [Header("Runtime Catalog")]
    [SerializeField] private PotionRunCatalog _runCatalog;
    [Header("Potion Cards")]
    [SerializeField] private DictionaryCardSlotView[] _cards;
    [Header("Bottle Images")]
    [SerializeField] private BottleSpriteBiding[] _bottleSprites;

    private Dictionary<Sprite, Sprite> _bottleSpriteFinder = new Dictionary<Sprite, Sprite>(); //<smallSprite, bigSprite>

    private bool[] _revealedToPlayer;
    private bool _isInitialized;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();
        RefreshOnOpen();
    }

    public void RefreshOnOpen()
    {
        Initialize();

        for (int appearanceIndex = 0; appearanceIndex < _runCatalog.Count; appearanceIndex++)
        {
            DictionaryCardSlotView view = _cards[appearanceIndex];

            if (!_runCatalog.IsIdentified(appearanceIndex))
            {
                view.SetHidden();
                continue;
            }

            PotionItem item = _runCatalog.GetPotionItemAt(appearanceIndex);

            PotionData data = item.PotionData;

            Sprite uiPotionSprite = _bottleSpriteFinder[item.BottleSprite];

            Sprite cardBackgroundSprite = data.VisualData.CardBackgroundSprite;

            if (view.HasRevealPlayed)
            {
                view.SetRevealed(data, uiPotionSprite, cardBackgroundSprite);
            }
            else
            {
                view.PlayRevealEffect(data, uiPotionSprite, cardBackgroundSprite);
            }
        }
    }

    private void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        if (_runCatalog == null)
        {
            _runCatalog = FindAnyObjectByType<PotionRunCatalog>();
        }
        _cards = GetComponentsInChildren<DictionaryCardSlotView>(true);

        CheckReferences();

        _revealedToPlayer = new bool[_cards.Length];
        _isInitialized = true;
    }

    private void CheckReferences()
    {
        if (_runCatalog == null)
        {
            throw new InvalidOperationException($"{name}: PotionRunCatalog가 연결되지 않았습니다.");
        }

        if (!_runCatalog.IsInitialized)
        {
            throw new InvalidOperationException($"{name}: PotionRunCatalog가 초기화되지 않았습니다.");
        }

        if (_cards == null || _cards.Length != _runCatalog.Count)
        {
            int cardCount = _cards?.Length ?? 0;

            throw new InvalidOperationException($"{name}: 카드 수({cardCount})와 Appearance 수({_runCatalog.Count})가 다릅니다.");
        }

        for (int i = 0; i < _cards.Length; i++)
        {
            if (_cards[i] == null)
            {
                throw new InvalidOperationException($"{name}: Appearance {i}의 카드 View가 없습니다.");
            }
        }

        foreach (BottleSpriteBiding binding in _bottleSprites)
        {
            if (!_bottleSpriteFinder.TryAdd(binding.SmallBottleImage, binding.BigBottleImage))
            {
                throw new InvalidOperationException($"{name} : SmallBottle Image를 Find에 넣으려니 중복이 됩니다.");
            }
        }
    }
}
