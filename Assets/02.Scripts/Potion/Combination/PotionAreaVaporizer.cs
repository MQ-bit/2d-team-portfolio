using System.Collections.Generic;
using UnityEngine;

// 기화에 닿은 장판 전체의 즉발 효과, 출처 해제, 제거와 반응 VFX를 처리한다.
internal sealed class PotionAreaVaporizer
{
    private const float VaporizationMultiplier = 1.2f;
    private readonly PotionAreaController _areaController;
    private readonly PotionAreaCombiner _combiner;

    internal PotionAreaVaporizer(PotionAreaController areaController, PotionAreaCombiner combiner)
    {
        _areaController = areaController;
        _combiner = combiner;
    }

    internal bool VaporizeOverlappingSurfaces()
    {
        if (!_combiner.CanVaporize) return false;

        // Snapshot every contact before removing a surface or the vaporization area.
        Physics2D.SyncTransforms();
        var overlaps = new List<Collider2D>();
        _areaController.AreaCollider.Overlap(new ContactFilter2D { useTriggers = true }, overlaps);
        var surfaces = new HashSet<PotionAreaCombiner>();
        foreach (Collider2D overlap in overlaps)
        {
            PotionAreaController area = overlap.GetComponentInParent<PotionAreaController>();
            if (area == null || area == _areaController || area.Data == null ||
                !area.Data.CreatesFloor || !area.gameObject.activeInHierarchy)
            {
                continue;
            }

            PotionAreaCombiner surface = area.GetComponent<PotionAreaCombiner>();
            if (surface == null || !surface.CanVaporize) continue;
            // Both original lobes can refer to the same combined surface.
            PotionAreaController primary = surface.PrimaryArea;
            PotionAreaCombiner primaryCombiner = primary.GetComponent<PotionAreaCombiner>();
            if (primaryCombiner != null) surfaces.Add(primaryCombiner);
        }

        bool reacted = false;
        foreach (PotionAreaCombiner surface in surfaces)
            reacted |= Vaporize(surface);

        // One vaporization potion consumes all touched surfaces in this batch.
        if (reacted) FinishVaporization();
        return reacted;
    }

    private bool Vaporize(PotionAreaCombiner surface)
    {
        PotionAreaController primary = surface.PrimaryArea;
        if (primary == null || !primary.gameObject.activeInHierarchy) return false;
        PotionAreaCombiner primaryCombiner = primary.GetComponent<PotionAreaCombiner>();
        if (primaryCombiner == null || !primaryCombiner.CanVaporize || primary.RemainingDuration <= 0f)
            return false;

        Collider2D effectCollider = primaryCombiner.ReactionCollider;
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
        PotionVaporizationSettings settings = _areaController.Data.VaporizationSettings;
        if (!primaryCombiner.IsCombined && primary.Data.Element == PotionElement.Heal)
        {
            // A healing reaction must not read as one of the damaging explosions.
            PotionPurifyingLifeVfx.Spawn(effectCollider, primary.AreaRenderer, primary.Footprint);
        }
        else if (primaryCombiner.IsCombined)
        {
            // A two-element surface uses the shared VFX when vaporization is added.
            PotionVaporizationReactionVfx.Spawn(settings, effectCollider, primary.AreaRenderer);
        }
        else
        {
            // A single-element surface uses its element + vaporization combination VFX.
            PotionCombinationDatabase database = _combiner.Database != null
                ? _combiner.Database : primaryCombiner.Database;
            if (database != null && database.TryGetCombination(PotionElement.Vaporization,
                    primary.Data.Element, out PotionCombinationData combination))
            {
                float duration = settings != null ? settings.ReactionVfxDuration : 1f;
                PotionVaporizationReactionVfx.Spawn(combination.VisualData.AreaVfxPrefab,
                    duration, effectCollider, primary.AreaRenderer);
            }
        }
        _combiner.MarkVaporized();
        primaryCombiner.MarkVaporized();
        PotionAreaController partner = primaryCombiner.PartnerArea;
        if (partner != null)
        {
            PotionAreaCombiner partnerCombiner = partner.GetComponent<PotionAreaCombiner>();
            if (partnerCombiner != null) partnerCombiner.MarkVaporized();
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

        return true;
    }

    // 기화 포션 하나가 여러 장판과 반응해도 소리는 한 번만 낸다.
    private void PlayReactionSound()
    {
        PotionVaporizationSettings settings = _areaController.Data.VaporizationSettings;
        if (settings == null || settings.ReactionSound == null) return;

        // 기화 장판은 바로 사라지지만 소리는 매니저가 끝까지 재생한다.
        SoundManager.Play(settings.ReactionSound, SoundChannel.Potion, settings.ReactionSoundVolume);
    }

    private void FinishVaporization()
    {
        PlayReactionSound();
        if (_areaController.AreaEffect != null) _areaController.AreaEffect.DisableForCombination();
        _areaController.FinishInstantImpact();
    }
}
