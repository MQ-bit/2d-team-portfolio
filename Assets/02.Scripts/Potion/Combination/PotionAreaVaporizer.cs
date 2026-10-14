using System.Collections.Generic;
using UnityEngine;

// 기화에 닿은 장판 전체의 즉발 효과, 출처 해제, 제거와 반응 VFX를 처리한다.
internal sealed class PotionAreaVaporizer
{
    private const float ElementSoundTailVolumeScale = 0.8f;
    private const float ElementSoundTailFadeTime = 0.8f;
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
        var batch = new PotionVaporizationBatch(_areaController.Data.VaporizationSettings);
        foreach (PotionAreaCombiner surface in surfaces)
            reacted |= Vaporize(surface, batch);

        // One vaporization potion consumes all touched surfaces in this batch.
        if (reacted)
        {
            batch.Apply();
            FinishVaporization();
        }
        return reacted;
    }

    private bool Vaporize(PotionAreaCombiner surface, PotionVaporizationBatch batch)
    {
        PotionAreaController primary = surface.PrimaryArea;
        if (primary == null || !primary.gameObject.activeInHierarchy) return false;
        PotionAreaCombiner primaryCombiner = primary.GetComponent<PotionAreaCombiner>();
        if (primaryCombiner == null || !primaryCombiner.CanVaporize || primary.RemainingDuration <= 0f)
            return false;

        Collider2D effectCollider = primaryCombiner.ReactionCollider;
        if (effectCollider == null || !effectCollider.enabled) return false;

        IPotionEffectData profile = primary.AreaEffect.ActiveProfile;
        var effects = PotionAreaEffectTracker.GetVaporizationEffects(profile,
            primary.RemainingDuration, _areaController.Data.VaporizationSettings);
        PotionEffectSource source = primary.AreaEffect.ActiveSource;
        // 기화도 마지막으로 착지해서 반응에 참여한 포션의 소유자가 가져간다.
        primary.Ownership.TakeLatest(_areaController.Ownership);
        // A newly landed floor may react before its single profile is activated.
        if (source.SourceId == 0)
        {
            source = new PotionEffectSource(primary.AreaEffect.GetInstanceID(), primary.Data,
                owner: primary.Owner, lifeStealRatio: profile.LifeStealRatio, areaOwnership: primary.Ownership);
        }
        source = new PotionEffectSource(source.SourceId, source.Potion, source.Combination,
            source.Owner, source.LifeStealRatio, primary.Ownership, batch);

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
        SpawnAfterimages(settings, primary, primaryCombiner);
        // Record both damaging and healing reactions, but not untested pairs from a three-element surface.
        if (!primaryCombiner.IsCombined)
        {
            PotionCombinationDatabase database = _combiner.Database != null
                ? _combiner.Database : primaryCombiner.Database;
            PotionRunCatalog catalog = _areaController.Catalog != null ? _areaController.Catalog : primary.Catalog;
            if (catalog != null && database != null && database.TryGetCombination(PotionElement.Vaporization,
                    primary.Data.Element, out PotionCombinationData reaction))
            {
                catalog.TryDiscoverCombination(reaction);
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
            partner.FadeOutLoopSound(ElementSoundTailFadeTime, ElementSoundTailVolumeScale);
            partner.Remove();
        }
        primary.AreaEffect.StopForVaporization();
        primary.FadeOutLoopSound(ElementSoundTailFadeTime, ElementSoundTailVolumeScale);
        primary.Remove();

        foreach (IPotionEffectTarget target in targets) batch.Queue(target, profile, source, effects);

        return true;
    }

    // 반응한 원소의 잔상을 기화가 아닌 쪽 장판 범위 안 무작위 위치에 남긴다. (기화 포션의 범위가 아니다)
    // 단일 원소면 그 장판의 범위, 두 원소가 합쳐진 장판이면 합쳐진 전체 범위이고, 이때는 각 원소가 절반씩 나눠 맡는다.
    private void SpawnAfterimages(PotionVaporizationSettings settings, PotionAreaController primary,
        PotionAreaCombiner primaryCombiner)
    {
        PotionAfterimageSettings afterimage = settings != null ? settings.AfterimageSettings : null;
        // 합쳐진 장판은 두 컨트롤러 모두 합쳐진 범위(SetCombinedFootprint)를 갖는다.
        PotionAreaFootprint area = primary.Footprint;
        if (afterimage == null || area == null) return;

        PotionAreaController partner = primaryCombiner.IsCombined ? primaryCombiner.PartnerArea : null;
        float share = partner != null ? 0.5f : 1f;
        PotionAfterimageVfx.Spawn(afterimage, primary.Data.Element, area, primary.AreaRenderer, share);
        if (partner != null)
        {
            PotionAfterimageVfx.Spawn(afterimage, partner.Data.Element, area, primary.AreaRenderer, share);
        }
    }

    // 기화 포션 하나가 여러 장판과 반응해도 소리는 한 번만 낸다.
    private void PlayReactionSound()
    {
        // 기화 장판은 바로 사라지지만 소리는 매니저가 끝까지 재생한다.
        SoundManager.Play(GameSoundId.VaporizationReaction);
    }

    private void FinishVaporization()
    {
        PlayReactionSound();
        // 성공한 반응을 모두 처리한 뒤, 기화 포션 하나당 충격·잔진동 연출을 한 번만 시작한다.
        PotionVaporizationSettings settings = _areaController.Data.VaporizationSettings;
        if (settings != null && settings.ReactionShakeForce > 0f)
        {
            CameraController camera = CameraController.Instance;
            if (camera != null)
            {
                camera.ShakeCameraBurst(settings.ReactionShakeForce,
                settings.ReactionShakePulseCount, settings.ReactionShakeInterval, settings.ReactionShakeDecay);
            }
        }
        if (_areaController.AreaEffect != null) _areaController.AreaEffect.DisableForCombination();
        _areaController.FinishInstantImpact();
    }
}
