using System;
using System.Collections.Generic;
using UnityEngine;

public class CombinationCatalogPresenter : MonoBehaviour
{
    [Header("Models")]
    [SerializeField] private PotionDatabase _potionDatabase;
    [SerializeField] private PotionCombinationDatabase _combinationDatabase;

    [Header("Views")]
    [SerializeField] private CombinationInfoSlot[] _slots;
    [Header("Fallback Sprite")]
    [SerializeField] private Sprite _missingIcon;

    private bool _isInitialized;

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
            Refresh();
        }
    }

    private void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        CheckReferences();

        IReadOnlyList<PotionCombinationData> combinations = _combinationDatabase.Combinations;

        for (int i = 0; i < _slots.Length; i++)
        {
            bool hasCombination = (i < combinations.Count && combinations[i] != null);

            _slots[i].SetVisible(hasCombination);

            if (!hasCombination)
            {
                _slots[i].Clear();
                continue;
            }

            DrawSlot(_slots[i], combinations[i]);
        }
    }

    private void DrawSlot(CombinationInfoSlot slot, PotionCombinationData data)
    {
        Sprite iconA = GetPotionIcon(data.ElementA);
        Sprite iconB = GetPotionIcon(data.ElementB);
        Sprite resultIcon = data.VisualData.IconSprite;

        if (resultIcon == null)
        {
            resultIcon = _missingIcon;
        }

        slot.DrawIcon(iconA, iconB, resultIcon);
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
    }

    private void CheckReferences()
    {
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

        int combinationCount = _combinationDatabase.Combinations.Count;
        if (_slots.Length < combinationCount)
        {
            throw new InvalidOperationException($"{name}: 조합 데이터는 {combinationCount}개지만, UI Slot은 {_slots.Length}개임.");
        }

        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] == null)
            {
                throw new InvalidOperationException($"{name}: {i}번 CombinationInfoSlot이 비어있음.");
            }
        }
    }
}
