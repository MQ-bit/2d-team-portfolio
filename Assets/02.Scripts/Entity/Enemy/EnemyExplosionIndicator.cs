using UnityEngine;

// 폭발형 적이 예열(충전)하는 동안 바닥에 폭발 범위를 표시한다.
// 디자인은 원거리 리퍼의 포션 낙하 경고 장판(ReaperAttackIndicator.SetPotionArea)을 따른다: 어두운 붉은 장판 + 테두리,
// 안쪽 원이 중앙에서 커지는 붉은 충전 연출. 가득 차면 터진다. 문양/마커는 없다.
// 장판 원은 실제 피해 반경과 같고, 표시는 적의 자식이 아니라 별도 오브젝트라 적의 스케일에 영향을 받지 않는다.
public class EnemyExplosionIndicator : MonoBehaviour
{
    private const int Segments = 48;

    [Header("References")]
    [SerializeField] private ExplodingEnemyController _controller;

    [Header("Area")]
    [Tooltip("장판으로 쓸 원형 스프라이트. (UI/Indicator/PotionAoE)")]
    [SerializeField] private Sprite _areaSprite;
    [Tooltip("장판 색. 알파는 충전될수록 아래 두 값 사이로 짙어진다.")]
    [SerializeField] private Color _areaColor = new Color(0.38f, 0.11f, 0.13f, 1f);
    [SerializeField, Range(0f, 1f)] private float _areaAlphaStart = 0.18f;
    [SerializeField, Range(0f, 1f)] private float _areaAlphaEnd = 0.3f;

    [Header("Border")]
    [SerializeField, Min(0.005f)] private float _outlineWidth = 0.045f;
    [SerializeField] private Color _shadowColor = new Color(0.08f, 0.045f, 0.05f, 0.75f);
    [SerializeField] private Color _outlineColor = new Color(0.56f, 0.2f, 0.17f, 0.8f);
    [SerializeField] private Color _outlineUrgentColor = new Color(0.7f, 0.28f, 0.23f, 0.95f);

    [Header("Charge (중앙에서 차오르는 붉은색)")]
    [SerializeField] private Color _fillColor = new Color(0.5f, 0.16f, 0.15f, 0.3f);
    [SerializeField, Range(0f, 1f)] private float _fillAlphaUrgentBonus = 0.1f;
    [SerializeField] private Color _fillEdgeColor = new Color(0.76f, 0.3f, 0.24f, 0.85f);
    [SerializeField] private Color _fillEdgeUrgentColor = new Color(0.88f, 0.42f, 0.33f, 1f);

    [Header("Sorting")]
    [SerializeField] private string _sortingLayer = "FieldUI";
    [SerializeField] private int _sortingOrder = 0;

    private GameObject _root;
    private SpriteRenderer _area;
    private SpriteRenderer _fill;
    private LineRenderer _shadow;
    private LineRenderer _outline;
    private LineRenderer _fillEdge;
    private Material _lineMaterial;
    private readonly Vector3[] _boundary = new Vector3[Segments];
    private readonly Vector3[] _fillBoundary = new Vector3[Segments];

    private void Awake()
    {
        if (_controller == null)
        {
            _controller = GetComponent<ExplodingEnemyController>();
        }
        if (_controller == null || _areaSprite == null)
        {
            Debug.LogError($"{gameObject.name} : 폭발 범위 표시에 ExplodingEnemyController 또는 장판 스프라이트가 할당 안됨");
            enabled = false;
            return;
        }

        CreateVisual();
    }

    private void CreateVisual()
    {
        _root = new GameObject("ExplosionIndicator");
        _lineMaterial = new Material(Shader.Find("Sprites/Default"));

        _area = CreateSprite("Area", _areaSprite, _sortingOrder);
        _fill = CreateSprite("Charge Fill", _areaSprite, _sortingOrder + 1);
        _shadow = CreateLine("Dark Border", _sortingOrder + 2);
        _outline = CreateLine("Border", _sortingOrder + 3);
        _fillEdge = CreateLine("Charge Edge", _sortingOrder + 4);

        _root.SetActive(false);
    }

    private SpriteRenderer CreateSprite(string objectName, Sprite sprite, int order)
    {
        GameObject obj = new GameObject(objectName);
        obj.transform.SetParent(_root.transform, false);
        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = _sortingLayer;
        renderer.sortingOrder = order;
        return renderer;
    }

    private LineRenderer CreateLine(string objectName, int order)
    {
        GameObject obj = new GameObject(objectName);
        obj.transform.SetParent(_root.transform, false);
        LineRenderer line = obj.AddComponent<LineRenderer>();
        line.sharedMaterial = _lineMaterial;
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = Segments;
        line.sortingLayerName = _sortingLayer;
        line.sortingOrder = order;
        return line;
    }

    private void LateUpdate()
    {
        if (_root == null)
        {
            return;
        }

        bool show = _controller.IsFusing && _controller.ExplosionRadius > 0f;
        if (_root.activeSelf != show)
        {
            _root.SetActive(show);
        }
        if (!show)
        {
            return;
        }

        float radius = _controller.ExplosionRadius;
        float progress = _controller.FuseProgress;
        // 막바지(70%~)에 깜빡이며 임박을 알린다.
        float pulse = 0.5f + 0.5f * Mathf.Sin(progress * Mathf.PI * (4f + progress * 8f));
        float urgency = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.7f, 1f, progress));

        _root.transform.position = transform.position;

        // 장판: 피해 반경과 같은 원.
        float areaScale = radius * 2f / Mathf.Max(0.001f, _areaSprite.bounds.size.x);
        _area.transform.localScale = new Vector3(areaScale, areaScale, 1f);
        Color area = _areaColor;
        area.a = Mathf.Lerp(_areaAlphaStart, _areaAlphaEnd, progress) + urgency * pulse * 0.05f;
        _area.color = area;

        // 테두리: 고정. 임박하면 밝아지고 두꺼워진다.
        for (int i = 0; i < Segments; i++)
        {
            float angle = i * Mathf.PI * 2f / Segments;
            _boundary[i] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            _fillBoundary[i] = _boundary[i] * progress;
        }
        _shadow.SetPositions(_boundary);
        _shadow.widthMultiplier = _outlineWidth * 3.2f;
        _shadow.startColor = _shadow.endColor = _shadowColor;
        _outline.SetPositions(_boundary);
        _outline.widthMultiplier = _outlineWidth * (1.15f + urgency * pulse * 0.2f);
        _outline.startColor = _outline.endColor = Color.Lerp(_outlineColor, _outlineUrgentColor, urgency * (0.6f + pulse * 0.4f));

        // 충전: 중앙에서 시작한 붉은 원이 경계까지 커진다.
        float fillScale = areaScale * progress;
        _fill.transform.localScale = new Vector3(fillScale, fillScale, 1f);
        Color fill = _fillColor;
        fill.a += urgency * _fillAlphaUrgentBonus;
        _fill.color = fill;
        _fill.enabled = progress > 0.01f;
        _fillEdge.enabled = progress > 0.01f;
        _fillEdge.SetPositions(_fillBoundary);
        _fillEdge.widthMultiplier = _outlineWidth * (1f + urgency * pulse * 0.2f);
        _fillEdge.startColor = _fillEdge.endColor = Color.Lerp(_fillEdgeColor, _fillEdgeUrgentColor, urgency * pulse);
    }

    private void OnDisable()
    {
        // 풀로 반환되거나 죽으면 표시를 끈다.
        if (_root != null)
        {
            _root.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (_root != null)
        {
            Destroy(_root);
        }
        if (_lineMaterial != null)
        {
            Destroy(_lineMaterial);
        }
    }
}
