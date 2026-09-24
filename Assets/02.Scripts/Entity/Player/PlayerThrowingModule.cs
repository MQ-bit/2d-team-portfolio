using UnityEngine;
using Entity.Inventory;

public class PlayerThrowingModule : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private Camera _worldCamera;
    [SerializeField] private Transform _throwPoint;
    [SerializeField] private PlayerStatus _status;
    [SerializeField] private PlayerInventoryModule _inventoryModule;

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

        if( _inventoryModule == null)
        {
            _inventoryModule = GetComponent<PlayerInventoryModule>();
        }
    }

    public void BeginAim(Vector2 screenPosition)
    {
        //if(_inventoryModule.SelectedPotion == null) return;

        IsAiming = true;
        _aimPreviewInstance.Show();

        // TODO : 아래 Draw에 PotionData를 넘겨줘서 색깔 및 범위 정하게 하기. 일단 선택된 포션데이터가 null이면 아무일도 없게하는 로직도 생각해보기
        //  PotionData selectedPotion = _inventoryModule.SelectedPotion;
        // _aimPreviewInstance.Configure(_inventoryModule.SelectedPotion.Color, _inventoryModule.SelectedPotion.AoeRadius);
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

        // TODO : 아래 Draw에 PotionData를 넘겨줘서 색깔 및 범위 정하게 하기.
        //  PotionData selectedPotion = _inventoryModule.SelectedPotion;
        // _aimPreviewInstance.Configure(_inventoryModule.SelectedPotion.Color, _inventoryModule.SelectedPotion.AoeRadius);
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
        float maxThrowDistance =  _status.MaxThrowDistance;

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
        // if(_inventoryModule.TryConsumeSelectedPotion(out PotionData selectedPotion))
        // {
        //     PotionProjectile proj = Instantiate(_potionPrefab, startPosition, Quaternion.identity);
        //     proj.SetPotionData(selectedPotion, startPosition, targetPosition, _arcHeight,_inventoryModule.PotionData); 포션데이타..를 넣나..?
        //     proj.Launch(startPos, _targetPosition, arcHeight);
        // }
    }
    private void HideAimPreview()
    {
        _aimPreviewInstance.Hide();
    }
}
