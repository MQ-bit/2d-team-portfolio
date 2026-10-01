using UnityEngine;

public class PlayerThrowingModule : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private Camera _worldCamera;
    [SerializeField] private Transform _throwPoint;
    [SerializeField] private PlayerStatus _status;
    [SerializeField] private PlayerInventoryModule _inventoryModule;

    [Header("Potion Catalog Setting")]
    [SerializeField] private PotionRunCatalog _potionRunCatalog;
    [SerializeField] private Color _defaultAreaColor = Color.white;

    [Header("Throw Settings")]
    [SerializeField] private ThrowAimPreview _aimPreviewPrefab;
    private ThrowAimPreview _aimPreviewInstance;
    [SerializeField] private float _arcHeight = 1.5f;
    [SerializeField] private float _throwDuration = 8f;
    [SerializeField] private float _rotationSpeed = 360f;

    [Header("Potion Prefab")]
    [SerializeField] private PotionProjectile _potionPrefab;

    private Vector2 _aimWorldPosition;
    private IDamageable _potionEffectOwner;

    public bool IsAiming { get; private set; }


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
        if (_status == null)
        {
            _status = GetComponent<PlayerStatus>();
        }

        if (_inventoryModule == null)
        {
            _inventoryModule = GetComponent<PlayerInventoryModule>();
        }

        _potionEffectOwner = GetComponent<IDamageable>();
    }

    private void Start()
    {
        if (_potionRunCatalog == null)
        {
            _potionRunCatalog = FindAnyObjectByType<PotionRunCatalog>();
        }
    }

    public void BeginAim(Vector2 screenPosition)
    {
        if (_inventoryModule.SelectedPotion == null) return;

        IsAiming = true;
        _aimPreviewInstance.Show();

        UpdateAim(screenPosition);
    }

    public void UpdateAim(Vector2 screenPosition)
    {
        if (!IsAiming || _worldCamera == null || _potionRunCatalog == null)
        {
            return;
        }

        PotionItem selectedPotionItem = _inventoryModule.SelectedPotionItem;

        if (selectedPotionItem.IsEmpty)
        {
            CancelAim();
            return;
        }

        Vector2 desiredPosition = ScreenToWorldPosition(screenPosition);

        _aimWorldPosition = ClampToThrowRange(desiredPosition);

        Vector3 startPosition = _throwPoint.position;
        Vector3 targetPosition = new Vector3(_aimWorldPosition.x, _aimWorldPosition.y, startPosition.z);
        PotionThrowPrediction prediction = _potionPrefab != null
            ? _potionPrefab.PredictThrow(startPosition, targetPosition, _arcHeight)
            : new PotionThrowPrediction(false, 1f, targetPosition, targetPosition);

        PotionData selectedPotion = selectedPotionItem.PotionData;
        bool identified = _potionRunCatalog.IsIdentified(selectedPotion);

        Color previewColor = identified ? selectedPotion.VisualData.AreaColor : _defaultAreaColor;
        // Identification changes the displayed color, never the actual effect range.
        float previewWidth = selectedPotion.AreaWidth;

        _aimPreviewInstance.Draw(startPosition, targetPosition, _arcHeight, previewColor, previewWidth,
            prediction);
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
        if (!IsAiming) return;

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
        float maxThrowDistance = _status.MaxThrowDistance;

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

        if (_potionRunCatalog == null)
        {
            Debug.LogError("Potion Catalog is not assgined.");
            return;
        }

        if (_inventoryModule.TryConsumeSelectedItem(out PotionItem consumedPotion))
        {
            Vector3 startPosition = _throwPoint.position;
            float distance = Vector2.Distance(startPosition, targetPosition);

            PotionProjectile proj = Instantiate(_potionPrefab, startPosition, Quaternion.identity); //TODO : 포션 투사채 던질 때 투사체의 스프라이트도 같이 업데이트 되게 하기
            proj.Initiate(consumedPotion, _potionRunCatalog, _potionEffectOwner);
            proj.Launch(targetPosition, _throwDuration, _arcHeight, _rotationSpeed);
        }
    }
    private void HideAimPreview()
    {
        _aimPreviewInstance.Hide();
    }
}
