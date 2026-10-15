using UnityEngine;

// 고정 지점에 포션을 떨어뜨린다. 장판은 원본 SO의 대상 설정에 따라 시전자도 피해를 받는다.
public sealed class ReaperPotionDrop : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _potionVisual;
    [SerializeField] private SpriteRenderer _magicCircle;
    [SerializeField] private Animator _magicCircleAnimator;
    [SerializeField] private Vector2 _magicCircleAnchorPixels = new Vector2(68f, 49f);
    [SerializeField] private PotionAreaController _areaPrefab;
    [SerializeField] private PotionBreakEffect _breakEffectPrefab;
    [SerializeField] private ReaperAttackIndicator _attackIndicatorPrefab;
    [Tooltip("포션 종류와 관계없이 사용하는 낙하 위험 표시 색상.")]
    [SerializeField] private Color _warningColor = new Color(0.38f, 0.11f, 0.13f, 0.95f);
    [Header("Summon magic circle")]
    [SerializeField] private Color _magicCircleColor = new Color(0.62f, 0.19f, 0.18f, 1f);
    [SerializeField] private Color _magicCircleChargedColor = new Color(0.86f, 0.36f, 0.3f, 1f);
    private ReaperData _data;
    private PotionData _potion;
    private PotionItem _overridePotion;
    private IDamageable _owner;
    private long _throwOrder;
    private float _elapsed;
    private bool _impacted;
    private ReaperAttackIndicator _warning;

    public void SetOwner(IDamageable owner) => _owner = owner;
    public void SetPotion(PotionItem potion) => _overridePotion = potion;

    public void Initialize(ReaperData data)
    {
        _throwOrder = PotionAreaOwnership.NextThrowOrder();
        _data = data;
        _potion = !_overridePotion.IsEmpty ? _overridePotion.PotionData : data.PickSummonPotion();
        if (_potion == null || _areaPrefab == null)
        {
            Debug.LogError($"{name}: 공격용 4종 포션 SO와 장판 프리팹을 연결하세요.", this);
            enabled = false;
            Destroy(gameObject);
            return;
        }
        Sprite bottle = _potion.VisualData.IconSprite;
        PotionRunCatalog catalog = FindAnyObjectByType<PotionRunCatalog>();
        if (catalog != null && catalog.IsInitialized && catalog.TryGetAppearanceIndex(_potion, out int index))
            bottle = catalog.GetPotionItemAt(index).BottleSprite;
        if (!_overridePotion.IsEmpty && _overridePotion.BottleSprite != null) bottle = _overridePotion.BottleSprite;
        if (bottle != null) _potionVisual.sprite = bottle;
        _elapsed = 0f;
        _impacted = false;
        // 기존 장판의 2:1 타원 범위와 일치시킨다.
        if (_attackIndicatorPrefab != null)
        {
            _warning = Instantiate(_attackIndicatorPrefab, transform);
            _warning.SetPotionArea(transform.position, _potion.EffectRadius, _warningColor);
        }
        _magicCircle.color = _magicCircleColor;
        _magicCircle.gameObject.SetActive(true);
        _magicCircleAnimator.Play("MagicCircle", 0, 0f);
        PositionMagicCircle();
        _potionVisual.enabled = false;
        _potionVisual.transform.localPosition = Vector3.up * data.SummonHeight;
    }

    private void Update()
    {
        if (_data == null || _impacted) return;
        _elapsed += Time.deltaTime;
        float warningProgress = Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, _data.WarningDuration + _data.FallDuration));
        if (_warning != null)
            _warning.SetProgress(warningProgress);
        _magicCircle.color = Color.Lerp(_magicCircleColor, _magicCircleChargedColor,
            Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.6f, 0.9f, warningProgress)));
        if (_elapsed < _data.WarningDuration) return;
        _potionVisual.enabled = true;
        float fall = Mathf.Clamp01((_elapsed - _data.WarningDuration) / _data.FallDuration);
        _potionVisual.transform.localPosition = Vector3.up * (_data.SummonHeight * (1f - fall * fall));
        if (fall < 1f) return;
        _impacted = true;
        SoundManager.PlayPotionBreak();
        PotionAreaController area = Instantiate(_areaPrefab, transform.position, Quaternion.identity);
        area.Initialize(_potion);
        area.SetOwner(_owner);
        area.SetThrowOrder(_throwOrder);
        area.Activate();
        if (_breakEffectPrefab != null)
            Instantiate(_breakEffectPrefab, transform.position, Quaternion.identity).Play();
        _potionVisual.enabled = false;
        _magicCircle.gameObject.SetActive(false);
        if (_warning != null) _warning.gameObject.SetActive(false);
        Destroy(gameObject, 0.15f);
    }
    private void LateUpdate()
    {
        if (_data != null && !_impacted) PositionMagicCircle();
    }
    private void PositionMagicCircle()
    {
        if (_magicCircle.sprite == null) return;
        // 프레임마다 잘린 영역이 달라도 원본 캔버스의 같은 지점에 정렬한다.
        Vector2 offset = (_magicCircle.sprite.rect.center - _magicCircleAnchorPixels) / _magicCircle.sprite.pixelsPerUnit;
        _magicCircle.transform.localPosition = Vector3.up * _data.SummonHeight
            + Vector3.Scale(new Vector3(offset.x, offset.y, 0f), _magicCircle.transform.localScale);
    }
}
