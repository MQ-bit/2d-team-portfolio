public class GeneratedRoomNode //TODO : 널값 없으면 구조체를 쓰는 것도 참고
{
    public RoomDefinition Room { get; }
    public RoomConnector Entry { get; }
    public RoomConnector Exit { get; }
    public int Index { get; }

    public GeneratedRoomNode(RoomDefinition room, RoomConnector entry, RoomConnector exit, int index)
    {
        Room = room;
        Entry = entry;
        Exit = exit;
        Index = index;
    }
}