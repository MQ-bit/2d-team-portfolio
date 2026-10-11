using System;
using System.Collections.Generic;
using UnityEngine;

public class MinimapPresenter : MonoBehaviour
{
    private static readonly Comparison<MinimapMarkerViewData> MarkerComparison = CompareMarkers;

    [Header("View")]
    [SerializeField] private MinimapView _view;

    [Header("View Range")]
    [SerializeField, Min(1f)] private float _viewWorldSize = 64f;

    [Header("Terrain Colors")]
    [SerializeField] private Color _groundColor = new Color32(139, 90, 43, 255);
    [SerializeField] private Color _wallColor = new Color32(0, 0, 0, 255);

    private StageManager _stageManager;
    private StageManager _subscribedStageManager;
    private PlayerController _player;
    private GeneratedStage _stage;
    private MinimapTerrainData _terrainData;

    private readonly List<MinimapMarker> _sourceBuffer = new List<MinimapMarker>(32);
    private readonly List<MinimapMarkerViewData> _markerBuffer = new List<MinimapMarkerViewData>(32);

    private void Awake()
    {
        if (_view == null)
        {
            _view = GetComponent<MinimapView>();
        }

        if (_view == null)
        {
            Debug.LogError($"{nameof(MinimapPresenter)}: {nameof(MinimapView)}가 없습니다.");
            enabled = false;
        }
    }

    private void OnEnable()
    {
        CheckReferences();
        RefreshStageManagerSubscription();

        BindStage(_stageManager != null ? _stageManager.CurrentStage : null);
    }

    private void OnDisable()
    {
        UnsubscribeStageManager();
        BindStage(null);
    }

    private void OnDestroy()
    {
        DisposeTerrainData();
    }

    private void OnValidate()
    {
        _viewWorldSize = Mathf.Max(1f, _viewWorldSize);
    }

    private void LateUpdate()
    {
        CheckReferences();
        RefreshStageManagerSubscription();

        GeneratedStage currentStage = _stageManager != null ? _stageManager.CurrentStage : null;

        if (!ReferenceEquals(_stage, currentStage))
        {
            BindStage(currentStage);
        }

        if (_stage == null || _stage.Root == null ||
            _player == null || _terrainData == null)
        {
            RenderNoMarkers();
            return;
        }

        UpdateTerrainWindow();
        UpdateMarkers();
    }

    private void CheckReferences()
    {
        if (_stageManager == null)
        {
            _stageManager = StageManager.Instance;
        }

        if (_player == null)
        {
            _player = FindAnyObjectByType<PlayerController>();
        }
    }

    private void RefreshStageManagerSubscription()
    {
        if (ReferenceEquals(_subscribedStageManager, _stageManager))
        {
            return;
        }

        UnsubscribeStageManager();

        if (_stageManager == null || !isActiveAndEnabled)
        {
            return;
        }

        _stageManager.CurrentStageChanged += HandleCurrentStageChanged;
        _subscribedStageManager = _stageManager;
    }

    private void UnsubscribeStageManager()
    {
        if (_subscribedStageManager == null)
        {
            return;
        }

        _subscribedStageManager.CurrentStageChanged -= HandleCurrentStageChanged;
        _subscribedStageManager = null;
    }

    private void HandleCurrentStageChanged(GeneratedStage stage)
    {
        BindStage(stage);
    }

    private void BindStage(GeneratedStage stage)
    {
        if (ReferenceEquals(_stage, stage) && (stage == null || _terrainData != null))
        {
            return;
        }

        DisposeTerrainData();

        _stage = stage;
        _sourceBuffer.Clear();
        _markerBuffer.Clear();
        _view.Clear();

        if (_stage == null)
        {
            return;
        }

        bool builtResult = MinimapTerrainRasterizer.TryBuild(_stage, _viewWorldSize, (Color32)_groundColor, (Color32)_wallColor, out _terrainData);

        if (!builtResult)
        {
            Debug.LogError($"{nameof(MinimapPresenter)}: 미니맵 지형 생성에 실패했습니다.");
            return;
        }

        _view.SetTerrain(_terrainData.Texture);
    }

    private void UpdateTerrainWindow()
    {
        Vector3 playerPosition = _player.transform.position;
        float halfView = _viewWorldSize * 0.5f;

        float textureX = playerPosition.x - halfView - _terrainData.MinCell.x;
        float textureY = playerPosition.y - halfView - _terrainData.MinCell.y;

        Texture2D texture = _terrainData.Texture;

        float uvX = textureX / texture.width;
        float uvY = textureY / texture.height;
        float uvWidth = _viewWorldSize / texture.width;
        float uvHeight = _viewWorldSize / texture.height;

        Rect uvRect = new(uvX, uvY, uvWidth, uvHeight);

        _view.SetTerrainWindow(uvRect);
    }

    private void UpdateMarkers()
    {
        _markerBuffer.Clear();

        Vector2 markerAreaSize = _view.MarkerAreaSize;

        if (markerAreaSize.x <= 0f || markerAreaSize.y <= 0f) // 마커 영역이 유효하지 않으면 마커를 렌더링하지 않음.
        {
            _view.RenderMarkers(_markerBuffer);
            return;
        }

        MinimapMarkerRegistry.CopyActiveMarkersTo(_sourceBuffer);

        foreach (MinimapMarker marker in _sourceBuffer)
        {
            TryAddMarker(marker, markerAreaSize);
        }

        _markerBuffer.Sort(MarkerComparison);
        _view.RenderMarkers(_markerBuffer);
    }

    private void TryAddMarker(MinimapMarker marker, Vector2 markerAreaSize)
    {
        if (marker == null || !marker.IsVisible)
        {
            return;
        }

        bool isPlayerMarker = IsPlayerMarker(marker);
        bool isStageMarker = IsCurrentStageMarker(marker);

        if (!isPlayerMarker && !isStageMarker)
        {
            return;
        }

        Vector2 delta = Vector2.zero;
        if (!isPlayerMarker)
        {
            delta = (Vector2)(marker.WorldPosition - _player.transform.position);
        }

        float halfView = _viewWorldSize * 0.5f;

        bool isInsideView = Mathf.Abs(delta.x) <= halfView && Mathf.Abs(delta.y) <= halfView;

        if (!isInsideView)
        {
            return;
        }

        Vector2 anchoredPosition = new(delta.x / _viewWorldSize * markerAreaSize.x, delta.y / _viewWorldSize * markerAreaSize.y);

        _markerBuffer.Add(new MinimapMarkerViewData(marker.Id, anchoredPosition, marker.Icon, marker.Color, marker.Size, marker.RenderPriority));
    }

    private bool IsPlayerMarker(MinimapMarker marker)
    {
        Transform markerTransform = marker.transform;
        Transform playerTransform = _player.transform;

        return markerTransform == playerTransform || markerTransform.IsChildOf(playerTransform);
    }

    private bool IsCurrentStageMarker(MinimapMarker marker)
    {
        return _stage?.Root != null && marker.transform.IsChildOf(_stage.Root.transform);
    }

    private void RenderNoMarkers()
    {
        _sourceBuffer.Clear();
        _markerBuffer.Clear();

        _view.RenderMarkers(_markerBuffer);
    }

    private static int CompareMarkers(MinimapMarkerViewData left, MinimapMarkerViewData right)
    {
        int priorityComparison = left.RenderPriority.CompareTo(right.RenderPriority);

        if (priorityComparison != 0)
        {
            return priorityComparison;
        }

        return left.Id.CompareTo(right.Id);
    }

    private void DisposeTerrainData()
    {
        _terrainData?.Dispose();
        _terrainData = null;
    }
}