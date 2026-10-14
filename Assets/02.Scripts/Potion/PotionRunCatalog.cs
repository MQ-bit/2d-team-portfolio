using System;
using System.Collections.Generic;
using UnityEngine;

public class PotionRunCatalog : MonoBehaviour
{
    private const int Required_Potion_Count = 6;

    [Header("Potion Definitions")]
    [SerializeField] private PotionDatabase _database;
    [SerializeField] private Sprite[] _potionSprites = new Sprite[Required_Potion_Count];

    [Header("Random")]
    [SerializeField] private bool _useRandomSeed = true;
    [SerializeField] private int _fixedSeed;

    private readonly List<PotionItem> _items = new List<PotionItem>();
    private readonly Dictionary<PotionData, int> _apperanceIndexes = new Dictionary<PotionData, int>();
    private bool[] _identified = Array.Empty<bool>();
    private readonly HashSet<PotionCombinationData> _discoveredCombinations = new();

    public event Action<int, PotionItem> OnIdentified; //UI 연결용
    public event Action<PotionCombinationData> OnCombinationDiscovered;

    public int Count => _items.Count;
    public int CurrentSeed { get; private set; }
    public bool IsInitialized { get; private set; }

    private void Awake()
    {
        int seed = _useRandomSeed ? Guid.NewGuid().GetHashCode() : _fixedSeed;

        Initialize(seed);
    }

    public void Initialize(int seed)
    {
        CheckDataBase();
        CurrentSeed = seed;
        IsInitialized = false;

        _items.Clear();
        _apperanceIndexes.Clear();
        _discoveredCombinations.Clear();
        _identified = new bool[_potionSprites.Length];

        List<PotionData> shuffledPotions = new List<PotionData>(_database.Potions);
        Shuffle(shuffledPotions, new System.Random(seed));

        for (int i = 0; i < _potionSprites.Length; i++)
        {
            PotionData data = shuffledPotions[i];
            PotionItem item = new PotionItem(data, _potionSprites[i]);

            _items.Add(item);
            _apperanceIndexes.Add(data, i);
        }

        IsInitialized = true;
    }

    public PotionItem GetPotionItemAt(int index)
    {
        if (!IsValidAppearanceIndex(index))
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return _items[index];
    }

    public PotionItem GetPotionItemByData(PotionData data)
    {
        if (!TryGetAppearanceIndex(data, out int index))
        {
            throw new KeyNotFoundException($"Not Added Potion Data fot this Run : {data?.name ?? "Null"}");
        }

        return _items[index];
    }

    public bool TryGetAppearanceIndex(PotionData data, out int index)
    {
        if (data == null)
        {
            index = -1;
            return false;
        }

        return _apperanceIndexes.TryGetValue(data, out index);
    }

    public bool IsIdentified(int index)
    {
        return IsValidAppearanceIndex(index)
            && _identified[index];
    }

    public bool IsIdentified(PotionData data)
    {
        return TryGetAppearanceIndex(data, out int appearanceIndex)
            && IsIdentified(appearanceIndex);
    }

    public bool IsCombinationDiscovered(PotionCombinationData combination) =>
        combination != null && _discoveredCombinations.Contains(combination);

    // Only successful world reactions call this; identifying ingredients does not unlock a recipe.
    public bool TryDiscoverCombination(PotionCombinationData combination)
    {
        if (!IsInitialized || combination == null || !_discoveredCombinations.Add(combination)) return false;
        OnCombinationDiscovered?.Invoke(combination);
        return true;
    }

    /// <summary>
    /// 포션 종류 해금을 시도하는 함수
    /// </summary>
    /// <param name="item">포션 아이템</param>
    /// <returns></returns>
    public bool TryIdentify(PotionItem item)
    {
        if (item.IsEmpty || !TryGetAppearanceIndex(item.PotionData, out int index))
        {
            return false;
        }

        PotionItem registeredItem = _items[index];

        if (!registeredItem.IsSameItem(item))
        {
            Debug.LogWarning($"Doesn't Match Potion at This Run!");
            return false;
        }

        if (_identified[index])
        {
            return false;
        }

        _identified[index] = true;
        OnIdentified?.Invoke(index, registeredItem);

        return true;
    }

    private bool IsValidAppearanceIndex(int index)
    {
        return 0 <= index && index < _items.Count;
    }

    private void CheckDataBase()
    {
        if (_database == null)
        {
            throw new InvalidOperationException($"{name}: PotionDatabase가 할당되지 않았습니다.");
        }

        if (_database.Count != Required_Potion_Count)
        {
            throw new InvalidOperationException($"PotionData는 정확히 {Required_Potion_Count}개여야 합니다. 현재 개수: {_database.Count}");
        }

        if (_potionSprites == null || _potionSprites.Length != Required_Potion_Count)
        {
            throw new InvalidOperationException($"포션 Sprite는 정확히 {Required_Potion_Count}개여야 합니다.");
        }

        HashSet<Sprite> sprites = new HashSet<Sprite>();

        for (int i = 0; i < Required_Potion_Count; i++)
        {
            PotionData data = _database.Potions[i];
            Sprite sprite = _potionSprites[i];

            if (data == null)
            {
                throw new InvalidOperationException($"PotionDatabase의 {i}번 항목이 비어 있습니다.");
            }
            if (sprite == null || !sprites.Add(sprite))
            {
                throw new InvalidOperationException($"{i}번 포션 Sprite가 비어 있거나 중복되었습니다.");
            }
        }
    }

    private static void Shuffle<T>(IList<T> values, System.Random random)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int swapIndex = random.Next(i + 1);

            (values[i], values[swapIndex]) = (values[swapIndex], values[i]);
        }
    }

}
