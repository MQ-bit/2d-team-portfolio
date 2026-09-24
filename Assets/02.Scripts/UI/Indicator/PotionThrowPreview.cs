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

    private SpriteRenderer _ownerSpriteRenderer;
    private SpriteRenderer _indicatorSpriteRenderer;

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
        _indicatorSpriteRenderer = _indicator.GetComponent<SpriteRenderer>();
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
        _ownerSpriteRenderer = _owner.GetComponent<SpriteRenderer>();
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Draw(Vector3 startPosition, Vector3 targetPosition, float arcHeight)
    {
        if (_lineRenderer == null || _indicator == null)
        {
            return;
        }

        _indicator.position = targetPosition;
        _aoeIndicator.position = targetPosition;
        RedrawLine(startPosition, targetPosition, arcHeight);
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

        _indicatorSpriteRenderer.sortingLayerID = _ownerSpriteRenderer.sortingLayerID;
        _indicatorSpriteRenderer.sortingOrder = _ownerSpriteRenderer.sortingOrder + orderModifier;

        _lineRenderer.sortingLayerID = _ownerSpriteRenderer.sortingLayerID;
        _lineRenderer.sortingOrder = _ownerSpriteRenderer.sortingOrder + orderModifier;
    }
}