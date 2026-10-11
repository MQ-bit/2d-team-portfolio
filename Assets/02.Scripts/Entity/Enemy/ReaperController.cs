using System.Collections.Generic;
using UnityEngine;

public sealed class ReaperController : EnemyControllerBase
{
    public enum ActionState { Idle, Walk, Melee, Charge, Dash, Cast, Recovery, Death, Steal }
    [SerializeField] private ReaperPotionDrop _potionDropPrefab;
    [SerializeField] private ReaperAttackIndicator _attackIndicatorPrefab;
    [SerializeField] private ReaperPotionSteal _potionStealPrefab;
    private ReaperPotionSteal _stealEffect;
    private Transform _stealTarget;
    private PlayerInventoryModule _stealInventory;
    private Vector2 _stealPosition;
    private PotionItem _stolenPotion;
    private ReaperAttackIndicator _dashIndicator;
    private ReaperData Settings => Data as ReaperData;
    protected override float DeathDuration => Settings.DeathDuration;
    public ActionState Action { get; private set; }
    public ActionState RecoveryFrom { get; private set; }
    public Vector2 Facing { get; private set; } = Vector2.left;
    private float _remaining;
    private float _meleeReady, _dashReady, _summonReady, _stealReady;
    private float _dashRemaining;
    private Vector2 _summonPosition;
    private readonly List<Collider2D> _hits = new();
    private readonly HashSet<PlayerStatus> _damaged = new();
    private readonly List<ReaperPotionDrop> _drops = new();

    protected override void Awake()
    {
        base.Awake();
        if (Settings == null) { Debug.LogError($"{name}: ReaperData를 연결하세요.", this); enabled = false; }
        else
        {
            SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>();
            Settings.ApplyVisual(sprite);
            if (sprite != null)
            {
                PotionStatusVisual statusVisual = GetComponent<PotionStatusVisual>();
                if (statusVisual == null) statusVisual = gameObject.AddComponent<PotionStatusVisual>();
                Vector3 scale = sprite.transform.localScale;
                // 원본 몸 영역(약 30x48px), 기준점(105,18px)에 맞춘 몸 중심(105,24px).
                // 낫과 베기 이펙트까지 포함되는 Renderer.bounds를 사용하지 않는다.
                statusVisual.ConfigureBodyBounds(new Vector3(0f, 0.06f * Mathf.Abs(scale.y), 0f),
                    new Vector2(0.3f * Mathf.Abs(scale.x), 0.48f * Mathf.Abs(scale.y)));
            }
            ResetBehaviour();
        }
    }

    private void FixedUpdate()
    {
        if (IsDead || Settings == null) return;
        if (!CanAct) { Movement.Stop(); return; }
        switch (Action)
        {
            case ActionState.Melee:
                Movement.Stop();
                float beforeTick = _remaining;
                bool meleeFinished = Tick();
                // 준비 후 베기 구간 동안 검사. HashSet은 공격 종료까지 유지한다.
                if (_remaining <= Settings.MeleeActiveDuration && beforeTick > 0f)
                {
                    float side = Facing.x > 0f ? 1f : -1f;
                    Vector2 center = Body.position + new Vector2(Settings.MeleeHitboxOffset.x * side, Settings.MeleeHitboxOffset.y);
                    ReaperAttackDamage.ApplyMelee(Body.position, center, Settings.MeleeHitboxSize,
                        Settings.MeleeDamage, Facing, _hits, _damaged);
                }
                if (meleeFinished) Recover(Settings.MeleeRecovery);
                return;
            case ActionState.Charge:
                Movement.Stop();
                if (Tick())
                {
                    HideDashIndicator();
                    Action = ActionState.Dash;
                    _dashRemaining = Settings.DashDistance;
                    SoundManager.Play(GameSoundId.ReaperDash);
                }
                return;
            case ActionState.Dash:
                UpdateDash();
                return;
            case ActionState.Steal:
                Movement.Stop();
                bool finished = Tick();
                if (_stealEffect != null) _stealEffect.SetProgress(1f - _remaining / Settings.StealWindup);
                if ((_remaining <= Settings.StealActiveDuration && CanHitStealTarget()) || finished)
                    FinishSteal();
                return;
            case ActionState.Cast:
                Movement.Stop();
                if (Tick())
                {
                    if (_potionDropPrefab != null)
                    {
                        _drops.RemoveAll(drop => drop == null);
                        ReaperPotionDrop drop = Instantiate(_potionDropPrefab, _summonPosition, Quaternion.identity);
                        drop.SetOwner(Status);
                        if (!_stolenPotion.IsEmpty)
                        {
                            drop.SetPotion(_stolenPotion);
                            _stolenPotion = default;
                        }
                        drop.Initialize(Settings);
                        _drops.Add(drop);
                    }
                    Recover(Settings.SummonRecovery);
                }
                return;
            case ActionState.Recovery:
                Movement.Stop();
                if (Tick()) Action = ActionState.Idle;
                return;
        }

        Transform target = FindPlayer();
        if (target == null) { Action = ActionState.Idle; Movement.Stop(); return; }
        Vector2 offset = (Vector2)target.position - Body.position;
        if (offset.sqrMagnitude > 0.0001f) Facing = offset.normalized;
        float distance = offset.magnitude;
        bool visible = EnemyPathfinder.Instance.HasLineOfSight(Body.position, target.position);
        // 근접 우선. 중거리 강탈, 먼 거리 소환, 돌진을 각 쿨타임에 따라 선택한다.
        if (Settings.UseMeleeAttack && visible && distance <= Settings.MeleeRange && Time.fixedTime >= _meleeReady)
        {
            _meleeReady = Time.fixedTime + Settings.MeleeCooldown;
            Begin(ActionState.Melee, Settings.MeleeWindup + Settings.MeleeActiveDuration);
        }
        else if (Settings.UsePotionSteal && visible && _potionStealPrefab != null && _stolenPotion.IsEmpty
            && distance >= Settings.StealMinRange && distance <= Settings.StealRange && Time.fixedTime >= _stealReady)
        {
            _stealReady = Time.fixedTime + Settings.StealCooldown;
            _stealTarget = target;
            _stealInventory = target.GetComponentInParent<PlayerInventoryModule>();
            _stealPosition = target.position;
            Begin(ActionState.Steal, Settings.StealWindup);
            if (_stealEffect != null) Destroy(_stealEffect.gameObject);
            _stealEffect = Instantiate(_potionStealPrefab, _stealPosition, Quaternion.identity);
            _stealEffect.Initialize(Settings);
        }
        else if (Settings.UsePotionSummon && visible && _potionDropPrefab != null && distance >= Settings.SummonMinRange && distance <= Settings.SummonRange
            && Time.fixedTime >= _summonReady)
        {
            _summonReady = Time.fixedTime + Settings.SummonCooldown;
            _summonPosition = target.position; // 시전 시작 순간에 고정, 이후 플레이어를 따라가지 않는다.
            Begin(ActionState.Cast, Settings.SummonWindup);
        }
        else if (Settings.UseDashAttack && visible && distance >= Settings.DashMinRange && distance <= Settings.DashMaxRange
            && Time.fixedTime >= _dashReady)
        {
            _dashReady = Time.fixedTime + Settings.DashCooldown;
            Begin(ActionState.Charge, Settings.DashWindup);
        }
        else if (visible && Settings.PreferredDistance > 0f)
        {
            if (Mathf.Abs(distance - Settings.PreferredDistance) <= Settings.DistanceTolerance)
            {
                Action = ActionState.Idle;
                Movement.Stop();
            }
            else
            {
                Action = ActionState.Walk;
                Vector2 away = distance > 0.001f ? -offset / distance : Vector2.left;
                Movement.ChaseTarget((Vector2)target.position + away * Settings.PreferredDistance, Status.Speed);
            }
        }
        else { Action = ActionState.Walk; Movement.ChaseTarget(target.position, Status.Speed); }
    }

    private bool Tick() { _remaining -= Time.fixedDeltaTime; return _remaining <= 0f; }
    private bool CanHitStealTarget()
    {
        PlayerStatus player = _stealTarget != null ? _stealTarget.GetComponent<PlayerStatus>() : null;
        return player != null && !player.IsDead && _stealEffect != null
            && ReaperPotionTheft.IsInsideWarning(_stealPosition, _stealTarget.position, Settings.StealAreaRadius)
            && EnemyPathfinder.Instance.HasLineOfSight(Body.position, _stealTarget.position);
    }

    private void FinishSteal()
    {
        PlayerStatus player = _stealTarget != null ? _stealTarget.GetComponent<PlayerStatus>() : null;
        bool hit = CanHitStealTarget();
        if (hit && ReaperPotionTheft.TrySteal(_stealInventory, out PotionItem stolen))
        {
            _stolenPotion = stolen;
            _stealEffect.PlayCapture(stolen, _stealTarget.position, transform);
            if (Settings.StealDamage > 0f)
                player.DecreaseHealth(Settings.StealDamage, ((Vector2)_stealTarget.position - Body.position).normalized);
        }
        else
        {
            if (hit && !ReaperPotionTheft.HasCandidate(_stealInventory))
                player.DecreaseHealth(Settings.StealEmptyDamage, ((Vector2)_stealTarget.position - Body.position).normalized);
            CancelSteal();
        }
        _stealTarget = null;
        _stealInventory = null;
        Recover(Settings.StealRecovery);
    }

    private void CancelSteal()
    {
        if (_stealEffect != null)
        {
            _stealEffect.gameObject.SetActive(false);
            Destroy(_stealEffect.gameObject);
        }
        _stealEffect = null;
        _stealTarget = null;
        _stealInventory = null;
    }
    private void LateUpdate()
    {
        if (_dashIndicator == null || Action != ActionState.Charge) return;
        float length = Settings.DashDistance;
        float radius = Movement.BodyRadius;
        // 실제 돌진의 벽 검사와 같은 조건으로 경고 경로를 잘라낸다.
        if (!EnemyPathfinder.Instance.HasLineOfSight(Body.position, Body.position + Facing * (length + radius)))
        {
            float low = 0f, high = length;
            for (int i = 0; i < 10; i++)
            {
                float middle = (low + high) * 0.5f;
                if (EnemyPathfinder.Instance.HasLineOfSight(Body.position, Body.position + Facing * (middle + radius)))
                    low = middle;
                else high = middle;
            }
            length = low;
        }
        _dashIndicator.SetDash(Body.position, Facing, length, radius + 0.1f);
        _dashIndicator.SetProgress(Settings.DashWindup > 0f ? 1f - _remaining / Settings.DashWindup : 1f);
    }

    private void HideDashIndicator()
    {
        if (_dashIndicator != null)
        {
            _dashIndicator.gameObject.SetActive(false);
            Destroy(_dashIndicator.gameObject);
        }
        _dashIndicator = null;
    }
    private void OnDrawGizmosSelected()
    {
        if (Settings == null) return;
        float side = Facing.x > 0f ? 1f : -1f;
        Vector3 center = transform.position + new Vector3(Settings.MeleeHitboxOffset.x * side, Settings.MeleeHitboxOffset.y, 0f);
        Gizmos.color = new Color(0.85f, 0.2f, 0.45f, 1f);
        Gizmos.DrawWireCube(center, new Vector3(Settings.MeleeHitboxSize.x, Settings.MeleeHitboxSize.y, 0f));
    }
    private void Begin(ActionState state, float duration)
    {
        Action = state;
        _remaining = duration;
        _damaged.Clear();
        Movement.Stop();
        HideDashIndicator();
        if (state == ActionState.Charge && _attackIndicatorPrefab != null)
        {
            _dashIndicator = Instantiate(_attackIndicatorPrefab);
            _dashIndicator.ApplyPalette(Settings);
            _dashIndicator.SetDash(Body.position, Facing, Settings.DashDistance, Movement.BodyRadius + 0.1f);
        }
        if (state == ActionState.Cast) SoundManager.Play(GameSoundId.ReaperPotionSummon);
        else if (state == ActionState.Steal) SoundManager.Play(GameSoundId.ReaperPotionSteal);
        else if (state == ActionState.Melee) SoundManager.Play(GameSoundId.ReaperAttack);
        else if (state == ActionState.Charge) SoundManager.Play(GameSoundId.ReaperDashCharge);
    }
    private void Recover(float duration)
    {
        RecoveryFrom = Action;
        Action = ActionState.Recovery;
        _remaining = duration;
        Movement.Stop();
    }
    private void UpdateDash()
    {
        float step = Mathf.Min(_dashRemaining, Settings.DashSpeed * Time.fixedDeltaTime);
        Vector2 destination = Body.position + Facing * step;
        // 벽을 뚫거나 벽에 계속 밀착해 돌진하지 않는다.
        if (!EnemyPathfinder.Instance.HasLineOfSight(Body.position,
            destination + Facing * Movement.BodyRadius)) { Recover(Settings.DashRecovery); return; }
        Movement.MoveTowards(destination, Settings.DashSpeed);
        _dashRemaining -= step;
        ReaperAttackDamage.Apply(Body.position, Movement.BodyRadius + 0.1f, Settings.DashDamage,
            Facing, 360f, _hits, _damaged, true);
        if (_dashRemaining <= 0f) Recover(Settings.DashRecovery);
    }

    protected override void OnDied() { CancelSteal(); HideDashIndicator(); CancelDrops(); Action = ActionState.Death; }
    protected override void OnDisable() { CancelSteal(); HideDashIndicator(); CancelDrops(); base.OnDisable(); }
    private void CancelDrops()
    {
        foreach (ReaperPotionDrop drop in _drops) if (drop != null) Destroy(drop.gameObject);
        _drops.Clear();
    }
    protected override void ResetBehaviour()
    {
        CancelSteal();
        _stolenPotion = default;
        HideDashIndicator();
        CancelDrops();
        Action = ActionState.Idle;
        RecoveryFrom = ActionState.Idle;
        Facing = Vector2.left;
        _remaining = _dashRemaining = 0f;
        _meleeReady = _dashReady = Time.fixedTime;
        _summonReady = Time.fixedTime + 2f;
        _stealReady = Time.fixedTime + 4f;
        _damaged.Clear();
    }
}
