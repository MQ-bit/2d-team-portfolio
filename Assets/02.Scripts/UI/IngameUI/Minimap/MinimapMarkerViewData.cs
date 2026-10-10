using UnityEngine;

public readonly struct MinimapMarkerViewData
{
    public int Id { get; }
    public MinimapMarkerType Type { get; }
    public Vector2 AnchoredPosition { get; }

    public MinimapMarkerViewData(int id, MinimapMarkerType type, Vector2 anchoredPosition)
    {
        Id = id;
        Type = type;
        AnchoredPosition = anchoredPosition;
    }
}