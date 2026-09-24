using UnityEngine;
using UnityEngine.InputSystem;

public class PotionThrower : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PotionProjectile _potionPrefab;
    [SerializeField] private Transform _throwPoint;
    [SerializeField] private Camera _worldCamera;

    [Header("Throw Settings")]
    [SerializeField, Min(0.01f)] private float _maxThrowDistance = 5f;
    [SerializeField, Min(0.01f)] private float _throwSpeed = 8f;
    [SerializeField, Min(0f)] private float _arcHeight = 2f;
    [SerializeField] private float _rotationSpeed = 360f;

    private bool _isAiming;

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
        HandleMouseInput();
    }

    private void OnDisable()
    {
        _isAiming = false;
    }

    private void HandleMouseInput()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            _isAiming = false;
            return;
        }

        if (mouse.leftButton.wasPressedThisFrame)
        {
            _isAiming = true;
        }

        if (!_isAiming || !mouse.leftButton.wasReleasedThisFrame)
        {
            return;
        }

        _isAiming = false;

        if (TryGetMouseWorldPosition(mouse, out Vector3 aimPosition))
        {
            Throw(aimPosition);
        }
    }

    public PotionProjectile Throw(Vector3 aimPosition)
    {
        Vector3 spawnPosition = SpawnPosition;
        return TryGetLandingPosition(spawnPosition, aimPosition, out Vector3 landingPosition)
            ? SpawnProjectile(spawnPosition, landingPosition)
            : null;
    }

    public bool TryGetCurrentThrowPath(out Vector3 spawnPosition, out Vector3 landingPosition)
    {
        spawnPosition = SpawnPosition;
        landingPosition = spawnPosition;

        Mouse mouse = Mouse.current;
        if (!_isAiming || mouse == null || !TryGetMouseWorldPosition(mouse, out Vector3 aimPosition))
        {
            return false;
        }

        return TryGetLandingPosition(spawnPosition, aimPosition, out landingPosition);
    }

    private bool TryGetMouseWorldPosition(Mouse mouse, out Vector3 worldPosition)
    {
        worldPosition = SpawnPosition;
        if (_worldCamera == null)
        {
            return false;
        }

        Vector3 screenPosition = mouse.position.ReadValue();
        screenPosition.z = Mathf.Abs(_worldCamera.transform.position.z - SpawnPosition.z);
        worldPosition = _worldCamera.ScreenToWorldPoint(screenPosition);
        return true;
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
