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
    [Header("Prefab Views")]
    [SerializeField] private Button _dictionaryTab;
    [SerializeField] private Button _combinationTab;
    [SerializeField] private RectTransform _table;
    [SerializeField] private ScrollRect _scroll;
    [SerializeField] private RectTransform _content;
    [SerializeField] private PotionCombinationRowView _rowPrefab;
    [SerializeField] private GameObject _previewRow;
    [SerializeField] private BookPageTurnController _pageTurnController;

    private readonly List<Row> _rows = new();
    private PotionRunCatalog _runCatalog;
    private PotionCombinationData _selected;
    private bool _showTable;

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
        _dictionaryTab.onClick.AddListener(ShowDictionary);
        _combinationTab.onClick.AddListener(ShowCombinations);
        SetTab(false);
    }

    private void OnDisable()
    {
        if (_runCatalog != null) _runCatalog.OnIdentified -= HandleIdentified;
        _dictionaryTab.onClick.RemoveListener(ShowDictionary);
        _combinationTab.onClick.RemoveListener(ShowCombinations);
        _selected = null;
    }

    private void BuildRows()
    {
        foreach (PotionCombinationData data in _combinationDatabase.Combinations)
        {
            if (data == null) continue;
            PotionCombinationRowView view = Instantiate(_rowPrefab, _content);
            view.name = "Combination_" + data.name;
            Row row = new Row { Data = data, View = view };
            view.Button.onClick.AddListener(() => Select(row));
            _rows.Add(row);
        }
    }

    private void ShowDictionary()
    {
        if (_showTable) RequestTab(false);
    }

    private void ShowCombinations()
    {
        if (!_showTable) RequestTab(true);
    }

    private void RequestTab(bool showTable)
    {
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
        _dictionarySector.gameObject.SetActive(!showTable);
        _table.gameObject.SetActive(showTable);
        _buttonTemplate.gameObject.SetActive(!showTable);
        SetTabAppearance(_dictionaryTab, !showTable);
        SetTabAppearance(_combinationTab, showTable);
        _selected = null;
        _details.ShowTableCombination(null);
        RefreshRows();
        if (showTable)
        {
            Canvas.ForceUpdateCanvases();
            _scroll.verticalNormalizedPosition = 1f;
        }
    }

    private void Select(Row row)
    {
        if (!IsKnown(row.Data)) return;
        _selected = row.Data;
        _details.ShowTableCombination(row.Data);
        RefreshRows();
    }

    private static void SetTabAppearance(Button tab, bool selected)
    {
        tab.interactable = true;
        tab.targetGraphic.color = selected
            ? new Color(1f, 0.84f, 0.52f, 1f) : new Color(0.90f, 0.88f, 0.82f, 1f);
        tab.GetComponentInChildren<TMP_Text>(true).color = selected
            ? new Color(0.12f, 0.07f, 0.03f, 1f) : new Color(0.24f, 0.18f, 0.11f, 1f);
        tab.transform.localScale = selected ? Vector3.one * 0.97f : Vector3.one;
        ColorBlock colors = tab.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        colors.selectedColor = Color.white;
        colors.fadeDuration = 0.12f;
        tab.colors = colors;
    }

    private void HandleIdentified(int appearanceIndex, PotionItem item)
    {
        if (_showTable) RefreshRows();
    }

    private bool IsKnown(PotionCombinationData data)
    {
        return _runCatalog != null &&
            _potionDatabase.TryGetByElement(data.ElementA, out PotionData first) &&
            _potionDatabase.TryGetByElement(data.ElementB, out PotionData second) &&
            _runCatalog.IsIdentified(first) && _runCatalog.IsIdentified(second);
    }

    private void RefreshRows()
    {
        foreach (Row row in _rows)
        {
            bool known = IsKnown(row.Data);
            row.View.Draw(known ? row.Data.Name : "미발견 조합",
                GetIcon(row.Data.ElementA),
                GetIcon(row.Data.ElementB),
                known && row.Data.VisualData.IconSprite != null
                    ? row.Data.VisualData.IconSprite : _unknownIcon,
                known, row.Data == _selected);
        }
    }

    private Sprite GetIcon(PotionElement element)
    {
        return _potionDatabase.TryGetByElement(element, out PotionData data) &&
            _runCatalog != null && _runCatalog.IsIdentified(data)
            ? data.VisualData.IconSprite : _unknownIcon;
    }

}
