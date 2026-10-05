using UnityEngine;

/// <summary>
/// 월드 공간에서 배치된 레이아웃의 정의, 위치, 경계 정보를 나타내는 구조체.
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct LayoutPlacement<T> where T : Component
{
    public T Definition { get; }    //안에는 RoomDefinition, HallwayDefinition이 들어올 수 있으므로 T로 제네릭화
    public Vector3 WorldPosition { get; }
    public Bounds WorldBounds { get; }

    public LayoutPlacement(T definition, Vector3 worldPosition, Bounds worldBounds)
    {
        Definition = definition;
        WorldPosition = worldPosition;
        WorldBounds = worldBounds;
    }
}