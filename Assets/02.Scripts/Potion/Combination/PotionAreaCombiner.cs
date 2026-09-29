using System.Collections.Generic;
using UnityEngine;

public sealed class PotionAreaCombiner : MonoBehaviour
{
    private const float CombinationDuration = 4f;
    private const float FixedInstantWindow = 0.02f;
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
            !database.TryGetCombination(_area.Data.Element, other.Data.Element, out PotionCombinationData data) ||
            !PotionCombinationEffectResolver.TryCalculate(data, _area.Data, other.Data, out var result))
        {
            return false;
        }

        // Claim both areas before applying anything so the mirrored trigger callback cannot duplicate the combination.
        IsCombined = true;
        otherCombiner.IsCombined = true;

        IDamageable primaryOwner = IsAlive(_area.Owner) ? _area.Owner : other.Owner;
        PotionEffectResolver.RestoreCombinationOwner(primaryOwner, result.PlayerHealthRecovery);

        bool hasTargetEffect = result.DamagePerTick > 0f || result.Status.HasValue;
        if (result.RemoveGroundAreas || data.InstantOnly)
        {
            if (hasTargetEffect)
            {
                PotionGroundArea sourceArea = _area.Data.Element == PotionElement.Vaporization ? other : _area;
                IDamageable sourceOwner = IsAlive(sourceArea.Owner) ? sourceArea.Owner : primaryOwner;
                sourceArea.AreaEffect.ReplaceWithCombination(result, sourceOwner,
                    Time.time + FixedInstantWindow, false);
            }

            _area.Remove();
            other.Remove();
            return true;
        }

        float expiresAt = Time.time + CombinationDuration;
        _combinedAreaShape.Create(_area, other, data.VisualData);
        IDamageable combinationOwner = IsAlive(_area.Owner) ? _area.Owner : other.Owner;
        _area.AreaEffect.ReplaceWithCombination(result, combinationOwner, expiresAt);
        _area.SetExpiration(expiresAt);
        other.SetExpiration(expiresAt);
        return true;
    }

    private static bool IsAlive(IDamageable target)
    {
        return target != null &&
               !(target is Object unityObject && unityObject == null) &&
               !target.IsDead;
    }
}
