using UnityEngine;

public class ThrowAimPreview : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LineRenderer _lineRenderer;
    [SerializeField] private Transform _indicator;

    [Header("Settings")]
    [SerializeField] private int _trajectoryPointCount = 24;

    private void Awake()
    {
        if (_lineRenderer == null)
        {
            _lineRenderer = GetComponent<LineRenderer>();
        }

        if (_indicator == null)
        {
            Debug.LogError($"{gameObject.name} : Indicator Transform이 할당되지 않음");
        }
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

        _lineRenderer.positionCount =
            _trajectoryPointCount;

        for (int i = 0; i < _trajectoryPointCount; i++)
        {
            float t = i / (float)(_trajectoryPointCount - 1);

            Vector3 point = ThrowTrajectory.EvaluateVisualPoint( startPosition, targetPosition, arcHeight, t);

            _lineRenderer.SetPosition(i, point);
        }
    }
}