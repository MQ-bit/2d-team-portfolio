using System.Collections;
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
    [SerializeField] private float _flightDuration = 0.5f;
    [SerializeField] private float _arcHeight = 1.5f;

    [Header("Potion Prefab")]
    [SerializeField] private GameObject _potionPrefab;

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
        Ray ray = _worldCamera.ScreenPointToRay(screenPosition);

        float groundZ = _throwPoint.position.z;
        Plane groundPlane = new Plane( Vector3.forward, new Vector3(0f, 0f, groundZ) );

        if (groundPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            return new Vector2(hitPoint.x, hitPoint.y);
        }

        return _throwPoint.position;
    }

    private Vector2 ClampToThrowRange(Vector2 desiredPosition)
    {
        Vector2 origin = _throwPoint.position;
        Vector2 offset = desiredPosition - origin;
        float maxThrowDistance =  _playerStatus.MaxThrowDistance;

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
        //TODO : 포션 로직 오면 여기에 생성시키는 로직 추가
    }
    private void HideAimPreview()
    {
        _aimPreviewInstance.Hide();
    }

 
}