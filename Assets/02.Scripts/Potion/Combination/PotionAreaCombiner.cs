using System.Collections.Generic;
using UnityEngine;

public sealed class PotionAreaCombiner : MonoBehaviour // 포션 조합 상대 탐색과 조합 확정
{
    private const float EarlierSoundVolumeRatio = 0.4f;
    private const float LaterSoundVolumeRatio = 0.6f;
    private const float CombinationSoundFadeTime = 0.3f;

    [SerializeField] private PotionCombinationDatabase _database;
    [SerializeField] private PotionCombinedAreaShape _combinedAreaShape;

    private readonly List<Collider2D> _overlaps = new();
    private PotionAreaController _areaController;
    private PotionAreaVaporizer _vaporizer;
    private bool _registered;
    private PotionAreaController _combinedPrimary;
    private PotionAreaController _combinedPartner;
    private bool _vaporized;

    public bool IsCombined { get; private set; }
    internal bool CanVaporize => _registered && !_vaporized;
    internal PotionCombinationDatabase Database => _database;
    internal PotionAreaController PrimaryArea => _combinedPrimary != null
        ? _combinedPrimary : _areaController;
    internal PotionAreaController PartnerArea => _combinedPartner;
    internal Collider2D ReactionCollider => IsCombined
        ? _combinedAreaShape.CombinationCollider : _areaController.AreaCollider;

    internal void MarkVaporized()
    {
        _vaporized = true;
        IsCombined = true;
    }

    private void Awake()
    {
        _areaController = GetComponent<PotionAreaController>();
        _vaporizer = new PotionAreaVaporizer(_areaController, this);
        if (_combinedAreaShape == null)
        {
            _combinedAreaShape = GetComponent<PotionCombinedAreaShape>();
        }
    }

    public void RegisterArea()
    {
        if (_registered || _areaController == null || _areaController.Data == null) return;
        _registered = true;

        if (_database == null || _combinedAreaShape == null || !_combinedAreaShape.IsReady)
        {
            Debug.LogError("PotionAreaCombiner requires a database and PotionCombinedAreaShape.", this);
            return;
        }

        FindExistingOverlap();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_registered || _vaporized) return;
        TryCombine(other.GetComponentInParent<PotionAreaController>());
    }

    private void FindExistingOverlap()
    {
        // 착탄 프레임에 생성된 장판의 위치와 크기를 즉시 반영한다.
        Physics2D.SyncTransforms();
        Collider2D areaCollider = _areaController.AreaCollider;

        ContactFilter2D filter = new() { useTriggers = true };
        areaCollider.Overlap(filter, _overlaps);

        foreach (Collider2D overlap in _overlaps)
        {
            PotionAreaController other = overlap.GetComponentInParent<PotionAreaController>();
            if (TryCombine(other)) break;
        }

        _overlaps.Clear();
    }

    private bool TryCombine(PotionAreaController other)
    {
        if (_vaporized || other == null || other == _areaController || other.Data == null ||
            !other.gameObject.activeInHierarchy)
        {
            return false;
        }

        PotionAreaCombiner otherCombiner = other.GetComponent<PotionAreaCombiner>();
        if (otherCombiner == null || !otherCombiner._registered || otherCombiner._vaporized)
        {
            return false;
        }

        if (!IsCombined && _areaController.Data.Element == PotionElement.Vaporization && other.Data.CreatesFloor)
            return _vaporizer.VaporizeOverlappingSurfaces();
        if (!otherCombiner.IsCombined && other.Data.Element == PotionElement.Vaporization && _areaController.Data.CreatesFloor)
            return otherCombiner._vaporizer.VaporizeOverlappingSurfaces();
        if (IsCombined || otherCombiner.IsCombined) return false;

        PotionCombinationDatabase database = _database != null ? _database : otherCombiner._database;
        if (database == null ||
            !database.TryGetCombination(_areaController.Data.Element, other.Data.Element, out PotionCombinationData data))
        {
            return false;
        }

        if (data.CreatesFloor || data.InheritThrownEffectsFrom != PotionElement.None)
        {
            Vector2 delta = other.OriginalCenter - _areaController.OriginalCenter;
            delta.y *= 2f;
            if (delta.magnitude > _areaController.OriginalRadius + other.OriginalRadius)
                return false;
            return CreateProfileCombination(other, otherCombiner, data, data);
        }

        return CreateInstantProfileCombination(other, otherCombiner, data, data);
    }

    private bool CreateProfileCombination(PotionAreaController other,
        PotionAreaCombiner otherCombiner, PotionCombinationData data,
        IPotionEffectData templateProfile)
    {
        IPotionEffectData effectProfile = templateProfile;
        float duration = data.FloorDuration;
        PotionElement inheritedElement = data.InheritThrownEffectsFrom;
        if (inheritedElement != PotionElement.None)
        {
            PotionData inheritedPotion = _areaController.Data.Element == inheritedElement ? _areaController.Data :
                other.Data.Element == inheritedElement ? other.Data : null;
            effectProfile = inheritedPotion;
            if (effectProfile == null) return false;
            duration = effectProfile.FloorDuration;
        }

        // 양쪽을 먼저 선점한다. 미러 Trigger와 세 번째 포션은 이후 IsCombined에서 차단된다.
        IsCombined = true;
        otherCombiner.IsCombined = true;
        _combinedPrimary = _areaController;
        _combinedPartner = other;
        otherCombiner._combinedPrimary = _areaController;
        otherCombiner._combinedPartner = _areaController;

        _combinedAreaShape.Create(_areaController, other, data.VisualData,
            Mathf.Min(data.SpreadDuration, duration));
        if (!_combinedAreaShape.CombinationCollider.enabled)
        {
            IsCombined = otherCombiner.IsCombined = false;
            _combinedPrimary = _combinedPartner = null;
            otherCombiner._combinedPrimary = otherCombiner._combinedPartner = null;
            return false;
        }
        float expiresAt = Time.time + duration;
        TransferLatestOwnership(other);
        IDamageable owner = _areaController.Owner;
        _areaController.AreaEffect.ReplaceWithCombinationProfile(data, effectProfile, owner,
            templateProfile.LifeStealRatio, expiresAt, _combinedAreaShape.CombinationCollider);
        _areaController.SetExpiration(expiresAt);
        other.SetExpiration(expiresAt);
        // 두 장판음을 유지하고, 먼저 던진 포션 40% / 나중 포션 60%로 부드럽게 섞는다.
        PotionAreaController earlier = _areaController.ActivationOrder < other.ActivationOrder ? _areaController : other;
        PotionAreaController later = earlier == _areaController ? other : _areaController;
        earlier.FadeLoopSoundTo(EarlierSoundVolumeRatio, CombinationSoundFadeTime);
        later.FadeLoopSoundTo(LaterSoundVolumeRatio, CombinationSoundFadeTime);
        RecordCombination(other, data);
        return true;
    }

    private bool CreateInstantProfileCombination(PotionAreaController other,
        PotionAreaCombiner otherCombiner, PotionCombinationData data,
        IPotionEffectData profile)
    {
        IsCombined = true;
        otherCombiner.IsCombined = true;

        PotionAreaController impactAreaController = _areaController.Data.Element == PotionElement.Vaporization
            ? _areaController : other;
        TransferLatestOwnership(other);
        IDamageable owner = _areaController.Owner;
        var source = new PotionEffectSource(_areaController.GetInstanceID(), combination: data,
            owner: owner, lifeStealRatio: profile.LifeStealRatio, areaOwnership: _areaController.Ownership);

        Physics2D.SyncTransforms();
        Vector3 scale = impactAreaController.transform.lossyScale;
        float radiusScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        float radius = profile.EffectRadius * radiusScale;
        var hits = new List<Collider2D>();
        var targets = new HashSet<IPotionEffectTarget>();
        ContactFilter2D filter = new() { useTriggers = true };
        Physics2D.OverlapCircle(impactAreaController.transform.position, radius, filter, hits);
        foreach (Collider2D hit in hits)
        {
            IPotionEffectTarget target = PotionEffectApplier.FindTarget(hit);
            if (!PotionEffectApplier.CanAffect(profile.TargetType, target) || !targets.Add(target)) continue;
            PotionEffectApplier.ApplyProfilePhase(profile, PotionEffectPhase.Impact, target, source);
        }

        RecordCombination(other, data);
        // 기화는 원본 장판의 FloorExit 잔류 효과까지 제거한다.
        _areaController.AreaEffect.DisableForCombination();
        other.AreaEffect.DisableForCombination();
        impactAreaController.FinishInstantImpact();
        if (_areaController != impactAreaController) _areaController.Remove();
        if (other != impactAreaController) other.Remove();
        return true;
    }

    private void TransferLatestOwnership(PotionAreaController other)
    {
        _areaController.Ownership.TakeLatest(other.Ownership);
        other.Ownership.TakeLatest(_areaController.Ownership);
    }

    private void RecordCombination(PotionAreaController other, PotionCombinationData data)
    {
        PotionRunCatalog catalog = _areaController.Catalog != null ? _areaController.Catalog : other.Catalog;
        if (catalog != null) catalog.TryDiscoverCombination(data);
    }
}
