using UnityEngine;

public class ThrowAimPreview : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject _owner;
    [SerializeField] private LineRenderer _lineRenderer;
    [SerializeField] private Transform _aoeIndicator;
    [SerializeField] private Transform _indicator;

    [Header("Settings")]
    [SerializeField, Min(2)] private int _trajectoryPointCount = 24;

    private SpriteRenderer _ownerRenderer;
    private SpriteRenderer _indicatorRenderer;
    private SpriteRenderer _aoeRenderer;
    private float _flightProgress = 1f;

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
            //ReorderSortingLayer();
        }
    }

    public void SetOwner(GameObject owner)
    {
        _owner = owner;
        _ownerRenderer = _owner.GetComponentInChildren<SpriteRenderer>();
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Draw(Vector3 startPosition, Vector3 targetPosition, float arcHeight, Color areaColor, float areaWidth)
    {
        Draw(startPosition, targetPosition, arcHeight, areaColor, areaWidth, 1f);
    }

    public void Draw(Vector3 startPosition, Vector3 targetPosition, float arcHeight, Color areaColor,
        float areaWidth, float flightProgress)
    {
        float progress = Mathf.Clamp01(flightProgress);
        Vector3 ground = ThrowTrajectory.EvaluateGroundPoint(startPosition, targetPosition, progress);
        Vector3 impact = progress < 1f
            ? ThrowTrajectory.EvaluateVisualPoint(startPosition, targetPosition, arcHeight, progress) : ground;
        Draw(startPosition, targetPosition, arcHeight, areaColor, areaWidth,
            new PotionThrowPrediction(progress < 1f, progress, impact, ground));
    }

    public void Draw(Vector3 startPosition, Vector3 targetPosition, float arcHeight, Color areaColor,
        float areaWidth, PotionThrowPrediction prediction)
    {
        if (_lineRenderer == null || _indicator == null)
        {
            return;
        }

        _flightProgress = Mathf.Clamp01(prediction.FlightProgress);
        _indicator.position = prediction.ImpactPosition;
        _aoeIndicator.position = prediction.AreaPosition;
        _aoeIndicator.gameObject.SetActive(prediction.CanCreateArea);
        UpdateAoeIndicator(areaColor, areaWidth);

        _lineRenderer.transform.position = prediction.ImpactPosition;
        RedrawLine(startPosition, targetPosition, arcHeight);
        _lineRenderer.SetPosition(_lineRenderer.positionCount - 1, prediction.ImpactPosition);
    }

    public void UpdateAoeIndicator(Color color, float areaWidth)
    {
        if (_aoeRenderer == null || _aoeIndicator == null) return;
        _aoeRenderer.color = color;
        if (_aoeRenderer.sprite == null) return;

        // areaWidth is a world-space diameter, not a multiplier for the source sprite.
        Vector2 spriteSize = _aoeRenderer.sprite.bounds.size;
        Vector3 parentScale = _aoeIndicator.parent != null
            ? _aoeIndicator.parent.lossyScale : Vector3.one;
        float width = Mathf.Max(0f, areaWidth);
        _aoeIndicator.localScale = new Vector3(
            width / Mathf.Max(0.001f, spriteSize.x * Mathf.Abs(parentScale.x)),
            width * 0.5f / Mathf.Max(0.001f, spriteSize.y * Mathf.Abs(parentScale.y)), 1f);
    }

    private void RedrawLine(Vector3 startPosition, Vector3 targetPosition, float arcHeight)
    {
        int pointCount = Mathf.Max(2, _trajectoryPointCount);
        _lineRenderer.positionCount = pointCount;

        for (int i = 0; i < pointCount; i++)
        {
            float t = i / (float)(pointCount - 1) * _flightProgress;
            Vector3 point = ThrowTrajectory.EvaluateVisualPoint(startPosition, targetPosition, arcHeight, t);

            _lineRenderer.SetPosition(i, point);
        }
    }

    // private void ReorderSortingLayer()
    // {
    //     int orderModifier = (_indicator.position.y > _owner.transform.position.y) ? -1 : 1;

    //     _indicatorRenderer.sortingLayerID = _ownerRenderer.sortingLayerID;
    //     _indicatorRenderer.sortingOrder = _ownerRenderer.sortingOrder + orderModifier;

    //     _lineRenderer.sortingLayerID = _ownerRenderer.sortingLayerID;
    //     _lineRenderer.sortingOrder = _ownerRenderer.sortingOrder + orderModifier;
    // }
}
