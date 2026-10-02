using System.Collections.Generic;
using UnityEngine;

public sealed class PotionAreaCombiner : MonoBehaviour // 포션 조합 상대 탐색과 조합 확정
{
    private const float VaporizationMultiplier = 1.2f;
    [SerializeField] private PotionCombinationDatabase _database;
    [SerializeField] private PotionCombinedAreaShape _combinedAreaShape;

    private readonly List<Collider2D> _overlaps = new();
    private PotionAreaController _areaController;
    private bool _registered;
    private PotionAreaController _combinedPrimary;
    private PotionAreaController _combinedPartner;
    private bool _vaporized;

    public bool IsCombined { get; private set; }

    private void Awake()
    {
        _areaController = GetComponent<PotionAreaController>();
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

        // Vaporization consumes a surface, including an already combined one,
        // before the usual two-potion combination restrictions are checked.
        if (!IsCombined && _areaController.Data.Element == PotionElement.Vaporization && other.Data.CreatesFloor)
            return Vaporize(otherCombiner);
        if (!otherCombiner.IsCombined && other.Data.Element == PotionElement.Vaporization && _areaController.Data.CreatesFloor)
            return otherCombiner.Vaporize(this);
        if (IsCombined || otherCombiner.IsCombined) return false;

        PotionCombinationDatabase database = _database != null ? _database : otherCombiner._database;
        if (database == null ||
            !database.TryGetCombination(_areaController.Data.Element, other.Data.Element, out PotionCombinationData data))
        {
            return false;
        }

        if (data.CreatesFloor || data.InheritThrownEffectsFrom != PotionElement.None)
        {
            // Single surfaces are 2:1 ellipses. A circular broad-phase overlap alone
            // must not start a visible reaction across an empty vertical gap.
            Vector2 delta = other.AreaCollider.bounds.center - _areaController.AreaCollider.bounds.center;
            delta.y *= 2f;
            if (delta.magnitude > _areaController.AreaCollider.bounds.extents.x + other.AreaCollider.bounds.extents.x)
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
        float expiresAt = Time.time + duration;
        IDamageable owner = IsAlive(_areaController.Owner) ? _areaController.Owner : other.Owner;
        _areaController.AreaEffect.ReplaceWithCombinationProfile(data, effectProfile, owner,
            templateProfile.LifeStealRatio, expiresAt, _combinedAreaShape.CombinationCollider);
        _areaController.SetExpiration(expiresAt);
        other.SetExpiration(expiresAt);
        return true;
    }

    private bool Vaporize(PotionAreaCombiner surface)
    {
        PotionAreaController primary = surface._combinedPrimary != null
            ? surface._combinedPrimary : surface._areaController;
        if (primary == null || !primary.gameObject.activeInHierarchy) return false;
        PotionAreaCombiner primaryCombiner = primary.GetComponent<PotionAreaCombiner>();
        if (primaryCombiner == null || primaryCombiner._vaporized || primary.RemainingDuration <= 0f)
            return false;

        Collider2D effectCollider = primaryCombiner.IsCombined
            ? primaryCombiner._combinedAreaShape.CombinationCollider : primary.AreaCollider;
        if (effectCollider == null || !effectCollider.enabled) return false;

        IPotionEffectData profile = primary.AreaEffect.ActiveProfile;
        var effects = PotionAreaEffectTracker.GetRemainingEffects(profile,
            primary.RemainingDuration, VaporizationMultiplier);
        PotionEffectSource source = primary.AreaEffect.ActiveSource;
        // A newly landed floor may react before its single profile is activated.
        if (source.SourceId == 0)
        {
            source = new PotionEffectSource(primary.AreaEffect.GetInstanceID(), primary.Data,
                owner: primary.Owner, lifeStealRatio: profile.LifeStealRatio);
        }

        Physics2D.SyncTransforms();
        var hits = new List<Collider2D>();
        effectCollider.Overlap(new ContactFilter2D { useTriggers = false }, hits);
        var targets = new HashSet<IPotionEffectTarget>();
        foreach (Collider2D hit in hits)
        {
            IPotionEffectTarget target = PotionEffectApplier.FindTarget(hit);
            if (PotionEffectApplier.CanAffect(profile.TargetType, target)) targets.Add(target);
        }

        // Claim all participants before callbacks, so the shared collider cannot
        // consume the same combination twice through either original lobe.
        PotionVaporizationReactionVfx.Spawn(_areaController.Data.VaporizationSettings,
            effectCollider, primary.AreaRenderer);
        _vaporized = true;
        IsCombined = true;
        primaryCombiner._vaporized = true;
        primaryCombiner.IsCombined = true;
        PotionAreaController partner = primaryCombiner._combinedPartner;
        if (partner != null)
        {
            PotionAreaCombiner partnerCombiner = partner.GetComponent<PotionAreaCombiner>();
            if (partnerCombiner != null) partnerCombiner._vaporized = true;
            partner.AreaEffect.StopForVaporization();
            partner.Remove();
        }
        primary.AreaEffect.StopForVaporization();
        primary.Remove();

        foreach (IPotionEffectTarget target in targets)
        {
            foreach (PotionEffectDefinition effect in effects)
            {
                if (PotionEffectApplier.CanAffect(profile.TargetType, target))
                    target.ApplyEffect(effect, profile, source);
            }
        }

        FinishVaporization();
        return true;
    }

    private void FinishVaporization()
    {
        if (_areaController.AreaEffect != null) _areaController.AreaEffect.DisableForCombination();
        _areaController.FinishInstantImpact();
    }

    private bool CreateInstantProfileCombination(PotionAreaController other,
        PotionAreaCombiner otherCombiner, PotionCombinationData data,
        IPotionEffectData profile)
    {
        IsCombined = true;
        otherCombiner.IsCombined = true;

        PotionAreaController impactAreaController = _areaController.Data.Element == PotionElement.Vaporization
            ? _areaController : other;
        IDamageable owner = IsAlive(impactAreaController.Owner) ? impactAreaController.Owner :
            IsAlive(_areaController.Owner) ? _areaController.Owner : other.Owner;
        var source = new PotionEffectSource(_areaController.GetInstanceID(), combination: data,
            owner: owner, lifeStealRatio: profile.LifeStealRatio);

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

        // 기화는 원본 장판의 FloorExit 잔류 효과까지 제거한다.
        _areaController.AreaEffect.DisableForCombination();
        other.AreaEffect.DisableForCombination();
        impactAreaController.FinishInstantImpact();
        if (_areaController != impactAreaController) _areaController.Remove();
        if (other != impactAreaController) other.Remove();
        return true;
    }

    private static bool IsAlive(IDamageable target)
    {
        return target != null &&
               !(target is Object unityObject && unityObject == null) &&
               !target.IsDead;
    }
}
