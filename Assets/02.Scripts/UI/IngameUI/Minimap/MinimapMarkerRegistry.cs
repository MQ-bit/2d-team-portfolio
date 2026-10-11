using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 현재 활성화된 미니맵 마커를 관리하는 Static class
/// </summary>
public static class MinimapMarkerRegistry
{
    private static readonly HashSet<MinimapMarker> Markers = new HashSet<MinimapMarker>();
    private static readonly List<MinimapMarker> StaleMarkers = new List<MinimapMarker>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // 런타임이 시작될 때, Awake보다 더 빠르게 실행되게 해주는 어트리뷰트
    private static void Reset()
    {
        Markers.Clear();
        StaleMarkers.Clear();
    }

    public static void Register(MinimapMarker marker)
    {
        if (marker != null)
        {
            Markers.Add(marker);
        }
    }

    public static void Unregister(MinimapMarker marker)
    {
        if (marker != null)
        {
            Markers.Remove(marker);
        }
    }

    public static void CopyActiveMarkersTo(List<MinimapMarker> destination)
    {
        destination.Clear();
        StaleMarkers.Clear();

        foreach (MinimapMarker marker in Markers)
        {
            if (marker == null)
            {
                StaleMarkers.Add(marker);
                continue;
            }

            if (marker.IsVisible)
            {
                destination.Add(marker);
            }
        }

        foreach (MinimapMarker marker in StaleMarkers)
        {
            Markers.Remove(marker);
        }
    }
}