using UnityEngine;

public class PotionProjectile : MonoBehaviour
{
    [SerializeField] private Transform _visual;
    [SerializeField] private Transform _shadow;

    private Vector3 _startPosition;
    private Vector3 _targetPosition;
    private float _flightDuration;
    private float _rotationSpeed;
    private float _elapsedTime;
    private float _verticalVelocity;
    private float _gravity;

    private void Awake()
    {
        enabled = false;
    }

    private void Update()
    {
        _elapsedTime += Time.deltaTime;
        float flightTime = Mathf.Min(_elapsedTime, _flightDuration);
        float progress = flightTime / _flightDuration;
        float virtualHeight = Mathf.Max(0f, _verticalVelocity * flightTime - 0.5f * _gravity * flightTime * flightTime);

        transform.position = Vector3.Lerp(_startPosition, _targetPosition, progress);
        if (_visual != null)
        {
            _visual.localPosition = Vector3.up * virtualHeight;
            _visual.Rotate(0f, 0f, _rotationSpeed * Time.deltaTime);
        }

        if (_elapsedTime >= _flightDuration && virtualHeight <= 0.001f)
        {
            Land();
        }
    }

    public void Launch(Vector3 targetPosition, float flightDuration, float arcHeight, float rotationSpeed)
    {
        _startPosition = transform.position;
        _targetPosition = targetPosition;
        _flightDuration = Mathf.Max(0.01f, flightDuration);
        _rotationSpeed = rotationSpeed;
        _elapsedTime = 0f;

        float height = Mathf.Max(0f, arcHeight);
        _gravity = 8f * height / (_flightDuration * _flightDuration);
        _verticalVelocity = 0.5f * _gravity * _flightDuration;

        if (_shadow != null)
        {
            _shadow.localPosition = Vector3.zero;
        }

        enabled = true;
    }

    public static Vector3 EvaluateVisualPosition(
        Vector3 startPosition,
        Vector3 targetPosition,
        float arcHeight,
        float progress)
    {
        float height = 4f * arcHeight * progress * (1f - progress);
        return Vector3.Lerp(startPosition, targetPosition, progress)
            + Vector3.up * height;
    }

    private void Land()
    {
        transform.position = _targetPosition;
        Destroy(gameObject);
    }
}
