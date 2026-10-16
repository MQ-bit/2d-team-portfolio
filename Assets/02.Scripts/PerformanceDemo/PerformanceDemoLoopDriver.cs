using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

// Scenario orchestration only. Movement, attack, damage, combinations and wave
// completion are performed by the production components, never by this driver.
public sealed class PerformanceDemoLoopDriver : MonoBehaviour
{
    sealed class EnemyObservation
    {
        public EnemyStatus status;
        public Vector2 previous;
        public float previousHealth;
        public Action<float, float> health;
        public Action death;
        public Action<WaveEnemyHandle> release;
        public Action<PotionDamageResult> potion;
        public WaveEnemyHandle handle;
        public PotionEffectReceiver receiver;
    }
    PerformanceDemoScenario _scenario;
    PerformanceDemoRecorder _recorder;
    PerformanceDemoPotionDriver _potions;
    PlayerStatus _player;
    PlayerController _controller;
    PlayerThrowingModule _throw;
    PlayerMeleeModule _melee;
    PlayerMovementModule _move;
    PlayerInventoryModule _inventory;
    InputReader _input;
    PotionRunCatalog _catalog;
    WaveController _wave;
    SpawnManager _spawn;
    Transform _enemyRoot;
    readonly List<Transform> _points = new();
    readonly List<EnemyObservation> _enemies = new();
    readonly HashSet<ulong> _combinedPairs = new();
    readonly List<PotionProjectile> _projectiles = new();
    float _started, _nextAction, _nextDash, _completedAt, _observeAt;
    Vector2 _previousPlayer;
    int _nextPair, _lastWave = -1, _throwStep;
    bool _running, _startedWork, _warmup, _interleavedMelee;
    Vector2 _comboTarget;
    public PerformanceDemoRecorder.LoopResult Result { get; private set; }
    public string Error { get; private set; }
    public int ActiveEnemies { get; private set; }
    bool AcceptedDefeat => IsCombat && _scenario.AllowPlayerDeath && _player != null && _player.IsDead;
    public bool ReadyToFinish => _scenario != null && IsCombat && (AcceptedDefeat ||
        (Result.combatCompleted && Time.time > _completedAt + .3f && Time.time - _started > 5));
    bool IsCombat => _scenario.Kind == PerformanceDemoKind.Combat || _scenario.Kind == PerformanceDemoKind.Lifecycle;
    static readonly ProfilerMarker InputMarker = new("PerfDemo.PlayerInput");
    static readonly ProfilerMarker SpawnMarker = new("PerfDemo.SpawnWave");

    public void Setup(PerformanceDemoScenario scenario, GameObject playerPrefab, PotionRunCatalog catalog,
        PerformanceDemoPotionDriver potions, PerformanceDemoRecorder recorder, int repetition)
    {
        _scenario = scenario; _catalog = catalog; _potions = potions; _recorder = recorder;
        Result = new PerformanceDemoRecorder.LoopResult
        {
            scenarioName = scenario.name,
            repetition = repetition,
            cycles = scenario.Kind == PerformanceDemoKind.Lifecycle ? scenario.LifecycleCycles : 1,
            configuredEnemies = scenario.EnemyCount,
            configuredWaves = scenario.WaveCount,
            effectiveSeed = scenario.Seed + repetition - 1,
            firstPotion = scenario.Potion.name,
            secondPotion = scenario.SecondPotion != null ? scenario.SecondPotion.name : "",
            showDemoUI = scenario.ShowDemoUI,
            diagnosticHighHealth = scenario.DiagnosticHighHealth
        };
        Error = null; _nextPair = _throwStep = 0; _lastWave = -1; _startedWork = _interleavedMelee = false; _completedAt = 0; _warmup = true;
        ActiveEnemies = 0; _combinedPairs.Clear();
        if (SpawnManager.Instance != null) throw new InvalidOperationException("A foreign SpawnManager is present; use the isolated demo scene.");
        _spawn = new GameObject("DemoSpawnManager").AddComponent<SpawnManager>();
        _enemyRoot = new GameObject("DemoEnemies").transform;
        Vector3 playerPosition = scenario.Kind == PerformanceDemoKind.Combine ? new Vector3(-24, -12, 0) :
            IsCombat ? new Vector3(-8, 0, 0) : new Vector3(-3, 0, 0);
        _player = Instantiate(playerPrefab, playerPosition, Quaternion.identity).GetComponent<PlayerStatus>();
        if (_player == null || _player.IsDead) throw new InvalidOperationException("Loop player did not initialize.");
        _controller = _player.GetComponent<PlayerController>();
        _throw = _player.GetComponent<PlayerThrowingModule>();
        _melee = _player.GetComponent<PlayerMeleeModule>();
        _move = _player.GetComponent<PlayerMovementModule>();
        _inventory = _player.GetComponent<PlayerInventoryModule>();
        _input = InputReader.CreateReplayReader();
        Result.playerData = _player.Data.name;
        _controller.UseInputSource(_input);
        _player.OnDamaged += OnPlayerDamaged;
        _throw.OnPotionThrown += OnPlayerThrow;
        _melee.OnAttackStarted += OnMelee;
        _melee.OnMeleeHit += OnMeleeHit;
        _move.OnDashStarted += OnDash;
        if (IsCombat)
        {
            if (!_inventory.TryTakeItem(catalog.GetPotionItemByData(scenario.Potion), 4) ||
                !_inventory.TryTakeItem(catalog.GetPotionItemByData(scenario.SecondPotion), 4))
            {
                throw new InvalidOperationException("Could not prepare finite combat inventory.");
            }
        }
        PotionAreaCombiner.CombinationCommitted += OnCombination;
        int pointCount = Mathf.Max(1, scenario.EnemyCount);
        for (int i = 0; i < pointCount; i++)
        {
            var point = new GameObject("DemoSpawnPoint_" + i).transform;
            float angle = 2 * Mathf.PI * i / pointCount;
            float radius = scenario.Kind == PerformanceDemoKind.ContactEffects ? 2 : scenario.DenseSpawn ? 3 : 6;
            point.position = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * .5f, 0);
            _points.Add(point);
        }
        if (!IsCombat && scenario.EnemyCount > 0) SpawnEnemies();
        if (IsCombat)
        {
            _wave = new GameObject("DemoWaveController").AddComponent<WaveController>();
            _wave.EnemySpawned += ObserveEnemy;
            _wave.CombatCompleted += OnCombatCompleted;
            _wave.CombatFailed += OnCombatFailed;
        }
        _previousPlayer = _player.transform.position;
    }
    public void Begin(bool warmup)
    {
        _warmup = warmup; _running = true;
        _started = Time.time; _nextAction = _started + .5f; _nextDash = _started + 3;
        _observeAt = 0;
        if (!warmup) _recorder.Current.gameLoops.Add(Result);
    }
    void SpawnEnemies()
    {
        using (SpawnMarker.Auto())
        {
            for (int i = 0; i < _scenario.EnemyCount; i++)
            {
                GameObject prefab = _scenario.EnemyPrefabs[i % _scenario.EnemyPrefabs.Length];
                if (!_spawn.TrySpawn(new EnemySpawnRequest(prefab.GetComponent<WaveEnemyHandle>()), _points[i], _enemyRoot, out var handle))
                    throw new InvalidOperationException("Enemy spawn failed: " + prefab.name);
                ObserveEnemy(handle);
            }
        }
    }
    void BeginWaves()
    {
        var waves = new List<ResolvedWave>();
        for (int w = 0; w < _scenario.WaveCount; w++)
        {
            var requests = new List<EnemySpawnRequest>();
            for (int i = 0; i < _scenario.EnemyCount; i++)
                requests.Add(new EnemySpawnRequest(_scenario.EnemyPrefabs[i % _scenario.EnemyPrefabs.Length].GetComponent<WaveEnemyHandle>()));
            waves.Add(new ResolvedWave(requests, _scenario.EnemyCount, _scenario.DenseSpawn ? 0 : .08f, .2f));
        }
        using (SpawnMarker.Auto())
        {
            if (!_wave.TryStartEncounter(new ResolvedWavePlan(waves, .7f), _points, _enemyRoot))
                throw new InvalidOperationException("WaveController refused encounter.");
        }
    }
    void Update()
    {
        if (!_running || Error != null || _player == null) return;
        try
        {
            float elapsed = Time.time - _started;
            if (!_startedWork && elapsed >= .3f)
            {
                _startedWork = true;
                if (IsCombat) BeginWaves();
                else if (_scenario.Kind == PerformanceDemoKind.ContactEffects && _scenario.CombinationPairs == 0)
                    CreateContactArea();
            }
            if (!_startedWork) return;
            if (_player.IsDead && !_scenario.AllowPlayerDeath) throw new InvalidOperationException("Loop player died.");
            if (!IsCombat && _nextPair < _scenario.CombinationPairs &&
                elapsed >= .5f + _nextPair * _scenario.PairInterval)
            {
                // Delayed work is spread across frames instead of catching up in
                // one artificial burst. PairInterval=0 intentionally bursts.
                int count = _scenario.PairInterval == 0 ? _scenario.CombinationPairs : _nextPair + 1;
                while (_nextPair < count) LaunchPair(_nextPair++);
            }
            if (IsCombat) CombatInput();
            if (_wave != null && _wave.CurrentWaveIndex >= 0 && _wave.CurrentWaveIndex != _lastWave)
            {
                _lastWave = _wave.CurrentWaveIndex; Result.wavesStarted++;
                Record("WaveStarted index=" + _lastWave);
            }
            if (Time.time >= _observeAt)
            {
                _observeAt = Time.time + .2f;
                ActiveEnemies = 0;
                foreach (var e in _enemies)
                {
                    if (e.status == null || !e.status.gameObject.activeInHierarchy) continue;
                    Vector2 current = e.status.transform.position;
                    Result.enemyDistance += Vector2.Distance(e.previous, current); e.previous = current;
                    if (!e.status.IsDead) ActiveEnemies++;
                }
                Result.peakEnemies = Mathf.Max(Result.peakEnemies, ActiveEnemies);
            }
        }
        catch (Exception exception) { Error = exception.Message; }
    }
    void FixedUpdate()
    {
        if (!_running || Error != null || _player == null || _player.IsDead) return;
        Vector2 current = _player.transform.position;
        Result.playerDistance += Vector2.Distance(_previousPlayer, current); _previousPlayer = current;
        Vector2 target = new(-3 + Mathf.Sin((Time.time - _started) * .6f) * 2, Mathf.Cos((Time.time - _started) * .6f));
        if (IsCombat)
        {
            float phase = (Time.time - _started) * .6f + Mathf.PI;
            target = new Vector2(Mathf.Cos(phase) * 8, Mathf.Sin(phase) * 4);
            if (_throwStep >= 4)
            {
                float nearest = float.MaxValue;
                foreach (var e in _enemies)
                {
                    if (e.status == null || !e.status.gameObject.activeInHierarchy || e.status.IsDead) continue;
                    float distance = ((Vector2)e.status.transform.position - current).sqrMagnitude;
                    if (distance < nearest) { nearest = distance; target = e.status.transform.position; }
                }
                if (nearest < 2) target = current; // stay in melee range rather than push through the enemy
            }
            foreach (var e in _enemies)
            {
                if (e.status != null && e.status.gameObject.activeInHierarchy &&
                    e.status.TryGetComponent<ExplodingEnemyController>(out var bomb) && bomb.IsFusing &&
                    Vector2.Distance(current, e.status.transform.position) < bomb.ExplosionRadius + 1)
                {
                    target += (current - (Vector2)e.status.transform.position).normalized * 4;
                }
            }
        }
        Vector2 movement = _scenario.MovePlayer ? Vector2.ClampMagnitude((target - current) * 2, 1) : Vector2.zero;
        using (InputMarker.Auto()) _input.ReplayState(movement, AimScreen(current + Vector2.right));
    }
    Vector2 AimScreen(Vector2 world) => Camera.main.WorldToScreenPoint(new Vector3(world.x, world.y, 0));
    void CombatInput()
    {
        if (Time.time < _nextAction || _player.IsDead) return;
        _nextAction = Time.time + .65f;
        Result.inputAttempts++;
        using (InputMarker.Auto())
        {
            if (_throwStep < 4 && !(_throwStep == 2 && !_interleavedMelee))
            {
                if (_throwStep % 2 == 0) _comboTarget = (Vector2)_player.transform.position + (-(Vector2)_player.transform.position).normalized * 4;
                if (_melee.IsMeleeModeActivated) _input.ReplayToggleMelee();
                _input.ReplayState(Vector2.zero, AimScreen(_comboTarget));
                var potion = _throwStep % 2 == 0 ? _scenario.Potion : _scenario.SecondPotion;
                for (int i = 0; i < _inventory.SlotCount; i++)
                    if (_inventory.GetItemAt(i).PotionData == potion) { _input.ReplaySelectSlot(i); break; }
                _input.ReplayPrimary(true); _input.ReplayPrimary(false); _throwStep++;
            }
            else
            {
                if (_throwStep == 2) _interleavedMelee = true;
                Vector2 aim = (Vector2)_player.transform.position + Vector2.right;
                float nearest = float.MaxValue;
                foreach (var e in _enemies)
                {
                    if (e.status == null || !e.status.gameObject.activeInHierarchy || e.status.IsDead) continue;
                    float distance = ((Vector2)e.status.transform.position - (Vector2)_player.transform.position).sqrMagnitude;
                    if (distance < nearest) { nearest = distance; aim = e.status.transform.position; }
                }
                _input.ReplayState(Vector2.zero, AimScreen(aim));
                if (!_melee.IsMeleeModeActivated) _input.ReplayToggleMelee();
                _input.ReplayPrimary(true); _input.ReplayPrimary(false);
            }
            if (_scenario.MovePlayer && Time.time >= _nextDash)
            {
                _nextDash = Time.time + 5;
                _input.ReplayState(Vector2.up, AimScreen(_player.transform.position)); _input.ReplayDash();
            }
        }
    }
    void LaunchPair(int pair)
    {
        float x = (pair % 5 - (Mathf.Min(5, _scenario.CombinationPairs) - 1) * .5f) * 9;
        float y = (pair / 5) * 5 - (_scenario.CombinationPairs > 5 ? 2.5f : 0);
        Vector2 target = _scenario.Arena == PerformanceDemoArena.OpenFloor ? new Vector2(x, y) : new Vector2(-2, 0);
        Launch(_scenario.Potion, target, target + Vector2.left * 3);
        Launch(_scenario.SecondPotion, target + Vector2.right * (_scenario.NonOverlappingControl ? 9 : .6f), target + Vector2.left * 3);
    }
    void Launch(PotionData potion, Vector2 target, Vector2 origin)
    {
        var projectile = Instantiate(_potions.ProjectilePrefab, origin, Quaternion.identity);
        projectile.Initiate(_catalog.GetPotionItemByData(potion), _catalog, _player);
        projectile.Launch(target, .6f, 2.5f, 480);
        _projectiles.Add(projectile); Result.projectilesLaunched++;
        Record("ProjectileLaunch pair=" + _nextPair + "; element=" + potion.Element);
    }
    void CreateContactArea()
    {
        var area = Instantiate(_potions.AreaPrefab, Vector3.zero, Quaternion.identity);
        area.Initialize(_scenario.Potion, LayerMask.GetMask("Wall")); area.SetOwner(_player); area.SetCatalog(_catalog); area.Activate();
        Record("ContactAreaActivated");
    }
    void OnCombination(PotionAreaController a, PotionAreaController b, PotionCombinationData data)
    {
        if (!_running || a.gameObject.scene != _player.gameObject.scene) return;
        uint x = unchecked((uint)a.GetInstanceID()), y = unchecked((uint)b.GetInstanceID());
        ulong key = ((ulong)Math.Min(x, y) << 32) | Math.Max(x, y);
        if (!_combinedPairs.Add(key)) { Error = "Duplicate combination commit for a pair."; return; }
        Result.combinations++; Record("CombinationCommitted pair=" + key + "; profile=" + data.name);
    }
    void ObserveEnemy(WaveEnemyHandle handle)
    {
        foreach (var existing in _enemies)
        {
            if (existing.handle == handle)
            {
                existing.previous = handle.transform.position;
                existing.previousHealth = existing.status.CurrentHealth;
                Result.enemiesSpawned++; ActiveEnemies++;
                Result.peakEnemies = Mathf.Max(Result.peakEnemies, ActiveEnemies);
                Record("EnemyRespawn id=" + handle.GetInstanceID());
                return;
            }
        }

        var status = handle.GetComponent<EnemyStatus>();
        var observation = new EnemyObservation
        {
            status = status,
            handle = handle,
            previous = handle.transform.position,
            receiver = handle.GetComponent<PotionEffectReceiver>()
        };
        observation.previousHealth = status.CurrentHealth;
        observation.health = (current, max) => { if (current < observation.previousHealth) Result.enemyDamageEvents++; observation.previousHealth = current; };
        observation.death = () => { Result.enemyDeaths++; Record("EnemyDeath id=" + handle.GetInstanceID()); };
        observation.release = released => { Result.enemyReleases++; ActiveEnemies = Mathf.Max(0, ActiveEnemies - 1); };
        observation.potion = damage => { if (damage.ActualDamage > 0) Result.potionDamageEvents++; };
        status.OnHealthChanged += observation.health; status.OnDeath += observation.death;
        handle.Released += observation.release;
        if (observation.receiver != null) observation.receiver.DamageApplied += observation.potion;
        _enemies.Add(observation); Result.enemiesSpawned++; ActiveEnemies++;
        Result.peakEnemies = Mathf.Max(Result.peakEnemies, ActiveEnemies);
        Record("EnemySpawn id=" + handle.GetInstanceID());
    }
    void OnPlayerThrow(Vector2 target) { Result.playerThrows++; Result.inputAccepted++; Record("PlayerThrow target=" + target); }
    void OnPlayerDamaged(float amount, Vector2 direction) { Result.playerDamageEvents++; }
    void OnMelee(Vector2 direction) { Result.meleeAttacks++; Result.inputAccepted++; }
    void OnMeleeHit(Vector2 direction, bool killed) { Result.meleeHits++; }
    void OnDash(float cooldown) { Result.dashes++; }
    void OnCombatCompleted() { Result.combatCompleted = true; _completedAt = Time.time; Result.combatCompletedAt = Time.time - _started; Record("CombatCompleted"); }
    void OnCombatFailed(string reason) { Error = reason; }
    void Record(string detail) { if (!_warmup) _recorder.Record(_scenario, Result.repetition, "Measure", detail, ActiveEnemies); }
    public string ValidateResult()
    {
        string failure = Error;
        if (failure == null && _scenario.MovePlayer && Result.playerDistance < _scenario.MinimumPlayerDistance) failure = "Player did not move the required distance.";
        int expectedPairs = _scenario.NonOverlappingControl ? 0 : IsCombat ? 1 : _scenario.CombinationPairs;
        if (failure == null && Result.combinations < expectedPairs) failure = "Expected combinations were not committed.";
        if (failure == null && _scenario.NonOverlappingControl && Result.combinations != 0) failure = "Control unexpectedly combined.";
        if (failure == null && !IsCombat && Result.projectilesLaunched != _scenario.CombinationPairs * 2) failure = "Not all pair projectiles launched.";
        if (failure == null && _scenario.EnemyCount > 0 && Result.enemiesSpawned < _scenario.EnemyCount) failure = "Expected enemies were not spawned.";
        if (failure == null && _scenario.Kind == PerformanceDemoKind.Chase && (Result.enemyDistance < 1 || ActiveEnemies != _scenario.EnemyCount)) failure = "Chase load did not persist or enemies did not move.";
        if (failure == null && _scenario.Kind == PerformanceDemoKind.ContactEffects && (Result.potionDamageEvents == 0 || Result.enemyDamageEvents == 0)) failure = "No actual potion damage was observed.";
        if (failure == null && IsCombat && !AcceptedDefeat && (!Result.combatCompleted || Result.wavesStarted != _scenario.WaveCount || Result.playerThrows < 2 || Result.meleeAttacks == 0 || Result.enemyReleases == 0 || Result.enemyDamageEvents == 0)) failure = "Combat did not complete the required waves/actions.";
        if (failure == null && AcceptedDefeat && (Result.enemiesSpawned < _scenario.EnemyCount || Result.playerDamageEvents == 0 || Result.playerThrows < 2 || Result.combinations == 0)) failure = "Defeat occurred before the configured load/required interactions were reached.";
        Result.outcome = failure != null ? "Failed" : AcceptedDefeat ? "PlayerDefeated" : IsCombat ? "CombatCompleted" : "IntervalCompleted";
        Result.functionalValid = failure == null; Result.failure = failure;
        return failure;
    }
    public void Clear()
    {
        if (_running && Result != null && Result.outcome == "Running")
        {
            Result.outcome = Error == null ? "Interrupted" : "Failed";
            Result.failure = Error ?? "Execution ended before functional validation.";
            Result.functionalValid = false;
        }
        _running = false;
        PotionAreaCombiner.CombinationCommitted -= OnCombination;
        if (_wave != null) { _wave.EnemySpawned -= ObserveEnemy; _wave.CombatCompleted -= OnCombatCompleted; _wave.CombatFailed -= OnCombatFailed; _wave.CancelEncounter(false); }
        foreach (var e in _enemies)
        {
            if (e.status != null) { e.status.OnHealthChanged -= e.health; e.status.OnDeath -= e.death; }
            if (e.handle != null) e.handle.Released -= e.release;
            if (e.receiver != null) e.receiver.DamageApplied -= e.potion;
        }
        if (_throw != null) _throw.OnPotionThrown -= OnPlayerThrow;
        if (_melee != null) { _melee.OnAttackStarted -= OnMelee; _melee.OnMeleeHit -= OnMeleeHit; }
        if (_move != null) _move.OnDashStarted -= OnDash;
        if (_controller != null) _controller.CancelAllActions();
        if (_player != null) _player.OnDamaged -= OnPlayerDamaged;
        if (_input != null) Destroy(_input);
        _input = null; _player = null; _enemies.Clear(); _points.Clear(); _projectiles.Clear();
    }
    void OnDisable() => Clear();
}
