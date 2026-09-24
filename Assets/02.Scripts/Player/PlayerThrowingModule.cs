using UnityEngine;

public class PlayerThrowingModule : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private Camera _worldCamera;
    [SerializeField] private Transform _throwPoint;
    [SerializeField] private PlayerStatus _playerStatus;

    [Header("Throw Settings")]
    [SerializeField] private ThrowAimPreview _aimPreviewPrefab;
    private ThrowAimPreview _aimPreviewInstance;
    [SerializeField] private float _arcHeight = 1.5f;
    [SerializeField, Min(0.01f)] private float _throwSpeed = 8f;
    [SerializeField] private float _rotationSpeed = 360f;

    [Header("Potion Prefab")]
    [SerializeField] private PotionProjectile _potionPrefab;

    private Vector2 _aimWorldPosition;
    public bool IsAiming { get; private set; }
    public Vector2 AimWorldPosition => _aimWorldPosition;

    private void Awake()
    {
        InitiateComponent();
        InitiateAimPreview();
    }

    private void InitiateAimPreview()
    {
        if (_worldCamera == null)
        {
            _worldCamera = Camera.main;
        }

        if (_throwPoint == null)
        {
            _throwPoint = transform;
        }

        _aimPreviewInstance = Instantiate(_aimPreviewPrefab, transform);
        _aimPreviewInstance.SetOwner(gameObject);
        _aimPreviewInstance.Hide();
    }

    private void InitiateComponent()
    {
        if (_playerStatus == null)
        {
            _playerStatus = GetComponent<PlayerStatus>();
        }
    }

    public void BeginAim(Vector2 screenPosition)
    {
        IsAiming = true;
        _aimPreviewInstance.Show();

        UpdateAim(screenPosition);
    }

    public void UpdateAim(Vector2 screenPosition)
    {
        if (!IsAiming || _worldCamera == null)
        {
            return;
        }

        Vector2 desiredPosition = ScreenToWorldPosition(screenPosition);

        _aimWorldPosition = ClampToThrowRange(desiredPosition);

        Vector3 startPosition = _throwPoint.position;
        Vector3 targetPosition = new Vector3(_aimWorldPosition.x, _aimWorldPosition.y, startPosition.z);

        _aimPreviewInstance.Draw(startPosition, targetPosition, _arcHeight);
    }

    public void ReleaseThrow(Vector2 screenPosition)
    {
        if (!IsAiming)
        {
            return;
        }

        UpdateAim(screenPosition);
        IsAiming = false;

        ThrowPotion(_aimWorldPosition);
        HideAimPreview();
    }

    public void CancelAim()
    {
        IsAiming = false;

        HideAimPreview();
    }

    private Vector2 ScreenToWorldPosition(Vector2 screenPosition)
    {
        float depth = Mathf.Abs(_throwPoint.position.z - _worldCamera.transform.position.z);
        Vector3 worldPosition = _worldCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, depth));

        return worldPosition;
    }

    private Vector2 ClampToThrowRange(Vector2 desiredPosition)
    {
        Vector2 origin = _throwPoint.position;
        Vector2 offset = desiredPosition - origin;
        float maxThrowDistance = _playerStatus.MaxThrowDistance;

        if (offset.sqrMagnitude <= maxThrowDistance * maxThrowDistance)
        {
            return desiredPosition;
        }

        return origin + offset.normalized * maxThrowDistance;
    }

    private void ThrowPotion(Vector2 targetPosition)
    {
        if (_potionPrefab == null)
        {
            Debug.LogError("Potion prefab is not assigned.");
            return;
        }

        Vector3 startPosition = _throwPoint.position;
        Vector3 landingPosition = new Vector3(targetPosition.x, targetPosition.y, startPosition.z);
        float throwDistance = Vector2.Distance(startPosition, landingPosition);
        float flightDuration = throwDistance / _throwSpeed;

        PotionProjectile projectile = Instantiate(_potionPrefab, startPosition, Quaternion.identity);
        projectile.Launch(landingPosition, flightDuration, _arcHeight, _rotationSpeed);
    }
    private void HideAimPreview()
    {
        _aimPreviewInstance.Hide();
    }
}
