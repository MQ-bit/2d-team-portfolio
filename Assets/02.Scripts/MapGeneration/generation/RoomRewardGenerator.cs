using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class RoomRewardGenerator : MonoBehaviour
{
    private const int Reward_Random_Salt = 1597463007;

    [Header("References")]
    [SerializeField] private StageManager _stageManager;
    [SerializeField] private StageGenerationProfileSO _profile;

    private readonly HashSet<int> _rewardedRoomChecker = new HashSet<int>(); //보상을 받은 Room 체크용

    private GeneratedStage _trackedStage;
    private bool _isSubscribed;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
        ResetRuntime();
    }

    private void ResolveReferences()
    {
        if (_stageManager == null)
        {
            _stageManager = GetComponent<StageManager>();
            if (_stageManager == null)
            {
                _stageManager = StageManager.Instance;
            }
        }
    }

    private void Subscribe()
    {
        if (_isSubscribed || _stageManager == null)
        {
            return;
        }

        _stageManager.CombatRoomCleared += HandleCombatRoomCleared;
        _isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_isSubscribed)
        {
            return;
        }

        if (_stageManager != null)
        {
            _stageManager.CombatRoomCleared -= HandleCombatRoomCleared;
        }

        _isSubscribed = false;
    }

    private void ResetRuntime()
    {
        _trackedStage = null;
        _rewardedRoomChecker.Clear();
    }

    public bool TrySpawnInitialRewards(GeneratedStage stage, out string error)
    {
        if (stage?.RoomNodes == null)
        {
            error = "초기 보상을 생성할 Stage가 없습니다.";
            return false;
        }

        foreach (GeneratedRoomNode node in stage.RoomNodes)
        {
            if (node?.Room == null)
            {
                continue;
            }

            RoomType type = node.Room.Type;

            if (type != RoomType.Start)
            {
                continue;
            }

            if (!TrySpawnRoomRewards(stage, node, out error))
            {
                return false;
            }
        }

        error = null;
        return true;
    }

    private void HandleCombatRoomCleared(GeneratedRoomNode node)
    {
        if (node?.Room == null)
        {
            return;
        }

        RoomType type = node.Room.Type;

        if (type != RoomType.Normal && type != RoomType.Elite)
        {
            return;
        }

        if (!TrySpawnRoomRewards(_stageManager?.CurrentStage, node, out string error))
        {
            Debug.LogWarning(error, this);
        }
    }

    private bool TrySpawnRoomRewards(GeneratedStage stage, GeneratedRoomNode node, out string error)
    {
        error = null;

        if (stage?.TransientsRoot == null || node?.Room == null || _profile == null)
        {
            error = "보상 생성에 필요한 Stage, Room 또는 Profile이 없습니다.";
            return false;
        }

        if (!ReferenceEquals(_trackedStage, stage))
        {
            _trackedStage = stage;
            _rewardedRoomChecker.Clear();
        }

        if (_rewardedRoomChecker.Contains(node.Index)) return true;

        RoomDefinition room = node.Room;
        System.Random random = CreateRoomRandom(stage, node);

        bool isInitialReward = room.Type == RoomType.Start || room.Type == RoomType.End;

        int rewardCount;

        switch (room.Type)
        {
            case RoomType.Start:
            case RoomType.End:
                {
                    rewardCount = 1;
                    break;
                }
            case RoomType.Normal:
                {
                    rewardCount = random.Next(0, 4);
                    break;
                }
            case RoomType.Elite:
                {
                    rewardCount = random.Next(2, 6);
                    break;
                }
            default:
                {
                    error = $"{room.name}: 지원하지 않는 방 타입입니다.";
                    return false;
                }
        }

        if (rewardCount == 0)
        {
            _rewardedRoomChecker.Add(node.Index);
            return true;
        }

        InteractablePotionBox prefab = _profile.GetRewardBoxPrefab(room.Type);

        if (prefab == null)
        {
            error = $"{room.name}: 보상 상자 프리팹이 없습니다.";
            return false;
        }

        IReadOnlyList<Transform> points = room.RewardSpawnPoints;

        if (points == null || points.Count == 0)
        {
            error = $"{room.name}: RewardSpawnPoint가 없습니다.";
            return false;
        }

        BoxCollider2D prefabCollider = prefab.GetComponent<BoxCollider2D>();

        if (isInitialReward)
        {
            if (points[0] == null)
            {
                error = $"{room.name}: 첫 번째 RewardSpawnPoint가 없습니다.";
                return false;
            }
        }
        else if (prefabCollider == null || prefabCollider.isTrigger)
        {
            error = $"{prefab.name}: 비트리거 BoxCollider2D가 없습니다.";
            return false;
        }

        Physics2D.SyncTransforms();

        for (int index = 0; index < rewardCount; index++)
        {
            Vector3 position;
            Quaternion rotation;

            if (isInitialReward)
            {
                // 확정 보상: 안전하게 배치한 첫 번째 지점 사용
                position = points[0].position;
                rotation = points[0].rotation;
            }
            else
            {
                // 전투 보상: 지점 주변에서 충돌 없는 위치 탐색
                if (!TryFindRewardPose(
                        room,
                        prefabCollider,
                        random,
                        out position,
                        out rotation))
                {
                    Debug.LogWarning(
                        $"{room.name}: 보상 상자 {index + 1}/{rewardCount}의 " +
                        $"위치를 {_profile.RewardPlacementAttempts}회 안에 찾지 못했습니다.",
                        room);
                    continue;
                }
            }

            InteractablePotionBox box = Instantiate(
                prefab,
                position,
                rotation,
                stage.TransientsRoot);

            if (box == null)
            {
                if (isInitialReward)
                {
                    error = $"{room.name}: 초기 보상 생성에 실패했습니다.";
                    return false;
                }

                Debug.LogWarning(
                    $"{room.name}: 보상 상자 생성에 실패했습니다.",
                    room);
                continue;
            }

            box.name = $"{room.name}_{prefab.name}_{index + 1}";
            Physics2D.SyncTransforms();
        }

        _rewardedRoomChecker.Add(node.Index);
        return true;
    }

    private bool TryFindRewardPose(RoomDefinition room, BoxCollider2D prefabCollider, System.Random random, out Vector3 position, out Quaternion rotation)
    {
        position = default;
        rotation = Quaternion.identity;

        IReadOnlyList<Transform> spawnPoints = room.RewardSpawnPoints;

        for (int attempt = 0; attempt < _profile.RewardPlacementAttempts; attempt++)
        {
            Transform spawnPoint = spawnPoints[random.Next(spawnPoints.Count)];

            if (spawnPoint == null)
            {
                continue;
            }

            Vector2 offset = GetRandomPointInsideCircle(random, _profile.RewardSpawnRadius);

            Vector3 candidatePosition = spawnPoint.position + new Vector3(offset.x, offset.y, 0f);
            Quaternion candidateRotation = spawnPoint.rotation;

            if (HasRewardCollision(candidatePosition, candidateRotation, prefabCollider))
            {
                continue;
            }

            position = candidatePosition;
            rotation = candidateRotation;
            return true;
        }

        return false;
    }

    private bool HasRewardCollision(Vector3 position, Quaternion rotation, BoxCollider2D prefabCollider)
    {
        Vector3 prefabScale = prefabCollider.transform.lossyScale;

        Vector2 colliderSize = new Vector2(prefabCollider.size.x * Mathf.Abs(prefabScale.x), prefabCollider.size.y * Mathf.Abs(prefabScale.y));
        Vector3 localOffset = new Vector3(prefabCollider.offset.x * prefabScale.x, prefabCollider.offset.y * prefabScale.y, 0f);

        Vector2 colliderCenter = position + rotation * localOffset;

        Collider2D overlap = Physics2D.OverlapBox(colliderCenter, colliderSize, rotation.eulerAngles.z, _profile.RewardBlockingLayers.value);

        return overlap != null;
    }

    private static Vector2 GetRandomPointInsideCircle(System.Random random, float radius)
    {
        float angle = (float)(random.NextDouble() * Math.PI * 2d);

        float distance = Mathf.Sqrt((float)random.NextDouble()) * radius;

        return new Vector2(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance);
    }

    private static System.Random CreateRoomRandom(GeneratedStage stage, GeneratedRoomNode node)
    {
        unchecked //오버플로우 무시
        {
            int seed = stage.Seed;
            seed = seed * 397 ^ node.Index;
            seed = seed * 397 ^ Reward_Random_Salt;

            return new System.Random(seed);
        }
    }
}