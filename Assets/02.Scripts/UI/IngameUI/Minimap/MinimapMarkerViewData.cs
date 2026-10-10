using UnityEngine;

/// <summary>
/// MinimapPresenter에서 계산한 미니맵 마커의 최종 표시 정보를 View에 전달하기 위한 읽기 전용 데이터 구조체.
/// </summary>
public readonly struct MinimapMarkerViewData
{
    public int Id { get; }
    public Vector2 AnchoredPosition { get; }
    public Sprite Icon { get; }
    public Color Color { get; }
    public float Size { get; }
    public int RenderPriority { get; }

    public MinimapMarkerViewData(int id, Vector2 anchoredPosition, Sprite icon, Color color, float size, int renderPriority)
    {
        Id = id;
        AnchoredPosition = anchoredPosition;
        Icon = icon;
        Color = color;
        Size = size;
        RenderPriority = renderPriority;
    }
}