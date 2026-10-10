using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class MinimapView : MonoBehaviour
{
    private sealed class MarkerEntry
    {
        public Image Image { get; }
        public MinimapMarkerType Type { get; set; }

        public MarkerEntry(Image image, MinimapMarkerType type)
        {
            Image = image;
            Type = type;
        }
    }

    [Header("UI")]
    [SerializeField] private RawImage _terrainImage;
    [SerializeField] private RectTransform _markerLayer;
    [SerializeField] private Image _playerMarker;

    [Header("Icons")]
    [SerializeField] private Sprite _boxIcon;
    [SerializeField] private Sprite _portalIcon;

    [Header("Colors")]
    [SerializeField] private Color _playerColor = new Color32(255, 220, 0, 255);

    [SerializeField] private Color _enemyColor = new Color32(220, 48, 48, 255);

    [Header("Marker Sizes")]
    [SerializeField, Min(1f)] private float _playerMarkerSize = 10f;

    [SerializeField, Min(1f)] private float _enemyMarkerSize = 8f;

    [SerializeField, Min(1f)] private float _objectMarkerSize = 16f;

    private readonly Dictionary<int, MarkerEntry> _activeMarkers = new Dictionary<int, MarkerEntry>();

    private readonly Stack<Image> _markerPool = new Stack<Image>();
    private readonly HashSet<int> _seenMarkerIds = new HashSet<int>();
    private readonly List<int> _removeBuffer = new List<int>();

    public Vector2 MarkerAreaSize => _markerLayer != null ? _markerLayer.rect.size : Vector2.zero;

    private void Awake()
    {
        ConfigureStaticUI();
    }

    private void OnDisable()
    {
        ReleaseAllMarkers();
    }

    public void SetTerrain(Texture texture)
    {
        if (_terrainImage == null)
        {
            return;
        }

        _terrainImage.texture = texture;
        _terrainImage.color = Color.white;
    }

    public void SetTerrainWindow(Rect uvRect)
    {
        if (_terrainImage != null)
        {
            _terrainImage.uvRect = uvRect;
        }
    }

    public void SetPlayerVisible(bool visible)
    {
        if (_playerMarker != null)
        {
            _playerMarker.gameObject.SetActive(visible);
        }
    }

    public void RenderMarkers(IReadOnlyList<MinimapMarkerViewData> markers)
    {
        _seenMarkerIds.Clear();

        if (markers != null)
        {
            for (int i = 0; i < markers.Count; i++)
            {
                MinimapMarkerViewData markerData = markers[i];

                _seenMarkerIds.Add(markerData.Id);

                MarkerEntry marker = GetOrCreateMarker(markerData);

                ApplyMarkerData(marker, markerData);
            }
        }

        ReleaseMissingMarkers();

        if (_playerMarker != null)
        {
            _playerMarker.transform.SetAsLastSibling();
        }
    }

    public void Clear()
    {
        if (_terrainImage != null)
        {
            _terrainImage.texture = null;
        }

        SetPlayerVisible(false);
        ReleaseAllMarkers();
    }

    private MarkerEntry GetOrCreateMarker(MinimapMarkerViewData markerData)
    {
        if (_activeMarkers.TryGetValue(markerData.Id, out MarkerEntry marker))
        {
            if (marker.Type != markerData.Type)
            {
                marker.Type = markerData.Type;
                ApplyMarkerStyle(marker);
            }

            return marker;
        }

        Image image = AcquireMarkerImage();

        marker = new MarkerEntry(image, markerData.Type);

        ApplyMarkerStyle(marker);

        _activeMarkers.Add(markerData.Id, marker);

        return marker;
    }

    private Image AcquireMarkerImage()
    {
        Image image;

        if (_markerPool.Count > 0)
        {
            image = _markerPool.Pop();
        }
        else
        {
            GameObject markerObject = new("Minimap Marker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

            markerObject.layer = gameObject.layer;

            RectTransform markerTransform = markerObject.GetComponent<RectTransform>();

            markerTransform.SetParent(_markerLayer, false);

            markerTransform.anchorMin = new Vector2(0.5f, 0.5f);
            markerTransform.anchorMax = new Vector2(0.5f, 0.5f);
            markerTransform.pivot = new Vector2(0.5f, 0.5f);

            image = markerObject.GetComponent<Image>();
            image.raycastTarget = false;
        }

        image.gameObject.SetActive(true);
        return image;
    }

    private void ApplyMarkerData(MarkerEntry marker, MinimapMarkerViewData markerData)
    {
        marker.Image.gameObject.SetActive(true);
        marker.Image.rectTransform.anchoredPosition = markerData.AnchoredPosition;
        marker.Image.rectTransform.localRotation = Quaternion.identity;
        marker.Image.transform.SetAsLastSibling();
    }

    private void ApplyMarkerStyle(MarkerEntry marker)
    {
        Image image = marker.Image;

        switch (marker.Type)
        {
            case MinimapMarkerType.Enemy:
            {
                image.gameObject.name = "Enemy Marker";
                image.sprite = null;
                image.color = _enemyColor;
                image.preserveAspect = false;
                image.rectTransform.sizeDelta = Vector2.one * _enemyMarkerSize;
                break;
            }

            case MinimapMarkerType.RewardBox:
            {
                image.gameObject.name = "Reward Box Marker";
                image.sprite = _boxIcon;
                image.color = Color.white;
                image.preserveAspect = true;
                image.rectTransform.sizeDelta = Vector2.one * _objectMarkerSize;
                break;
            }

            case MinimapMarkerType.Portal:
            {
                image.gameObject.name = "Portal Marker";
                image.sprite = _portalIcon;
                image.color = Color.white;
                image.preserveAspect = true;
                image.rectTransform.sizeDelta = Vector2.one * _objectMarkerSize;
                break;
            }
        }
    }

    private void ReleaseMissingMarkers()
    {
        _removeBuffer.Clear();

        foreach (KeyValuePair<int, MarkerEntry> pair in _activeMarkers)
        {
            if (!_seenMarkerIds.Contains(pair.Key))
            {
                _removeBuffer.Add(pair.Key);
            }
        }

        foreach (int markerId in _removeBuffer)
        {
            if (!_activeMarkers.TryGetValue(markerId, out MarkerEntry marker))
            {
                continue;
            }

            _activeMarkers.Remove(markerId);
            ReleaseMarkerImage(marker.Image);
        }
    }

    private void ReleaseAllMarkers()
    {
        foreach (MarkerEntry marker in _activeMarkers.Values)
        {
            ReleaseMarkerImage(marker.Image);
        }

        _activeMarkers.Clear();
        _seenMarkerIds.Clear();
        _removeBuffer.Clear();
    }

    private void ReleaseMarkerImage(Image image)
    {
        if (image == null)
        {
            return;
        }

        image.gameObject.SetActive(false);
        _markerPool.Push(image);
    }

    private void ConfigureStaticUI()
    {
        if (_terrainImage != null)
        {
            _terrainImage.raycastTarget = false;
        }

        if (_playerMarker == null)
        {
            return;
        }

        _playerMarker.sprite = null;
        _playerMarker.color = _playerColor;
        _playerMarker.preserveAspect = false;
        _playerMarker.raycastTarget = false;

        _playerMarker.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);

        _playerMarker.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

        _playerMarker.rectTransform.pivot = new Vector2(0.5f, 0.5f);

        _playerMarker.rectTransform.anchoredPosition = Vector2.zero;

        _playerMarker.rectTransform.sizeDelta = Vector2.one * _playerMarkerSize;

        _playerMarker.rectTransform.localRotation = Quaternion.identity;
    }
}