
/// <summary>
/// StageConnection은 두 개의 방 사이의 연결을 나타내는 구조체.
/// </summary>
public readonly struct StageConnection
{
    public int FromRoomIndex { get; }
    public int ToRoomIndex { get; }
    public ConnectorDirection FromDirection { get; }
    public ConnectorDirection ToDirection => FromDirection.OppositeDirection();

    /// <summary>
    /// StageConnection 구조체의 생성자. 두 개의 방 사이의 연결을 정의함.
    /// </summary>
    /// <param name="fromRoomIndex"> 연결의 시작 방 인덱스 </param>
    /// <param name="toRoomIndex"> 연결의 끝 방 인덱스 </param>
    /// <param name="fromDirection"> 연결의 시작 방향 </param>
    public StageConnection(int fromRoomIndex, int toRoomIndex, ConnectorDirection fromDirection)
    {
        FromRoomIndex = fromRoomIndex;
        ToRoomIndex = toRoomIndex;
        FromDirection = fromDirection;
    }
}