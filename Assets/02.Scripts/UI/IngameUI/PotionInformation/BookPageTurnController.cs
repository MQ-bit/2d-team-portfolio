using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class BookPageTurnController : MonoBehaviour
{
    [Header("Book and live pages")]
    [SerializeField] private RectTransform _book;
    [SerializeField] private RectTransform _leftPage;
    [SerializeField] private RectTransform _rightPage;
    [SerializeField] private CanvasGroup _inputGroup;
    [SerializeField] private CanvasGroup[] _contentGroups;
    [Header("Editable animation layer")]
    [SerializeField] private RectTransform _turnLayer;
    [SerializeField] private RawImage _leftSnapshot;
    [SerializeField] private RawImage _rightSnapshot;
    [SerializeField] private CurvedBookPageImage _turningPage;
    [SerializeField] private CurvedBookPageImage _turningPageBack;
    [SerializeField] private RawImage _pageShadow;
    [Header("Paper curvature")]
    [SerializeField, Range(0f, 2f)] private float _curl = 1.4f;
    [SerializeField, Range(0f, 0.25f)] private float _perspective = 0.14f;
    [Header("Timing and sound")]
    [SerializeField, Min(0.1f)] private float _duration = 0.6f;
    [SerializeField, Min(256)] private int _captureWidth = 2048;
    [SerializeField] private AudioClip _pageTurnSound;
    [SerializeField, Range(0f, 1f)] private float _soundVolume = 0.5f;

    private RenderTexture _oldLeft, _oldRight, _newLeft, _newRight;
    private Texture2D _shadowTexture;
    private Coroutine _routine;
    private float[] _savedAlpha;
    private bool _savedInteractable, _savedRaycasts;
    private bool _hidden;

    public bool IsTurning { get; private set; }

    private void Awake()
    {
        _turnLayer.gameObject.SetActive(false);
        _shadowTexture = new Texture2D(64, 1, TextureFormat.RGBA32, false);
        _shadowTexture.filterMode = FilterMode.Point;
        _shadowTexture.wrapMode = TextureWrapMode.Clamp;
        for (int x = 0; x < 64; x++)
            _shadowTexture.SetPixel(x, 0, new Color(0f, 0f, 0f, Mathf.Pow(1f - x / 63f, 2f)));
        _shadowTexture.Apply();
        _pageShadow.texture = _shadowTexture;
    }

    public bool TryTurn(bool forward, Action showNextPages)
    {
        if (!isActiveAndEnabled || IsTurning || showNextPages == null) return false;
        _routine = StartCoroutine(Turn(forward, showNextPages));
        return true;
    }

    private IEnumerator Turn(bool forward, Action showNextPages)
    {
        IsTurning = true;
        if (!Prepare(showNextPages))
        {
            Cleanup();
            yield break;
        }

        Rect left = BookPageCapture.PageArea(_turnLayer, _leftPage);
        Rect right = BookPageCapture.PageArea(_turnLayer, _rightPage);
        Place(_leftSnapshot.rectTransform, left, 0.5f);
        Place(_rightSnapshot.rectTransform, right, 0.5f);
        _leftSnapshot.texture = forward ? _oldLeft : _newLeft;
        _rightSnapshot.texture = forward ? _newRight : _oldRight;
        _turningPage.texture = forward ? _oldRight : _oldLeft;
        _turningPageBack.texture = forward ? _newLeft : _newRight;
        UpdatePaper(forward, left, right, 0f);
        _turnLayer.gameObject.SetActive(true);

        try
        {
            if (_pageTurnSound != null) SoundManager.Play(_pageTurnSound, SoundChannel.Ui, _soundVolume);
            double started = Time.realtimeSinceStartupAsDouble;
            float elapsed = 0f;
            float duration = Mathf.Max(0.1f, _duration);
            while (elapsed < duration)
            {
                float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                UpdatePaper(forward, left, right, progress);
                yield return null;
                elapsed = (float)(Time.realtimeSinceStartupAsDouble - started);
            }
            UpdatePaper(forward, left, right, 1f);
        }
        finally { Cleanup(); }
    }

    private bool Prepare(Action showNextPages)
    {
        bool changed = false;
        try
        {
            _turnLayer.gameObject.SetActive(false);
            CaptureSpread(out _oldLeft, out _oldRight);
            changed = true;
            showNextPages();
            Canvas.ForceUpdateCanvases();
            CaptureSpread(out _newLeft, out _newRight);
            _savedAlpha = new float[_contentGroups.Length];
            _savedInteractable = _inputGroup.interactable;
            _savedRaycasts = _inputGroup.blocksRaycasts;
            for (int i = 0; i < _contentGroups.Length; i++)
            {
                _savedAlpha[i] = _contentGroups[i].alpha;
            }
            _hidden = true;
            for (int i = 0; i < _contentGroups.Length; i++) _contentGroups[i].alpha = 0f;
            _inputGroup.interactable = false;
            _inputGroup.blocksRaycasts = false;
            return true;
        }
        catch (Exception exception)
        {
            // 캡처에 실패해도 탭 자체는 정상적으로 전환한다.
            if (!changed) showNextPages();
            Debug.LogWarning($"책 페이지 캡처를 완료하지 못했습니다: {exception.Message}", this);
            return false;
        }
    }

    private void CaptureSpread(out RenderTexture left, out RenderTexture right)
    {
        left = right = null;
        RenderTexture spread = BookPageCapture.Capture(_book, _turnLayer, _captureWidth);
        try
        {
            left = BookPageCapture.Crop(spread, _book, _leftPage);
            right = BookPageCapture.Crop(spread, _book, _rightPage);
        }
        finally { RenderTexture.ReleaseTemporary(spread); }
    }

    private void UpdatePaper(bool forward, Rect left, Rect right, float progress)
    {
        Rect source = forward ? right : left;
        Rect destination = forward ? left : right;
        float sourceHinge = forward ? source.xMin : source.xMax;
        float destinationHinge = forward ? destination.xMax : destination.xMin;
        Vector2 hinge = new Vector2(Mathf.Lerp(sourceHinge, destinationHinge, progress),
            Mathf.Lerp(source.center.y, destination.center.y, progress));
        Vector2 size = Vector2.Lerp(source.size, destination.size, progress);
        PlaceSurface(_turningPage.rectTransform, hinge, size);
        PlaceSurface(_turningPageBack.rectTransform, hinge, size);
        _turningPage.Configure(progress, _curl, _perspective, forward, false);
        _turningPageBack.Configure(progress, _curl, _perspective, forward, true);

        Vector2 edge = _turningPage.Project(1f, 0f);
        float lift = Mathf.Sin(progress * Mathf.PI);
        RectTransform shadow = _pageShadow.rectTransform;
        bool extendsRight = edge.x >= 0f;
        shadow.anchorMin = shadow.anchorMax = new Vector2(0.5f, 0.5f);
        shadow.pivot = new Vector2(extendsRight ? 0f : 1f, 0.5f);
        shadow.anchoredPosition = new Vector2(hinge.x + edge.x, hinge.y);
        shadow.sizeDelta = new Vector2(Mathf.Lerp(12f, size.x * 0.15f, lift), size.y);
        _pageShadow.uvRect = extendsRight ? new Rect(0f, 0f, 1f, 1f) : new Rect(1f, 0f, -1f, 1f);
        _pageShadow.color = new Color(1f, 1f, 1f, lift * 0.3f);
    }

    private static void PlaceSurface(RectTransform target, Vector2 hinge, Vector2 size)
    {
        target.anchorMin = target.anchorMax = new Vector2(0.5f, 0.5f);
        target.pivot = new Vector2(0.5f, 0.5f);
        target.anchoredPosition = hinge;
        target.sizeDelta = size;
        target.localScale = Vector3.one;
    }

    private static void Place(RectTransform target, Rect area, float pivotX)
    {
        target.anchorMin = target.anchorMax = new Vector2(0.5f, 0.5f);
        target.pivot = new Vector2(pivotX, 0.5f);
        target.anchoredPosition = new Vector2(Mathf.Lerp(area.xMin, area.xMax, pivotX), area.center.y);
        target.sizeDelta = area.size;
        target.localScale = Vector3.one;
    }

    private void Cleanup()
    {
        if (_hidden)
        {
            for (int i = 0; i < _contentGroups.Length; i++)
                if (_contentGroups[i] != null) _contentGroups[i].alpha = _savedAlpha[i];
            if (_inputGroup != null)
            {
                _inputGroup.interactable = _savedInteractable;
                _inputGroup.blocksRaycasts = _savedRaycasts;
            }
            _hidden = false;
        }
        if (_turnLayer != null) _turnLayer.gameObject.SetActive(false);
        if (_leftSnapshot != null) _leftSnapshot.texture = null;
        if (_rightSnapshot != null) _rightSnapshot.texture = null;
        if (_turningPage != null) _turningPage.texture = null;
        if (_turningPageBack != null) _turningPageBack.texture = null;
        Release(ref _oldLeft); Release(ref _oldRight);
        Release(ref _newLeft); Release(ref _newRight);
        _routine = null;
        IsTurning = false;
    }

    private static void Release(ref RenderTexture texture)
    {
        if (texture == null) return;
        RenderTexture.ReleaseTemporary(texture);
        texture = null;
    }

    private void OnDisable()
    {
        if (_routine != null) StopCoroutine(_routine);
        Cleanup();
    }

    private void OnDestroy()
    {
        if (_shadowTexture == null) return;
        if (Application.isPlaying) Destroy(_shadowTexture);
        else DestroyImmediate(_shadowTexture);
    }
}
