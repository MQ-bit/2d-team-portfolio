using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MeleeAttackIndicator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Canvas _worldCanvas;
    [SerializeField] private RectTransform _indicatorRect;
    [SerializeField] private Image _indicatorImage;

    [Header("Image Settings")]
    [SerializeField] private float _fillStartLocalAngle = -90f;

    private float _attackAngle;

    private void Awake()
    {
        ValidateReferences();
    }

    public void SetIndicatorSize(float attackRange, float attackAngle)
    {
        if (!ValidateReferences())
        {
            return;
        }

        _attackAngle = Mathf.Clamp(attackAngle, 0f, 360f);

        UpdateSize(attackRange);
        _indicatorImage.fillAmount = _attackAngle / 360f;
    }

    public void Draw(Vector3 origin, Vector2 attackDirection)
    {
        if (_indicatorRect == null || attackDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        _indicatorRect.position = origin;

        float directionAngle = Mathf.Atan2(attackDirection.y, attackDirection.x) * Mathf.Rad2Deg;

        float fillDirection = _indicatorImage.fillClockwise ? -1f : 1f;
        float localSectorCenter = _fillStartLocalAngle + fillDirection * _attackAngle * 0.5f;

        float indicatorRotation = directionAngle - localSectorCenter;

        _indicatorRect.rotation = Quaternion.Euler(0f, 0f, indicatorRotation);
    }

    public void Show()
    {
        if (_indicatorRect != null)
        {
            _indicatorRect.gameObject.SetActive(true);
        }
    }

    public void Hide()
    {
        if (_indicatorRect != null)
        {
            _indicatorRect.gameObject.SetActive(false);
        }
    }

    private void UpdateSize(float attackRange)
    {
        float worldDiameter = Mathf.Max(0f, attackRange) * 2f;

        Transform parent = _indicatorRect.parent;
        Vector3 parentScale = parent != null ? parent.lossyScale : Vector3.one;

        float canvasDiameter = worldDiameter / parentScale.x;

        _indicatorRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, canvasDiameter);
        _indicatorRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, canvasDiameter);

        _indicatorRect.localScale = Vector3.one;
    }

    private bool ValidateReferences()
    {
        if (_worldCanvas == null)
        {
            _worldCanvas = GetComponentInParent<Canvas>();
        }

        if (_indicatorRect == null)
        {
            Debug.LogError($"{gameObject.name}: Indicator RectTransform이 할당되지 않음");
            return false;
        }

        if (_indicatorImage == null)
        {
            _indicatorImage = _indicatorRect.GetComponent<Image>();
            if (_indicatorImage == null)
            {
                Debug.LogError($"{gameObject.name}: Indicator Image가 할당되지 않음");
                return false;
            }
        }

        if (_worldCanvas == null || _worldCanvas.renderMode != RenderMode.WorldSpace)
        {
            Debug.LogWarning($"{gameObject.name}: World Space Canvas가 필요합니다.");
        }

        return true;
    }
}