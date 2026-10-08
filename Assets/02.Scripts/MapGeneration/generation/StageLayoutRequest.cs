/// <summary>
/// StageLayoutPlanner에 전달되는 요청 정보를 나타내는 클래스. StartRoom은 시작 방의 정의를 나타내며, Profile은 스테이지 생성에 필요한 설정을 담고 있는 StageGenerationProfileSO를 나타냄.
public class StageLayoutRequest
{
    // 해당 클래스는 여러 Plan 메서드에 반복 전달되므로 구조체가 아닌 클래스로 정의
    public RoomDefinition StartRoom { get; }
    public StageGenerationProfileSO Profile { get; }
    public int Seed { get; }

    public StageLayoutRequest(RoomDefinition startRoom, StageGenerationProfileSO profile, int seed)
    {
        StartRoom = startRoom;
        Profile = profile;
        Seed = seed;
    }
}