using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MinimapView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RawImage _terrainImage;
    [SerializeField] private RectTransform _markerLayer;

    private readonly Dictionary<int, Image> _activeMarkers = new Dictionary<int, Image>();

    private readonly Stack<Image> _markerPool = new Stack<Image>();
    private readonly HashSet<int> _seenMarkerIds = new HashSet<int>();
    private readonly List<int> _removeBuffer = new List<int>();

    public Vector2 MarkerAreaSize => _markerLayer != null ? _markerLayer.rect.size : Vector2.zero;

    private void Awake()
    {
        if (_terrainImage != null)
        {
            _terrainImage.raycastTarget = false;
        }
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

    public void RenderMarkers(IReadOnlyList<MinimapMarkerViewData> markers)
    {
        _seenMarkerIds.Clear();

        if (markers != null)
        {
            foreach (MinimapMarkerViewData markerData in markers)
            {
                _seenMarkerIds.Add(markerData.Id);

                Image marker = GetOrCreateMarker(markerData.Id);
                ApplyMarkerData(marker, markerData);
            }
        }

        ReleaseMissingMarkers();
    }

    public void Clear()
    {
        if (_terrainImage != null)
        {
            _terrainImage.texture = null;
        }

        ReleaseAllMarkers();
    }

    private Image GetOrCreateMarker(int markerId)
    {
        if (_activeMarkers.TryGetValue(markerId, out Image marker))
        {
            return marker;
        }

        marker = AcquireMarkerImage();
        _activeMarkers.Add(markerId, marker);

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

    private static void ApplyMarkerData(Image marker, MinimapMarkerViewData markerData)
    {
        Image image = marker;
        bool hasIcon = markerData.Icon != null;

        image.gameObject.SetActive(true);
        image.sprite = markerData.Icon;

        image.color = hasIcon ? Color.white : markerData.Color; // 아이콘이 있으면 원본 색상을 사용. 아이콘이 없으면 지정된 색상의 네모가 됨.

        image.preserveAspect = hasIcon;
        image.type = Image.Type.Simple;
        image.raycastTarget = false;

        RectTransform markerTransform = image.rectTransform;

        markerTransform.anchoredPosition = markerData.AnchoredPosition;
        markerTransform.localRotation = Quaternion.identity;

        markerTransform.sizeDelta = Vector2.one * markerData.Size;

        image.transform.SetAsLastSibling();
    }

    private void ReleaseMissingMarkers()
    {
        _removeBuffer.Clear();

        foreach (KeyValuePair<int, Image> pair in _activeMarkers)
        {
            if (!_seenMarkerIds.Contains(pair.Key))
            {
                _removeBuffer.Add(pair.Key);
            }
        }

        foreach (int markerId in _removeBuffer)
        {
            if (!_activeMarkers.TryGetValue(markerId, out Image marker))
            {
                continue;
            }

            _activeMarkers.Remove(markerId);
            ReleaseMarkerImage(marker);
        }
    }

    private void ReleaseAllMarkers()
    {
        foreach (Image marker in _activeMarkers.Values)
        {
            ReleaseMarkerImage(marker);
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
}