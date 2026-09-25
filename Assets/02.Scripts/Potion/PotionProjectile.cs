using UnityEngine;

public class PotionProjectile : MonoBehaviour
{
    [SerializeField] private Transform _visual;
    [SerializeField] private PotionBreakEffect _breakEffectPrefab;
    [SerializeField] private PotionGroundArea _groundAreaPrefab;

    private Vector3 _startPosition;
    private Vector3 _targetPosition;
    private float _flightDuration;
    private float _rotationSpeed;
    private float _elapsedTime;
    private float _verticalVelocity;
    private float _gravity;
    private PotionData _potionData;

    private void Awake()
    {
        enabled = false;
    }

    private void Update()
    {
        _elapsedTime = Mathf.Min(_elapsedTime + Time.deltaTime, _flightDuration);
        float progress = _elapsedTime / _flightDuration;
        float virtualHeight = Mathf.Max(0f, _verticalVelocity * _elapsedTime - 0.5f * _gravity * _elapsedTime * _elapsedTime);

        transform.position = Vector3.Lerp(_startPosition, _targetPosition, progress);
        if (_visual != null)
        {
            _visual.localPosition = Vector3.up * virtualHeight;
            _visual.Rotate(0f, 0f, _rotationSpeed * Time.deltaTime);
        }

        if (_elapsedTime >= _flightDuration)
        {
            Land();
        }
    }

    public void Launch(PotionData potionData, Vector3 targetPosition, float flightDuration, float arcHeight, float rotationSpeed)
    {
        _potionData = potionData;
        _startPosition = transform.position;
        _targetPosition = targetPosition;
        _flightDuration = Mathf.Max(0.01f, flightDuration);
        _rotationSpeed = rotationSpeed;
        _elapsedTime = 0f;

        float height = Mathf.Max(0f, arcHeight);
        _gravity = 8f * height / (_flightDuration * _flightDuration);
        _verticalVelocity = 0.5f * _gravity * _flightDuration;

        if (_visual != null && _potionData != null && _potionData.VisualData.PotionSprite != null)
        {
            SpriteRenderer visualRenderer = _visual.GetComponent<SpriteRenderer>();
            if (visualRenderer != null)
            {
                visualRenderer.sprite = _potionData.VisualData.PotionSprite;
            }
        }

        enabled = true;
    }

    private void Land()
    {
        transform.position = _targetPosition;

        if (_breakEffectPrefab != null)
        {
            PotionBreakEffect breakEffect = Instantiate(_breakEffectPrefab, _targetPosition, Quaternion.identity);
            breakEffect.Play();
        }

        if (_groundAreaPrefab != null)
        {
            PotionGroundArea groundArea = Instantiate(_groundAreaPrefab, _targetPosition, Quaternion.identity);
            groundArea.Initialize(_potionData);
            groundArea.Activate();
        }

        Destroy(gameObject);
    }
}
