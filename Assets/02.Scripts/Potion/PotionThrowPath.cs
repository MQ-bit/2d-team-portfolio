using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class PotionThrowPath : MonoBehaviour
{
    [SerializeField] private PotionThrower _potionThrower;
    [SerializeField, Range(2, 100)] private int _pointCount = 30;

    private LineRenderer _lineRenderer;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();

        if (_potionThrower == null)
        {
            _potionThrower = GetComponent<PotionThrower>();
        }
    }

    private void LateUpdate()
    {
        if (_potionThrower == null || !_potionThrower.TryGetCurrentThrowPath(out Vector3 startPosition, out Vector3 landingPosition))
        {
            _lineRenderer.positionCount = 0;
            return;
        }

        DrawPath(startPosition, landingPosition);
    }

    private void DrawPath(Vector3 startPosition, Vector3 landingPosition)
    {
        _lineRenderer.positionCount = _pointCount;
        float arcHeight = _potionThrower.GetArcHeight(startPosition, landingPosition);

        for (int i = 0; i < _pointCount; i++)
        {
            float progress = i / (float)(_pointCount - 1);
            Vector3 position = PotionProjectile.EvaluateVisualPosition(startPosition, landingPosition, arcHeight, progress);

            _lineRenderer.SetPosition(i, position);
        }
    }

}
