/// <summary>
/// Spawn을 원하는 Enemy를 감싸는 구조체
/// </summary>
public readonly struct EnemySpawnRequest //TODO: 불필요한 구조체일 수 있음. 제거 고려
{
    public WaveEnemyHandle Prefab { get; }

    public EnemySpawnRequest(WaveEnemyHandle prefab)
    {
        Prefab = prefab;
    }
}