using System.Collections.Generic;
using UnityEngine;

public sealed class MinimapPresenter : MonoBehaviour
{
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

    private readonly List<MinimapMarkerViewData> _markerBuffer = new(32);

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
        ResolveDependencies();
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

    private void LateUpdate()
    {
        ResolveDependencies();
        RefreshStageManagerSubscription();

        GeneratedStage currentStage = _stageManager != null ? _stageManager.CurrentStage : null;

        if (!ReferenceEquals(_stage, currentStage))
        {
            BindStage(currentStage);
        }

        if (_stage == null || _player == null || _terrainData == null)
        {
            _view.SetPlayerVisible(false);
            return;
        }

        _view.SetPlayerVisible(true);

        UpdateTerrainWindow();
        UpdateMarkers();
    }

    private void ResolveDependencies()
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
        _markerBuffer.Clear();
        _view.Clear();

        if (_stage == null)
        {
            return;
        }

        bool built = MinimapTerrainRasterizer.TryBuild(_stage, _viewWorldSize, (Color32)_groundColor, (Color32)_wallColor, out _terrainData);

        if (!built)
        {
            Debug.LogError($"{nameof(MinimapPresenter)}: 미니맵 지형 생성에 실패했습니다.");
            return;
        }

        _view.SetTerrain(_terrainData.Texture);
        _view.SetPlayerVisible(_player != null);
    }

    private void UpdateTerrainWindow()
    {
        Vector3 playerPosition = _player.transform.position;

        float halfView = _viewWorldSize * 0.5f;

        float textureX = playerPosition.x - halfView - _terrainData.MinCell.x;
        float textureY = playerPosition.y - halfView - _terrainData.MinCell.y;

        Texture2D texture = _terrainData.Texture;
        Rect uvRect = new(textureX / texture.width, textureY / texture.height, _viewWorldSize / texture.width, _viewWorldSize / texture.height);

        _view.SetTerrainWindow(uvRect);
    }

    private void UpdateMarkers()
    {
        _markerBuffer.Clear();

        Vector2 markerAreaSize = _view.MarkerAreaSize;

        if (markerAreaSize.x <= 0f || markerAreaSize.y <= 0f)
        {
            _view.RenderMarkers(_markerBuffer);
            return;
        }

        // 호출 순서가 기본 렌더링 우선순위.
        CollectRewardBoxMarkers(markerAreaSize);
        CollectPortalMarkers(markerAreaSize);
        CollectEnemyMarkers(markerAreaSize);

        _view.RenderMarkers(_markerBuffer);
    }

    private void CollectRewardBoxMarkers(Vector2 markerAreaSize)
    {
        Transform root = _stage.TransientsRoot;

        if (root == null)
        {
            return;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform target = root.GetChild(i);

            if (!target.gameObject.activeInHierarchy ||
                !target.TryGetComponent(out InteractablePotionBox box) ||
                box.IsOpened)
            {
                continue;
            }

            TryAddMarker(target, MinimapMarkerType.RewardBox, markerAreaSize);
        }
    }

    private void CollectPortalMarkers(Vector2 markerAreaSize)
    {
        Transform root = _stage.PortalRoot;

        if (root == null)
        {
            return;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform target = root.GetChild(i);

            if (!target.gameObject.activeInHierarchy ||
                !target.TryGetComponent(out InteractablePortal _))
            {
                continue;
            }

            TryAddMarker(target, MinimapMarkerType.Portal, markerAreaSize);
        }
    }

    private void CollectEnemyMarkers(Vector2 markerAreaSize)
    {
        Transform root = _stage.EnemiesRoot;

        if (root == null)
        {
            return;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform target = root.GetChild(i);

            if (!target.gameObject.activeInHierarchy ||
                !target.TryGetComponent(out WaveEnemyHandle _))
            {
                continue;
            }

            TryAddMarker(target, MinimapMarkerType.Enemy, markerAreaSize);
        }
    }

    private void TryAddMarker(Transform target, MinimapMarkerType markerType, Vector2 markerAreaSize)
    {
        Vector2 delta = target.position - _player.transform.position;

        float halfView = _viewWorldSize * 0.5f;

        bool isInsideView = Mathf.Abs(delta.x) <= halfView && Mathf.Abs(delta.y) <= halfView;

        if (!isInsideView)
        {
            return;
        }

        Vector2 anchoredPosition = new(delta.x / _viewWorldSize * markerAreaSize.x, delta.y / _viewWorldSize * markerAreaSize.y);

        _markerBuffer.Add(new MinimapMarkerViewData(target.gameObject.GetInstanceID(), markerType, anchoredPosition));
    }

    private void DisposeTerrainData()
    {
        _terrainData?.Dispose();
        _terrainData = null;
    }
}