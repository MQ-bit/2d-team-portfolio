using System;
using UnityEngine;
using UnityEngine.UI;

// 플레이어에게 상태이상이 걸려 있는 동안 화면 테두리에 상태별 연출을 띄운다.
// 화상은 일렁이는 불길, 중독은 맥동하는 독 얼룩, 빙결은 결정 무늬와 서리, 마비는 번쩍이는 전류. 모양은 UI/StatusVignette 셰이더가 그린다.
// 상태가 걸리고 풀리는 이벤트가 없어서 매 프레임 PotionEffectReceiver에 남은 시간을 물어본다.
// 오버레이 캔버스는 런타임에 직접 만들므로 씬에 따로 배치할 것이 없다.
public class StatusScreenVignette : MonoBehaviour
{
    // 셰이더의 _Style 값과 순서를 맞춘다.
    private enum VignetteStyle
    {
        Fire = 0,
        Poison = 1,
        Frost = 2,
        Electric = 3
    }

    [Serializable]
    private class StatusVignette
    {
        [SerializeField] private PotionStatusEffectTracker _status;
        [SerializeField] private VignetteStyle _style;
        [SerializeField] private Color _color = Color.white;

        public PotionStatusEffectTracker Status => _status;
        public VignetteStyle Style => _style;
        public Color Color => _color;
        public Image Image { get; set; }
        public Material Material { get; set; }
        public float Alpha { get; set; }

        public StatusVignette() { }

        public StatusVignette(PotionStatusEffectTracker status, VignetteStyle style, Color color)
        {
            _status = status;
            _style = style;
            _color = color;
        }
    }

    private static readonly int StyleId = Shader.PropertyToID("_Style");
    private static readonly int ThicknessId = Shader.PropertyToID("_Thickness");

    [Header("Component References")]
    [SerializeField] private PotionEffectReceiver _receiver;
    [SerializeField] private Shader _vignetteShader;

    [Header("Vignettes")]
    [SerializeField]
    private StatusVignette[] _vignettes =
    {
        new StatusVignette(PotionStatusEffectTracker.Burning, VignetteStyle.Fire, new Color(1f, 0.25f, 0.1f)),
        new StatusVignette(PotionStatusEffectTracker.Poisoned, VignetteStyle.Poison, new Color(0.35f, 0.9f, 0.2f)),
        new StatusVignette(PotionStatusEffectTracker.Frozen, VignetteStyle.Frost, new Color(0.45f, 0.85f, 1f)),
        new StatusVignette(PotionStatusEffectTracker.Paralyzed, VignetteStyle.Electric, new Color(0.65f, 0.3f, 1f)),
    };

    [Header("Vignette Settings")]
    [SerializeField, Range(0f, 1f)] private float _maxAlpha = 0.1f;
    [Tooltip("테두리 두께(화면 높이 대비 비율)")]
    [SerializeField, Range(0.01f, 1f)] private float _thickness = 0.1f;
    [SerializeField, Min(0.01f)] private float _fadeInTime = 0.2f;
    [Tooltip("상태가 끝나기 이 시간 전부터 서서히 사라진다.")]
    [SerializeField, Min(0.01f)] private float _fadeOutTime = 0.5f;
    [Tooltip("다른 오버레이 UI보다 뒤에 그려지도록 낮게 둔다.")]
    [SerializeField] private int _sortingOrder = -100;

    [Header("Debug")]
    [Tooltip("플레이 중 상태이상 없이도 해당 상태의 연출을 계속 띄운다. 수치 조절용.")]
    [SerializeField] private bool _usePreview;
    [SerializeField] private PotionStatusEffectTracker _previewStatus = PotionStatusEffectTracker.Burning;

    private GameObject _canvasObject;

    private void Awake()
    {
        if (_receiver == null)
        {
            _receiver = GetComponentInChildren<PotionEffectReceiver>();
            if (_receiver == null)
            {
                Debug.LogError($"{gameObject.name} : PotionEffectReceiver 가 할당 안됨");
            }
        }
        if (_vignetteShader == null)
        {
            _vignetteShader = Shader.Find("UI/StatusVignette");
            if (_vignetteShader == null)
            {
                Debug.LogError($"{gameObject.name} : StatusVignette 셰이더가 할당 안됨");
                enabled = false;
                return;
            }
        }

        CreateOverlay();
    }

    private void OnDestroy()
    {
        if (_canvasObject != null)
        {
            Destroy(_canvasObject);
        }

        foreach (StatusVignette vignette in _vignettes)
        {
            if (vignette.Material != null)
            {
                Destroy(vignette.Material);
            }
        }
    }

    private void Update()
    {
        float fadeSpeed = _maxAlpha / _fadeInTime;

        foreach (StatusVignette vignette in _vignettes)
        {
            float remaining = _receiver != null ? _receiver.GetStatusRemainingTime(vignette.Status) : 0f;
            float targetAlpha = remaining > 0f ? _maxAlpha * Mathf.Clamp01(remaining / _fadeOutTime) : 0f;

            if (_usePreview && vignette.Status == _previewStatus)
            {
                targetAlpha = _maxAlpha;
            }

            // 플레이 중 인스펙터에서 두께를 바꿔도 바로 반영되게 매 프레임 넘긴다.
            if (vignette.Material != null)
            {
                vignette.Material.SetFloat(ThicknessId, _thickness);
            }

            vignette.Alpha = Mathf.MoveTowards(vignette.Alpha, targetAlpha, fadeSpeed * Time.deltaTime);
            ApplyAlpha(vignette);
        }
    }

    private void ApplyAlpha(StatusVignette vignette)
    {
        if (vignette.Image == null)
        {
            return;
        }

        Color color = vignette.Color;
        color.a *= vignette.Alpha;
        vignette.Image.color = color;
        vignette.Image.enabled = vignette.Alpha > 0f;
    }

    private void CreateOverlay()
    {
        _canvasObject = new GameObject("StatusVignetteCanvas", typeof(Canvas));
        Canvas canvas = _canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = _sortingOrder;

        // 상태마다 이미지를 따로 둬서 여러 상태가 동시에 걸리면 연출이 겹쳐 보이게 한다.
        foreach (StatusVignette vignette in _vignettes)
        {
            GameObject imageObject = new GameObject($"Vignette_{vignette.Status}", typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(_canvasObject.transform, false);

            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            vignette.Material = new Material(_vignetteShader);
            vignette.Material.SetFloat(StyleId, (float)vignette.Style);
            vignette.Material.SetFloat(ThicknessId, _thickness);

            Image image = imageObject.GetComponent<Image>();
            image.material = vignette.Material;
            image.raycastTarget = false;

            vignette.Image = image;
            vignette.Alpha = 0f;
            ApplyAlpha(vignette);
        }
    }
}
