using System.Collections.Generic;
using UnityEngine;

// Demo-only opt-in: the isolated scene supplies the layers of its single player.
// No cached target or slower search interval: distance/physics ordering stay unchanged.
internal static class OptimizedEnemySearch
{
    private static bool _hasPlayerLayers;
    private static int _playerLayers;

    internal static void ConfigureIsolatedPlayer(PotionEffectReceiver player)
    {
        _hasPlayerLayers = false;
        _playerLayers = 0;
        if (player == null || player.TargetKind != PotionTargetKind.Player) return;
        foreach (Collider2D collider in player.GetComponentsInChildren<Collider2D>(true))
        {
            if (collider.GetComponentInParent<PotionEffectReceiver>() == player)
                _playerLayers |= 1 << collider.gameObject.layer;
        }

        _hasPlayerLayers = _playerLayers != 0;
    }

    internal static void Clear() { _hasPlayerLayers = false; _playerLayers = 0; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => Clear();

    internal static Transform Find(Vector2 origin, float range, List<Collider2D> results)
    {
        ContactFilter2D filter = ContactFilter2D.noFilter;
        if (_hasPlayerLayers) filter.SetLayerMask(_playerLayers);
        Physics2D.OverlapCircle(origin, range, filter, results);
        foreach (Collider2D hit in results)
        {
            PotionEffectReceiver receiver = hit.GetComponentInParent<PotionEffectReceiver>();
            if (receiver != null && receiver.TargetKind == PotionTargetKind.Player) return receiver.transform;
        }
        return null;
    }
}
