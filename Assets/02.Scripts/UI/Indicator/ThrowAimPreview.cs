using UnityEngine;

public class ThrowAimPreview : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject _owner;
    [SerializeField] private LineRenderer _lineRenderer;
    [SerializeField] private Transform _aoeIndicator;
    [SerializeField] private Transform _indicator;

    [Header("Settings")]
    [SerializeField] private int _trajectoryPointCount = 24;

    private SpriteRenderer _ownerRenderer;
    private SpriteRenderer _indicatorRenderer;
    private SpriteRenderer _aoeRenderer;

    private void Awake()
    {
        if (_lineRenderer == null)
        {
            _lineRenderer = GetComponent<LineRenderer>();
        }
        if (_aoeIndicator == null)
        {
            _aoeIndicator = transform.Find("AOEIndicator");
        }

        if (_indicator == null)
        {
            Debug.LogError($"{gameObject.name} : Indicator Transform이 할당되지 않음");
        }
        _indicatorRenderer = _indicator.GetComponent<SpriteRenderer>();
        if (_aoeIndicator != null)
        {
            _aoeRenderer = _aoeIndicator.GetComponent<SpriteRenderer>();
        }
    }

    private void LateUpdate()
    {
        if (_owner != null)
        {
            ReorderSortingLayer();
        }
    }

    public void SetOwner(GameObject owner)
    {
        _owner = owner;
        _ownerRenderer = _owner.GetComponent<SpriteRenderer>();
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Draw(Vector3 startPosition, Vector3 targetPosition, float arcHeight, PotionData potionData)
    {
        if (_lineRenderer == null || _indicator == null || potionData == null)
        {
            return;
        }

        _indicator.position = targetPosition;
        _aoeIndicator.position = targetPosition;
        UpdateAoeIndicator(potionData.VisualData.AreaColor, potionData.Radius);
        RedrawLine(startPosition, targetPosition, arcHeight);
    }

    public void UpdateAoeIndicator(Color color, float radius)
    {
        _aoeRenderer.color = color;
        _aoeIndicator.localScale = new Vector3(radius * 2f, radius, 1f);
    }

    private void RedrawLine(Vector3 startPosition, Vector3 targetPosition, float arcHeight)
    {
        _lineRenderer.positionCount = _trajectoryPointCount;

        for (int i = 0; i < _trajectoryPointCount; i++)
        {
            float t = i / (float)(_trajectoryPointCount - 1);
            Vector3 point = ThrowTrajectory.EvaluateVisualPoint(startPosition, targetPosition, arcHeight, t);

            _lineRenderer.SetPosition(i, point);
        }
    }

    private void ReorderSortingLayer()
    {
        int orderModifier = (_indicator.position.y > _owner.transform.position.y) ? -1 : 1;

        _indicatorRenderer.sortingLayerID = _ownerRenderer.sortingLayerID;
        _indicatorRenderer.sortingOrder = _ownerRenderer.sortingOrder + orderModifier;

        _lineRenderer.sortingLayerID = _ownerRenderer.sortingLayerID;
        _lineRenderer.sortingOrder = _ownerRenderer.sortingOrder + orderModifier;
    }
}
