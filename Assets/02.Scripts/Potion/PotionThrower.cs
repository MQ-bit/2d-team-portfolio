using UnityEngine;
using UnityEngine.InputSystem;

public class PotionThrower : MonoBehaviour
{
    [Header("Throw Curve Settings")]
    [SerializeField] private PotionProjectile _potionPrefab;
    [SerializeField] private Transform _throwPoint;
    [SerializeField] private Camera _worldCamera;
    [SerializeField, Min(0.01f)] private float _maxThrowDistance = 5f;
    [SerializeField, Min(0.01f)] private float _throwSpeed = 8f;
    [SerializeField, Min(0f)] private float _arcHeight = 2f;
    [SerializeField] private float _rotationSpeed = 360f;

    private Vector3 SpawnPosition => _throwPoint != null ? _throwPoint.position : transform.position;

    private void Awake()
    {
        if (_worldCamera == null)
        {
            _worldCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && TryGetCurrentThrowPath(out Vector3 start, out Vector3 end))
        {
            SpawnProjectile(start, end);
        }
    }

    public PotionProjectile Throw(Vector3 aimPosition)
    {
        Vector3 start = SpawnPosition;
        return TryGetLandingPosition(start, aimPosition, out Vector3 end)
            ? SpawnProjectile(start, end) : null;
    }

    public bool TryGetCurrentThrowPath(out Vector3 spawnPosition, out Vector3 landingPosition)
    {
        spawnPosition = SpawnPosition;
        landingPosition = spawnPosition;

        if (_worldCamera == null || Mouse.current == null)
        {
            return false;
        }

        Vector3 screenPosition = Mouse.current.position.ReadValue();
        screenPosition.z = Mathf.Abs(_worldCamera.transform.position.z - spawnPosition.z);
        Vector3 aimPosition = _worldCamera.ScreenToWorldPoint(screenPosition);
        return TryGetLandingPosition(spawnPosition, aimPosition, out landingPosition);
    }

    private PotionProjectile SpawnProjectile(Vector3 spawnPosition, Vector3 landingPosition)
    {
        if (_potionPrefab == null)
        {
            Debug.LogError("Potion prefab is not assigned.", this);
            return null;
        }

        float throwDistance = Vector2.Distance(spawnPosition, landingPosition);

        PotionProjectile projectile = Instantiate(_potionPrefab, spawnPosition, Quaternion.identity);
        projectile.Launch(landingPosition, throwDistance / _throwSpeed, GetArcHeight(spawnPosition, landingPosition), _rotationSpeed);
        return projectile;
    }

    public float GetArcHeight(Vector3 startPosition, Vector3 landingPosition)
    {
        float distance = Vector2.Distance(startPosition, landingPosition);
        return _arcHeight * Mathf.Clamp01(distance / _maxThrowDistance);
    }

    private bool TryGetLandingPosition(Vector3 spawnPosition, Vector3 aimPosition, out Vector3 landingPosition)
    {
        Vector2 direction = aimPosition - spawnPosition;
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            landingPosition = spawnPosition;
            return false;
        }

        landingPosition = spawnPosition + (Vector3)Vector2.ClampMagnitude(direction, _maxThrowDistance);
        return true;
    }
}
