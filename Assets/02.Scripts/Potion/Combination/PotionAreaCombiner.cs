using System.Collections.Generic;
using UnityEngine;

public sealed class PotionAreaCombiner : MonoBehaviour // 포션 조합 상대 탐색과 조합 확정
{
    [SerializeField] private PotionCombinationDatabase _database;
    [SerializeField] private PotionCombinedAreaShape _combinedAreaShape;

    private readonly List<Collider2D> _overlaps = new();
    private PotionGroundArea _area;
    private bool _registered;

    public bool IsCombined { get; private set; }

    private void Awake()
    {
        _area = GetComponent<PotionGroundArea>();
        if (_combinedAreaShape == null)
        {
            _combinedAreaShape = GetComponent<PotionCombinedAreaShape>();
        }
    }

    public void RegisterArea()
    {
        if (_registered || _area == null || _area.Data == null) return;
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
        if (!_registered || IsCombined) return;
        TryCombine(other.GetComponentInParent<PotionGroundArea>());
    }

    private void FindExistingOverlap()
    {
        // 착탄 프레임에 생성된 장판의 위치와 크기를 즉시 반영한다.
        Physics2D.SyncTransforms();
        CircleCollider2D areaCollider = _area.AreaCollider;
        Vector3 scale = areaCollider.transform.lossyScale;
        float radiusScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        float radius = areaCollider.radius * radiusScale;
        Vector2 center = areaCollider.transform.TransformPoint(areaCollider.offset);

        ContactFilter2D filter = new() { useTriggers = true };
        Physics2D.OverlapCircle(center, radius, filter, _overlaps);

        foreach (Collider2D overlap in _overlaps)
        {
            PotionGroundArea other = overlap.GetComponentInParent<PotionGroundArea>();
            if (TryCombine(other)) break;
        }

        _overlaps.Clear();
    }

    private bool TryCombine(PotionGroundArea other)
    {
        if (IsCombined || other == null || other == _area || other.Data == null ||
            !other.gameObject.activeInHierarchy)
        {
            return false;
        }

        PotionAreaCombiner otherCombiner = other.GetComponent<PotionAreaCombiner>();
        if (otherCombiner == null || !otherCombiner._registered || otherCombiner.IsCombined)
        {
            return false;
        }

        PotionCombinationDatabase database = _database != null ? _database : otherCombiner._database;
        if (database == null ||
            !database.TryGetCombination(_area.Data.Element, other.Data.Element, out PotionCombinationData data))
        {
            return false;
        }

        if (data.CreatesFloor || data.InheritThrownEffectsFrom != PotionElement.None)
        {
            // Single surfaces are 2:1 ellipses. A circular broad-phase overlap alone
            // must not start a visible reaction across an empty vertical gap.
            Vector2 delta = other.AreaCollider.bounds.center - _area.AreaCollider.bounds.center;
            delta.y *= 2f;
            if (delta.magnitude > _area.AreaCollider.bounds.extents.x + other.AreaCollider.bounds.extents.x)
                return false;
            return CreateProfileCombination(other, otherCombiner, data, data);
        }

        return CreateInstantProfileCombination(other, otherCombiner, data, data);
    }

    private bool CreateProfileCombination(PotionGroundArea other,
        PotionAreaCombiner otherCombiner, PotionCombinationData data,
        IPotionEffectData templateProfile)
    {
        IPotionEffectData effectProfile = templateProfile;
        float duration = data.FloorDuration;
        PotionElement inheritedElement = data.InheritThrownEffectsFrom;
        if (inheritedElement != PotionElement.None)
        {
            PotionData inheritedPotion = _area.Data.Element == inheritedElement ? _area.Data :
                other.Data.Element == inheritedElement ? other.Data : null;
            effectProfile = inheritedPotion;
            if (effectProfile == null) return false;
            duration = effectProfile.FloorDuration;
        }

        // 양쪽을 먼저 선점한다. 미러 Trigger와 세 번째 포션은 이후 IsCombined에서 차단된다.
        IsCombined = true;
        otherCombiner.IsCombined = true;

        _combinedAreaShape.Create(_area, other, data.VisualData,
            Mathf.Min(data.SpreadDuration, duration));
        float expiresAt = Time.time + duration;
        IDamageable owner = IsAlive(_area.Owner) ? _area.Owner : other.Owner;
        _area.AreaEffect.ReplaceWithCombinationProfile(data, effectProfile, owner,
            templateProfile.LifeStealRatio, expiresAt, _combinedAreaShape.CombinationCollider);
        _area.SetExpiration(expiresAt);
        other.SetExpiration(expiresAt);
        return true;
    }

    private bool CreateInstantProfileCombination(PotionGroundArea other,
        PotionAreaCombiner otherCombiner, PotionCombinationData data,
        IPotionEffectData profile)
    {
        IsCombined = true;
        otherCombiner.IsCombined = true;

        PotionGroundArea impactArea = _area.Data.Element == PotionElement.Vaporization
            ? _area : other;
        IDamageable owner = IsAlive(impactArea.Owner) ? impactArea.Owner :
            IsAlive(_area.Owner) ? _area.Owner : other.Owner;
        var source = new PotionEffectSource(_area.GetInstanceID(), combination: data,
            owner: owner, lifeStealRatio: profile.LifeStealRatio);

        Physics2D.SyncTransforms();
        Vector3 scale = impactArea.transform.lossyScale;
        float radiusScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        float radius = profile.EffectRadius * radiusScale;
        var hits = new List<Collider2D>();
        var targets = new HashSet<IPotionEffectTarget>();
        ContactFilter2D filter = new() { useTriggers = true };
        Physics2D.OverlapCircle(impactArea.transform.position, radius, filter, hits);
        foreach (Collider2D hit in hits)
        {
            IPotionEffectTarget target = PotionEffectApplier.FindTarget(hit);
            if (!PotionEffectApplier.CanAffect(profile.TargetType, target) || !targets.Add(target)) continue;
            PotionEffectApplier.ApplyProfilePhase(profile, PotionEffectPhase.Impact, target, source);
        }

        // 기화는 원본 장판의 FloorExit 잔류 효과까지 제거한다.
        _area.AreaEffect.DisableForCombination();
        other.AreaEffect.DisableForCombination();
        _area.Remove();
        other.Remove();
        return true;
    }

    private static bool IsAlive(IDamageable target)
    {
        return target != null &&
               !(target is Object unityObject && unityObject == null) &&
               !target.IsDead;
    }
}
