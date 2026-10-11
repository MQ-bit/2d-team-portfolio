using UnityEngine;

// 바닥 봉인진으로 포션을 흡수해 Reaper에게 보내는 강탈 연출.
public sealed class ReaperPotionSteal : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _potionVisual;
    [SerializeField] private SpriteRenderer _magicCircle;
    [SerializeField] private Animator _magicCircleAnimator;
    [SerializeField] private Vector2 _magicCircleAnchorPixels = new Vector2(68f, 49f);
    [SerializeField] private ReaperAttackIndicator _attackIndicatorPrefab;
    [SerializeField] private TrailRenderer _captureTrailPrefab;
    private TrailRenderer _captureTrail;
    [Header("Reverse pickup effect")]
    [SerializeField] private ParticleSystem _pickupEffectPrefab;
    [SerializeField, Min(0.01f)] private float _reverseEffectSampleTime = 0.35f;
    [SerializeField, Min(0.01f)] private float _reverseEffectDuration = 0.4f;
    private ParticleSystem _reversePickupEffect;
    private ParticleSystem[] _reversePickupParticles;
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
            _warning.ApplyStealPalette(data);
            _warning.SetArea(transform.position, data.StealAreaRadius);
        }
        // 바닥 경고 위에 기존 소용돌이 마법진을 띄운다.
        _magicCircle.gameObject.SetActive(true);
        _magicCircle.color = Color.Lerp(data.StealRuneColor, data.StealAccentColor, 0.3f);
        _magicCircleAnimator.Play("MagicCircle", 0, 0f);
        PositionMagicCircle();
    }

    public void SetProgress(float progress)
    {
        bool active = (1f - Mathf.Clamp01(progress)) * _data.StealWindup <= _data.StealActiveDuration;
        if (_warning != null) _warning.SetProgress(active ? 1f : progress);
        _magicCircle.color = active ? _data.StealChargedColor
            : Color.Lerp(_data.StealRuneColor, _data.StealAccentColor, 0.3f);
        _magicCircleAnimator.speed = active ? 1.35f : 1f;
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
        _potionVisual.transform.localScale = _bottleScale;
        _potionVisual.transform.localRotation = Quaternion.identity;
        if (_captureTrailPrefab != null)
        {
            _captureTrail = Instantiate(_captureTrailPrefab, _potionVisual.transform);
            _captureTrail.transform.localPosition = Vector3.zero;
            _captureTrail.transform.localRotation = Quaternion.identity;
            _captureTrail.transform.localScale = Vector3.one;
            _captureTrail.autodestruct = false;
            _captureTrail.time = 0.35f;
            _captureTrail.widthMultiplier = 0.16f;
            _captureTrail.minVertexDistance = 0.03f;
            _captureTrail.sortingLayerName = _potionVisual.sortingLayerName;
            _captureTrail.sortingOrder = _potionVisual.sortingOrder - 1;
            _captureTrail.startColor = _data.StealRuneColor;
            Color tail = _data.StealAccentColor;
            tail.a = 0f;
            _captureTrail.endColor = tail;
            _captureTrail.Clear();
            _captureTrail.emitting = false;
        }
        if (_warning != null) _warning.gameObject.SetActive(false);
        if (_pickupEffectPrefab != null)
        {
            _reversePickupEffect = Instantiate(_pickupEffectPrefab, playerPosition, Quaternion.identity, transform);
            _reversePickupParticles = _reversePickupEffect.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < _reversePickupParticles.Length; i++)
            {
                ParticleSystem particle = _reversePickupParticles[i];
                var main = particle.main;
                // 복사본의 자동 삭제만 끄고, 고정 시드로 같은 입자를 매번 재현한다.
                main.stopAction = ParticleSystemStopAction.None;
                main.playOnAwake = false;
                // 생성 직후 자동 재생된 입자까지 완전히 비운 다음 시드를 설정한다.
                particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                particle.useAutoRandomSeed = false;
                particle.randomSeed = (uint)(173 + i * 17);
            }
            UpdateReversePickupEffect();
        }
    }

    private void Update()
    {
        if (!_capturing || _data == null) return;
        if (_destination == null) { Destroy(gameObject); return; }
        _captureElapsed += Time.deltaTime;
        UpdateReversePickupEffect();
        float progress = Mathf.Clamp01(_captureElapsed / Mathf.Max(0.01f, _data.StealCaptureDuration));
        Vector3 portal = transform.position + Vector3.up * _data.SummonHeight;
        if (_captureTrail != null) _captureTrail.emitting = progress >= 0.12f;
        if (progress < 0.12f)
        {
            // 빼앗기는 순간 잠깐 버티며 흔들린다. 양끝에서 진동이 사라지게 한다.
            float hold = progress / 0.12f;
            float shake = Mathf.Sin(hold * Mathf.PI * 8f) * Mathf.Sin(hold * Mathf.PI);
            _potionVisual.transform.position = _captureStart + Vector3.right * (shake * 0.06f);
            _potionVisual.transform.localRotation = Quaternion.Euler(0f, 0f, shake * 12f);
            _potionVisual.transform.localScale = _bottleScale * Mathf.Lerp(1f, 1.15f, hold);
        }
        else if (progress < 0.45f)
        {
            float lift = (progress - 0.12f) / 0.33f;
            float pull = lift * lift;
            Vector3 position = Vector3.Lerp(_captureStart, portal, pull);
            position.x += Mathf.Sin(lift * Mathf.PI * 2f) * Mathf.Sin(lift * Mathf.PI) * 0.25f;
            _potionVisual.transform.position = position;
            _potionVisual.transform.localScale = _bottleScale * Mathf.Lerp(1.15f, 1f, lift);
            _potionVisual.transform.localRotation = Quaternion.Euler(0f, 0f, -180f * pull);
        }
        else if (progress < 0.55f)
        {
            float gather = (progress - 0.45f) / 0.1f;
            _potionVisual.transform.position = portal + Vector3.up * (Mathf.Sin(gather * Mathf.PI) * 0.12f);
            _potionVisual.transform.localScale = _bottleScale * (1f + Mathf.Sin(gather * Mathf.PI) * 0.08f);
            _potionVisual.transform.localRotation = Quaternion.Euler(0f, 0f, -180f - gather * 180f);
            _magicCircle.color = Color.Lerp(_data.StealRuneColor, _data.StealChargedColor, gather);
        }
        else
        {
            _magicCircle.gameObject.SetActive(false);
            float transfer = (progress - 0.55f) / 0.45f;
            float pull = transfer * transfer;
            Vector3 end = _destination.position + Vector3.up * 0.6f;
            Vector3 control = BezierCalculator.CalculateWorldUpControlPoint(portal, end, 1.2f);
            _potionVisual.transform.position = BezierCalculator.CalculateQuadraticBezier(portal, control, end,
                pull);
            _potionVisual.transform.localRotation = Quaternion.Euler(0f, 0f, -360f - pull * 240f);
            _potionVisual.transform.localScale = _bottleScale * Mathf.Lerp(1f, 0.1f,
                Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1f, transfer)));
        }
        if (progress >= 1f) Destroy(gameObject);
    }

    private void UpdateReversePickupEffect()
    {
        if (_reversePickupEffect == null) return;
        float progress = Mathf.Clamp01(_captureElapsed / Mathf.Max(0.01f, _reverseEffectDuration));
        if (progress >= 1f)
        {
            _reversePickupEffect.gameObject.SetActive(false);
            return;
        }
        float sampleTime = _reverseEffectSampleTime * (1f - progress);
        foreach (ParticleSystem particle in _reversePickupParticles)
        {
            // 시작부터 같은 시점까지 다시 계산해, 시간이 감소할 때 역재생처럼 보이게 한다.
            particle.Simulate(sampleTime, false, true, false);
        }
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
