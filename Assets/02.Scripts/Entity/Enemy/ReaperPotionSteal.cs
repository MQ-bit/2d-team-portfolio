using UnityEngine;

// 소환 프리팹의 마법진을 재사용하되 포션을 위로 흡수해 Reaper에게 보내는 연출.
public sealed class ReaperPotionSteal : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _potionVisual;
    [SerializeField] private SpriteRenderer _magicCircle;
    [SerializeField] private Animator _magicCircleAnimator;
    [SerializeField] private Vector2 _magicCircleAnchorPixels = new Vector2(68f, 49f);
    [SerializeField] private ReaperAttackIndicator _attackIndicatorPrefab;
    private ReaperAttackIndicator _warning;
    private ReaperData _data;
    private Transform _destination;
    private Vector3 _captureStart, _bottleScale;
    private float _captureElapsed;
    private bool _capturing;

    public void Initialize(ReaperData data)
    {
        _data = data;
        _potionVisual.enabled = false;
        _bottleScale = _potionVisual.transform.localScale;
        if (_attackIndicatorPrefab != null)
        {
            _warning = Instantiate(_attackIndicatorPrefab, transform);
            _warning.SetArea(transform.position, data.StealAreaRadius);
        }
        _magicCircleAnimator.Play("MagicCircle", 0, 0f);
        PositionMagicCircle();
    }

    public void SetProgress(float progress)
    {
        if (_warning != null) _warning.SetProgress(progress);
    }

    public void PlayCapture(PotionItem item, Vector3 playerPosition, Transform destination)
    {
        _capturing = true;
        _captureElapsed = 0f;
        _captureStart = playerPosition;
        _destination = destination;
        _potionVisual.sprite = item.BottleSprite != null ? item.BottleSprite : item.PotionData.VisualData.IconSprite;
        _potionVisual.enabled = true;
        _potionVisual.transform.position = playerPosition;
        if (_warning != null) _warning.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!_capturing || _data == null) return;
        if (_destination == null) { Destroy(gameObject); return; }
        _captureElapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(_captureElapsed / _data.StealCaptureDuration);
        Vector3 portal = transform.position + Vector3.up * _data.SummonHeight;
        if (progress < 0.55f)
        {
            _potionVisual.transform.position = Vector3.Lerp(_captureStart, portal, Mathf.SmoothStep(0f, 1f, progress / 0.55f));
        }
        else
        {
            _magicCircle.gameObject.SetActive(false);
            float transfer = (progress - 0.55f) / 0.45f;
            _potionVisual.transform.position = Vector3.Lerp(portal, _destination.position + Vector3.up * 0.6f, transfer);
            _potionVisual.transform.localScale = _bottleScale * Mathf.Lerp(1f, 0.15f, transfer);
        }
        if (progress >= 1f) Destroy(gameObject);
    }

    private void LateUpdate()
    {
        if (_data != null && _magicCircle.gameObject.activeSelf) PositionMagicCircle();
    }

    private void PositionMagicCircle()
    {
        if (_magicCircle.sprite == null) return;
        Vector2 offset = (_magicCircle.sprite.rect.center - _magicCircleAnchorPixels) / _magicCircle.sprite.pixelsPerUnit;
        _magicCircle.transform.localPosition = Vector3.up * _data.SummonHeight
            + Vector3.Scale(new Vector3(offset.x, offset.y, 0f), _magicCircle.transform.localScale);
    }
}
