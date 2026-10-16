using UnityEngine;

public enum PerformanceDemoKind { Baseline, Areas, Throw, Dash, Steal, Summon, Combine, Chase, ContactEffects, Combat, Lifecycle }
public enum PerformanceDemoArena { OpenFloor, SimpleWall, ComplexCorner, ReaperArena }

[CreateAssetMenu(menuName = "Performance/Demo Scenario")]
public sealed class PerformanceDemoScenario : ScriptableObject
{
    public string Id;
    public PerformanceDemoKind Kind;
    public PerformanceDemoArena Arena;
    [Min(1)] public int Count = 1;
    [Min(1)] public int Repetitions = 3;
    public int Seed = 1014;
    [Min(0)] public float WarmupSeconds = 5;
    [Min(0.1f)] public float MeasureSeconds = 20;
    public PotionData Potion;
    public GameObject ReaperPrefab;
    [TextArea] public string DataChanges;
    [Header("Game loop scenarios")]
    public PotionData SecondPotion;
    public GameObject[] EnemyPrefabs;
    [Min(0)] public int EnemyCount;
    [Min(0)] public int CombinationPairs;
    [Min(0)] public float PairInterval;
    [Min(1)] public int WaveCount = 2;
    [Min(1)] public int LifecycleCycles = 1;
    public bool MovePlayer = true;
    public bool DenseSpawn;
    public bool ShowDemoUI = true;
    public bool AllowPlayerDeath;
    public bool DiagnosticHighHealth;
    public bool NonOverlappingControl;
    [Min(0)] public float MinimumPlayerDistance = 1;
    public bool IsGameLoop => Kind >= PerformanceDemoKind.Combine;
}
