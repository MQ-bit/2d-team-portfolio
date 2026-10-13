using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;

// Canvas의 UI보다 뒤에 숫자 전용 레이어를 만든다. 생성한 TMP와 머티리얼은 이 씬에서만 소유한다.
[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public sealed class DamageNumberPresenter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_FontAsset _font;
    [Tooltip("비워두면 Main Camera를 사용합니다.")]
    [SerializeField] private Camera _worldCamera;

    [Header("Appearance")]
    [SerializeField, Min(1f)] private float _fontSize = 42f;
    [SerializeField] private Color _enemyColor = new Color(1f, 0.94f, 0.7f);
    [SerializeField] private Color _playerColor = new Color(1f, 0.35f, 0.3f);
    [SerializeField] private Color _healingColor = new Color(0.35f, 1f, 0.45f);
    [SerializeField] private Color _thirstColor = new Color(0.78f, 0.5f, 1f);
    [SerializeField] private Color _outlineColor = new Color(0.12f, 0.08f, 0.06f, 1f);
    [Tooltip("픽셀 글꼴에도 적용되는 외곽선. 1이면 글자 크기의 10% 두께입니다.")]
    [SerializeField, Range(0f, 1f)] private float _outlineWidth = 0.2f;

    [Header("Motion")]
    [SerializeField] private Vector3 _worldOffset = new Vector3(0f, 0.7f, 0f);
    [SerializeField, Min(0.1f)] private float _duration = 0.65f;
    [Tooltip("처음 위로 튀어 오르는 속도. Canvas 단위/초입니다.")]
    [SerializeField, Min(0f)] private float _launchSpeed = 320f;
    [Tooltip("아래로 떨어지는 가속도. 커질수록 짧고 무겁게 튑니다.")]
    [SerializeField, Min(1f)] private float _gravity = 1500f;
    [Tooltip("Canvas 기준 좌우 분산 거리입니다.")]
    [SerializeField, Min(0f)] private float _horizontalSpread = 32f;
    [SerializeField, Range(1f, 2f)] private float _popScale = 1.45f;

    [Header("Pool")]
    [SerializeField, Min(1)] private int _prewarmCount = 16;
    [SerializeField, Min(1)] private int _maxVisible = 64;

    private sealed class Number
    {
        public TextMeshProUGUI Text;
        public RectTransform Rect;
        public Vector3 WorldPosition;
        public Color Color;
        public float Age;
        public float Drift;
    }

    private readonly List<Number> _active = new();
    private readonly Stack<Number> _available = new();
    private Canvas _canvas;
    private RectTransform _layer;
    private Material _material;
    private int _sequence;
    private Color _lastOutlineColor;
    private float _lastOutlineWidth;

    private static readonly Vector2[] OutlineDirections =
    {
        new(-1f, -1f), new(0f, -1f), new(1f, -1f), new(-1f, 0f),
        new(1f, 0f), new(-1f, 1f), new(0f, 1f), new(1f, 1f)
    };

    private void Awake()
    {
        _canvas = GetComponent<Canvas>();
        if (_font == null) _font = TMP_Settings.defaultFontAsset;
        if (_font == null)
        {
            Debug.LogError($"{name}: 데미지 숫자용 TMP Font Asset이 필요합니다.", this);
            enabled = false;
            return;
        }

        _duration = Mathf.Max(0.1f, _duration);
        _maxVisible = Mathf.Max(1, _maxVisible);
        _material = new Material(_font.material) { name = "Damage Numbers (Runtime)" };
        // 이 프로젝트의 폰트 아틀라스는 RASTER_HINTED라 SDF 외곽선을 사용할 수 없다.
        // 글자 메시를 8방향으로 복사해서 외곽선을 그리고, 셰이더 외곽선은 중복 적용하지 않는다.
        _material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0f);
        _lastOutlineColor = _outlineColor;
        _lastOutlineWidth = _outlineWidth;

        _layer = new GameObject("DamageNumbers", typeof(RectTransform)).GetComponent<RectTransform>();
        _layer.gameObject.layer = gameObject.layer;
        _layer.SetParent(transform, false);
        _layer.SetAsFirstSibling();
        _layer.anchorMin = Vector2.zero;
        _layer.anchorMax = Vector2.one;
        _layer.offsetMin = _layer.offsetMax = Vector2.zero;

        for (int i = 0; i < Mathf.Clamp(_prewarmCount, 0, _maxVisible); i++)
        {
            _available.Push(CreateNumber());
        }
    }

    private void OnEnable() => DamageFeedback.OnValueChanged += Show;

    private void OnDisable()
    {
        DamageFeedback.OnValueChanged -= Show;
        for (int i = _active.Count - 1; i >= 0; i--) Release(i);
    }

    private void OnDestroy()
    {
        if (_material != null) Destroy(_material);
        if (_layer != null) Destroy(_layer.gameObject);
    }

    private Number CreateNumber()
    {
        GameObject instance = new GameObject("DamageNumber", typeof(RectTransform));
        instance.SetActive(false);
        instance.layer = gameObject.layer;
        RectTransform rect = instance.GetComponent<RectTransform>();
        rect.SetParent(_layer, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(240f, 64f);

        TextMeshProUGUI text = instance.AddComponent<TextMeshProUGUI>();
        text.font = _font;
        text.fontSharedMaterial = _material;
        text.fontSize = _fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.richText = false;
        text.raycastTarget = false;
        text.OnPreRenderText += ApplyOutline;
        text.UpdateMeshPadding();
        return new Number { Text = text, Rect = rect };
    }

    private void ApplyOutline(TMP_TextInfo textInfo)
    {
        float thickness = Mathf.Clamp01(_outlineWidth) * textInfo.textComponent.fontSize * 0.1f;

        for (int m = 0; m < textInfo.materialCount; m++)
        {
            ref TMP_MeshInfo mesh = ref textInfo.meshInfo[m];
            int sourceCount = mesh.vertexCount;
            if (sourceCount == 0 || thickness <= 0f || _outlineColor.a <= 0f)
            {
                // 풀 재사용이나 외곽선 비활성화 후 이전 프레임의 복사본을 남기지 않는다.
                mesh.ClearUnusedVertices();
                continue;
            }

            int faceStart = sourceCount * OutlineDirections.Length;
            int totalCount = faceStart + sourceCount;
            if (mesh.vertices.Length < totalCount) mesh.ResizeMeshInfo(totalCount / 4);

            // 원래 글자를 마지막에 그린다. 원본 정점을 뒤로 옮기고 그 앞에 외곽선을 배치한다.
            System.Array.Copy(mesh.vertices, 0, mesh.vertices, faceStart, sourceCount);
            System.Array.Copy(mesh.uvs0, 0, mesh.uvs0, faceStart, sourceCount);
            System.Array.Copy(mesh.uvs2, 0, mesh.uvs2, faceStart, sourceCount);
            System.Array.Copy(mesh.colors32, 0, mesh.colors32, faceStart, sourceCount);

            for (int direction = 0; direction < OutlineDirections.Length; direction++)
            {
                Vector3 offset = OutlineDirections[direction] * thickness;
                for (int v = 0; v < sourceCount; v++)
                {
                    int source = faceStart + v;
                    int destination = direction * sourceCount + v;
                    mesh.vertices[destination] = mesh.vertices[source] + offset;
                    mesh.uvs0[destination] = mesh.uvs0[source];
                    mesh.uvs2[destination] = mesh.uvs2[source];
                    Color32 color = _outlineColor;
                    color.a = (byte)Mathf.RoundToInt(color.a * (mesh.colors32[source].a / 255f));
                    mesh.colors32[destination] = color;
                }
            }
            mesh.vertexCount = totalCount;
            mesh.ClearUnusedVertices();
        }
    }

    private void Show(Vector3 position, float amount, FloatingNumberKind kind)
    {
        if (_layer == null || amount == 0f || float.IsNaN(amount) || float.IsInfinity(amount)) return;
        Vector3 origin = position + _worldOffset;
        if (!TryProject(origin, out _)) return;

        // 장판 다중 타격에서도 개체 수를 제한하고, 가장 오래된 숫자부터 재사용한다.
        if (_active.Count >= _maxVisible) Release(0);
        Number number = _available.Count > 0 ? _available.Pop() : CreateNumber();
        number.WorldPosition = origin;
        number.Age = 0f;
        number.Color = kind switch
        {
            FloatingNumberKind.PlayerDamage => _playerColor,
            FloatingNumberKind.Healing => _healingColor,
            FloatingNumberKind.ThirstChange => _thirstColor,
            _ => _enemyColor
        };
        // 전투/포션 무작위 결과에 영향을 주지 않도록 UnityEngine.Random을 소비하지 않는다.
        number.Drift = Mathf.Sin(++_sequence * 2.399963f) * _horizontalSpread;
        number.Text.text = FormatAmount(amount, kind);
        number.Text.fontSize = _fontSize;
        number.Rect.SetAsLastSibling();
        number.Rect.gameObject.SetActive(true);
        _active.Add(number);
        Render(number);
    }

    private static string FormatAmount(float amount, FloatingNumberKind kind)
    {
        // .5는 0에서 멀어지는 방향으로 반올림. 실제 게임 수치는 바꾸지 않는다.
        double rounded = System.Math.Round((double)amount, 0, System.MidpointRounding.AwayFromZero);
        if (rounded == 0d) return "0";
        bool signed = kind == FloatingNumberKind.Healing || kind == FloatingNumberKind.ThirstChange;
        return (signed && rounded > 0d ? "+" : string.Empty) + rounded.ToString("0", CultureInfo.InvariantCulture);
    }

    private void LateUpdate()
    {
        // Inspector에서 플레이 중 바꾼 외곽선 설정도 다음 메시 갱신에 반영한다.
        bool outlineChanged = _outlineColor != _lastOutlineColor || _outlineWidth != _lastOutlineWidth;
        _lastOutlineColor = _outlineColor;
        _lastOutlineWidth = _outlineWidth;
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            Number number = _active[i];
            if (outlineChanged) number.Text.havePropertiesChanged = true;
            // 일시정지/히트스톱에서는 전투와 함께 멈춘다.
            number.Age += Time.deltaTime;
            if (number.Age >= _duration) Release(i);
            else Render(number);
        }
    }

    private void Render(Number number)
    {
        bool visible = TryProject(number.WorldPosition, out Vector2 origin);
        number.Text.enabled = visible;
        if (!visible) return;

        float t = Mathf.Clamp01(number.Age / Mathf.Max(0.1f, _duration));
        // 투사체 운동: 짧게 솟은 뒤 정점을 지나 가속하며 떨어진다.
        float vertical = _launchSpeed * number.Age - 0.5f * _gravity * number.Age * number.Age;
        number.Rect.anchoredPosition = origin + new Vector2(number.Drift * (0.25f + t), vertical);
        float scale = number.Age < 0.055f
            ? Mathf.Lerp(0.85f, _popScale, number.Age / 0.055f)
            : Mathf.Lerp(_popScale, 1f, Mathf.Clamp01((number.Age - 0.055f) / 0.105f));
        number.Rect.localScale = Vector3.one * scale;
        Color color = number.Color;
        color.a *= 1f - Mathf.InverseLerp(0.65f, 1f, t);
        number.Text.color = color;
    }

    private bool TryProject(Vector3 position, out Vector2 localPosition)
    {
        localPosition = default;
        if (_worldCamera == null) _worldCamera = Camera.main;
        if (_worldCamera == null) return false;

        Vector3 screen = _worldCamera.WorldToScreenPoint(position);
        if (screen.z <= 0f || !_worldCamera.pixelRect.Contains(new Vector2(screen.x, screen.y))) return false;

        Camera uiCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(_layer, screen, uiCamera, out localPosition);
    }

    private void Release(int index)
    {
        Number number = _active[index];
        _active.RemoveAt(index);
        number.Rect.gameObject.SetActive(false);
        _available.Push(number);
    }
}
