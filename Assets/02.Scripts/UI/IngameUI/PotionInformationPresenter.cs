using System;
using System.Collections.Generic;
using UnityEngine;

public class PotionInformationPresenter : MonoBehaviour
{
    [Serializable]
    private struct CardBiding
    {
        [SerializeField] private PotionData _potionData;
        [SerializeField] private DictionaryCardSlotView _view;

        public PotionData PotionData => _potionData;
        public DictionaryCardSlotView View => _view;
    }

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
    [SerializeField] private CardBiding[] _cards;
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

        for (int i = 0; i < _cards.Length; i++)
        {
            PotionData data = _cards[i].PotionData;
            DictionaryCardSlotView view = _cards[i].View;

            if (!_runCatalog.IsIdentified(data))
            {
                view.SetHidden();
                continue;
            }

            PotionItem item = _runCatalog.GetPotionItemByData(data);
            Sprite bigBottleSprite = _bottleSpriteFinder[item.BottleSprite];

            if (view.HasRevealPlayed)
            {
                view.SetRevealed(item, bigBottleSprite);
            }
            else
            {
                view.PlayRevealEffect(item, bigBottleSprite);
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

            throw new InvalidOperationException($"{name}: 카드 수({cardCount})와 카탈로그 포션 수 ({_runCatalog.Count})가 다릅니다.");
        }

        HashSet<PotionData> registeredData = new();

        for (int i = 0; i < _cards.Length; i++)
        {
            PotionData data = _cards[i].PotionData;
            DictionaryCardSlotView view = _cards[i].View;

            if (data == null)
            {
                throw new InvalidOperationException($"{name}: {i}번 카드에 PotionData가 없습니다.");
            }

            if (view == null)
            {
                throw new InvalidOperationException($"{name}: {i}번 카드에 View가 없습니다.");
            }

            if (!registeredData.Add(data))
            {
                throw new InvalidOperationException($"{name}: {data.name}이 중복 등록되었습니다.");
            }

            if (!_runCatalog.TryGetAppearanceIndex(data, out _))
            {
                throw new InvalidOperationException($"{name}: {data.name}이 PotionRunCatalog에 없습니다.");
            }
        }

        foreach (BottleSpriteBiding binding in _bottleSprites)
        {
            Sprite smallSprite = binding.SmallBottleImage;
            Sprite bigSprite = binding.BigBottleImage;

            if (smallSprite == null || bigSprite == null)
            {
                throw new InvalidOperationException($"{name} : 포션병 스프라이트가 Null입니다! 체크 요망");
            }

            if (!_bottleSpriteFinder.TryAdd(smallSprite, bigSprite))
            {
                throw new InvalidOperationException($"스프라이트 중복 등록 발생함.");
            }
        }
    }
}
