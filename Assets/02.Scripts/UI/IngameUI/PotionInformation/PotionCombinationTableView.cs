using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PotionCombinationTableView : MonoBehaviour
{
    [SerializeField] private PotionDatabase _potionDatabase;
    [SerializeField] private PotionCombinationDatabase _combinationDatabase;
    [SerializeField] private CombinationCatalogPresenter _details;
    [SerializeField] private RectTransform _dictionarySector;
    [SerializeField] private Button _buttonTemplate;
    [SerializeField] private Sprite _unknownIcon;
    [SerializeField] private RectTransform _table;
    [SerializeField] private ScrollRect _scroll;
    [SerializeField] private RectTransform _content;
    [SerializeField] private PotionCombinationRowView _rowPrefab;
    [SerializeField] private GameObject _previewRow;
    [SerializeField] private TMP_Text _combinationCountLabel;
    [SerializeField] private BookPageTurnController _pageTurnController;
    [Header("Right page")]
    [SerializeField] private GameObject _combinationSector;
    [SerializeField] private GameObject _combinationGuide;
    [SerializeField] private TMP_Text _guideCountLabel;
    [SerializeField] private CombinationInfoSlot _guideDetails;

    private readonly List<Row> _rows = new();
    private PotionRunCatalog _runCatalog;
    private bool _showTable;
    private PotionCombinationData _selectedCombination;
    private float _nextHoverSoundTime;

    private sealed class Row
    {
        public PotionCombinationData Data;
        public PotionCombinationRowView View;
    }

    private void Awake()
    {
        _runCatalog = FindAnyObjectByType<PotionRunCatalog>();
        if (_previewRow != null) _previewRow.SetActive(false);
        BuildRows();
    }

    private void OnEnable()
    {
        if (_runCatalog != null) _runCatalog.OnIdentified += HandleIdentified;
        if (_runCatalog != null) _runCatalog.OnCombinationDiscovered += HandleCombinationDiscovered;
        if (_pageTurnController != null) _pageTurnController.PageRequested += RequestTab;
        SetTab(false);
    }

    private void OnDisable()
    {
        if (_runCatalog != null) _runCatalog.OnIdentified -= HandleIdentified;
        if (_runCatalog != null) _runCatalog.OnCombinationDiscovered -= HandleCombinationDiscovered;
        if (_pageTurnController != null) _pageTurnController.PageRequested -= RequestTab;
    }

    private void BuildRows()
    {
        foreach (PotionCombinationData data in _combinationDatabase.Combinations)
        {
            if (data == null) continue;
            PotionCombinationRowView view = Instantiate(_rowPrefab, _content);
            view.name = "Combination_" + data.name;
            Row row = new Row { Data = data, View = view };
            _rows.Add(row);
            view.Button.onClick.AddListener(() => SelectCombination(row.Data));
            view.Hovered += PlayRowHoverSound;
        }
    }

    private void RequestTab(bool showTable)
    {
        if (_showTable == showTable) return;
        if (_pageTurnController != null)
        {
            if (_pageTurnController.IsTurning) return;
            if (_pageTurnController.TryTurn(showTable, () => SetTab(showTable))) return;
        }
        SetTab(showTable);
    }

    private void SetTab(bool showTable)
    {
        _showTable = showTable;
        _selectedCombination = null;
        _dictionarySector.gameObject.SetActive(!showTable);
        _table.gameObject.SetActive(showTable);
        _buttonTemplate.gameObject.SetActive(!showTable);
        if (_combinationSector != null) _combinationSector.SetActive(!showTable);
        if (_combinationGuide != null) _combinationGuide.SetActive(showTable);
        if (_pageTurnController != null) _pageTurnController.SetNavigation(showTable, !showTable);
        _details.ShowTableCombination(null);
        RefreshRows();
        if (showTable)
        {
            Canvas.ForceUpdateCanvases();
            _scroll.verticalNormalizedPosition = 1f;
        }
    }

    private void HandleIdentified(int appearanceIndex, PotionItem item)
    {
        if (_showTable) RefreshRows();
    }

    private bool IsKnown(PotionCombinationData data)
    {
        return _runCatalog != null && _runCatalog.IsCombinationDiscovered(data);
    }

    private void HandleCombinationDiscovered(PotionCombinationData combination) => RefreshRows();

    private void SelectCombination(PotionCombinationData combination)
    {
        if (!_showTable || !IsKnown(combination)) return;
        SoundManager.Play(GameSoundId.PotionCatalogConfirm);
        _selectedCombination = combination;
        RefreshRows();
        if (_guideDetails != null) _guideDetails.PlayResultReveal();
    }

    private void PlayRowHoverSound()
    {
        if (!isActiveAndEnabled || !_showTable || Time.unscaledTime < _nextHoverSoundTime) return;
        GameSoundData.Entry sound = SoundManager.GetSound(GameSoundId.PotionCatalogHover);
        if (sound?.Clip == null) return;
        _nextHoverSoundTime = Time.unscaledTime + sound.Interval;
        SoundManager.Play(GameSoundId.PotionCatalogHover);
    }

    private void RefreshRows()
    {
        if (!IsKnown(_selectedCombination)) _selectedCombination = null;
        int discoveredCount = 0;
        foreach (Row row in _rows)
        {
            bool known = IsKnown(row.Data);
            if (known) discoveredCount++;
            row.View.Draw(known ? row.Data.Name : "미발견 조합",
                GetIcon(row.Data.ElementA),
                GetIcon(row.Data.ElementB),
                known && row.Data.VisualData.IconSprite != null
                    ? row.Data.VisualData.IconSprite : _unknownIcon,
                known, known && row.Data == _selectedCombination);
        }

        if (_combinationCountLabel != null)
        {
            _combinationCountLabel.text = $"발견한 조합 수 {discoveredCount} / {_rows.Count}";
        }
        if (_guideCountLabel != null)
            _guideCountLabel.text = $"발견한 조합 {discoveredCount} / {_rows.Count}";
        if (_guideDetails != null)
        {
            PotionData first = null, second = null;
            if (_selectedCombination != null)
            {
                _potionDatabase.TryGetByElement(_selectedCombination.ElementA, out first);
                _potionDatabase.TryGetByElement(_selectedCombination.ElementB, out second);
            }
            _guideDetails.DrawTableRecord(first, second, _selectedCombination);
        }
    }

    private Sprite GetIcon(PotionElement element)
    {
        return _potionDatabase.TryGetByElement(element, out PotionData data) &&
            _runCatalog != null && _runCatalog.IsIdentified(data)
            ? data.VisualData.IconSprite : _unknownIcon;
    }

}
