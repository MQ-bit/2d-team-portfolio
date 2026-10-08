public readonly struct EnemySpawnRequest
{
    public EnemyKind Kind { get; }
    public WaveEnemyHandle Prefab { get; }

    public EnemySpawnRequest(EnemyKind kind, WaveEnemyHandle prefab)
    {
        Kind = kind;
        Prefab = prefab;
    }
}