using System;
using UnityEngine;

public class PotionCatralogPresenter : MonoBehaviour
{
    [Header("Runtime Catalog")]
    [SerializeField] private PotionRunCatalog _runCatalog;

    [Header("Dictionary Slots")]
    [SerializeField] private PotionDictionaryView[] _slots;

    [Header("Default Icon")]
    [SerializeField] private Sprite _unidentifiedIcon;

    private bool _isSubscribed;

    private void Awake()
    {
        if (_runCatalog == null)
        {
            _runCatalog = FindAnyObjectByType<PotionRunCatalog>();
        }

        if (_slots == null || _slots.Length == 0)
        {
            _slots = GetComponentsInChildren<PotionDictionaryView>(true);
        }
    }

    private void Start()
    {
        ValidateReferences();
        LinkEvents();
        RefreshAllSlots();
    }

    private void LinkEvents()
    {
        if (_isSubscribed)
        {
            return;
        }

        _runCatalog.OnIdentified += HandlePotionIdentified;
        _isSubscribed = true;
    }

    private void UnlinkEvents()
    {
        if (!_isSubscribed || _runCatalog == null)
        {
            return;
        }

        _runCatalog.OnIdentified -= HandlePotionIdentified;
        _isSubscribed = false;
    }

    private void RefreshAllSlots()
    {
        for (int appearanceIndex = 0; appearanceIndex < _runCatalog.Count; appearanceIndex++)
        {
            RefreshSlot(appearanceIndex);
        }
    }

    private void RefreshSlot(int appearanceIndex)
    {
        if (!IsValidSlotIndex(appearanceIndex))
        {
            return;
        }

        PotionItem item = _runCatalog.GetPotionItemAt(appearanceIndex);
        bool isIdentified = _runCatalog.IsIdentified(appearanceIndex);

        _slots[appearanceIndex].SetPotion(item, isIdentified, _unidentifiedIcon);
    }

    private void HandlePotionIdentified(int appearanceIndex, PotionItem identifiedItem)
    {
        if (!IsValidSlotIndex(appearanceIndex))
        {
            return;
        }

        _slots[appearanceIndex].SetPotion(identifiedItem, true, _unidentifiedIcon);
    }

    private bool IsValidSlotIndex(int index)
    {
        return _slots != null
            && (index >= 0 && index < _slots.Length)
            && _slots[index] != null;
    }

    private void ValidateReferences()
    {
        if (_runCatalog == null)
        {
            throw new InvalidOperationException($"{name}: PotionRunCatalog가 연결되지 않았습니다.");
        }

        if (!_runCatalog.IsInitialized)
        {
            throw new InvalidOperationException($"{name}: PotionRunCatalog가 아직 초기화되지 않았습니다.");
        }

        if (_slots == null || _slots.Length != _runCatalog.Count)
        {
            int slotCount = _slots?.Length ?? 0;
            throw new InvalidOperationException($"{name}: 도감 슬롯 수({slotCount})와 이번 판 카탈로그 포션 수({_runCatalog.Count})가 다릅니다.");
        }

        if (_unidentifiedIcon == null)
        {
            throw new InvalidOperationException($"{name}: 미식별 포션 아이콘이 연결되지 않았습니다.");
        }
    }

    private void OnDestroy()
    {
        UnlinkEvents();
    }
}
