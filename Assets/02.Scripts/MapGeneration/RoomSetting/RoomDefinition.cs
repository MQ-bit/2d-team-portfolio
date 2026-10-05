using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Stage를 구성하는 방의 정의를 나타내는 컴포넌트. 
/// 방의 유형, 로컬 경계, 벽 타일맵, 벽 콜라이더, 연결 지점, 스폰 지점 등의 정보를 포함하며, 방의 유효성을 검증하고 연결 지점을 관리함.
/// </summary>
public class RoomDefinition : MonoBehaviour
{
    private const float Direction_Tolerance = 0.999f; // 방향 벡터의 정규화된 값이 0.999 이상이면 해당 방향으로 간주

    [Header("Room")]
    [SerializeField] private RoomType _roomType;
    [SerializeField] private Bounds _localBounds;

    [Header("Wall")]
    [SerializeField] private Tilemap _wallTilemap;
    [SerializeField] private TilemapCollider2D _wallCollider;

    [Header("Connectors")]
    [SerializeField] private RoomConnector[] _connectors = Array.Empty<RoomConnector>();

    [Header("Spawn Points")]
    [SerializeField] private Transform _playerSpawnPoint;
    [SerializeField] private Transform _portalSpawnPoint;
    [SerializeField] private Transform[] _enemySpawnPoints = Array.Empty<Transform>();
    //[SerializeField] private Transform _rewardSpawnPoint; //보상 오브젝트가 있다면 사용

    public RoomType Type => _roomType;
    public Bounds LocalBounds => _localBounds;
    public Tilemap WallTilemap => _wallTilemap;
    public TilemapCollider2D WallCollider => _wallCollider;

    public IReadOnlyList<RoomConnector> Connectors => _connectors;
    public IReadOnlyList<Transform> EnemySpawnPoints => _enemySpawnPoints;

    public Transform PlayerSpawnPoint => _playerSpawnPoint;
    public Transform PortalSpawnPoint => _portalSpawnPoint;
    //public Transform RewardSpawnPoint => _rewardSpawnPoint;

    public Bounds WorldBounds
    {
        get
        {
            Vector3 scale = transform.lossyScale;
            Vector3 absoluteScale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));

            return new Bounds(transform.TransformPoint(_localBounds.center), Vector3.Scale(_localBounds.size, absoluteScale));
        }
    }

    public bool TryGetConnector(ConnectorDirection direction, out RoomConnector connector)
    {
        foreach(RoomConnector candidate in _connectors)
        {
            if(candidate == null || candidate.Direction != direction)
            {
                continue;
            }

            connector = candidate;
            return true;
        }

        connector = null;
        return false;
    }

    public bool TryCarveConnector(ConnectorDirection direction, out int removedTileCount)
    {
        removedTileCount = 0;
        if(_wallTilemap == null || !TryGetConnector(direction, out RoomConnector connector))
        {
            return false;
        }

        removedTileCount = connector.Carve(_wallTilemap);
        return true;
    }

    public void RefreshWallCollider()
    {
        if(_wallCollider == null)
        {
            Debug.LogError($"{name} : WallTilemapCollider2D is not assigned. Cannot refresh collider.");
            return;
        }

        _wallCollider.ProcessTilemapChanges();
    }

    public void CollectValidationErrors(List<string> errors)
    {
        if (errors == null) return;
        if (_wallTilemap == null)
        {
            errors.Add($"{name}: Wall Tilemap 참조가 없습니다.");
        }

        if (_wallCollider == null)
        {
            errors.Add($"{name}: Wall TilemapCollider2D 참조가 없습니다.");
        }
        else
        {
            if (_wallCollider.compositeOperation != Collider2D.CompositeOperation.Merge)
            {
                errors.Add($"{name}: TilemapCollider2D의 Composite Operation이 Merge가 아닙니다.");
            }

            if (_wallCollider.GetComponent<CompositeCollider2D>() == null)
            {
                errors.Add($"{name}: CompositeCollider2D가 없습니다.");
            }

            Rigidbody2D rigidbody2D =_wallCollider.GetComponent<Rigidbody2D>();

            if (rigidbody2D == null)
            {
                errors.Add($"{name}: Wall Tilemap에 Rigidbody2D가 없습니다.");
            }
            else if (rigidbody2D.bodyType != RigidbodyType2D.Static)
            {
                errors.Add($"{name}: Wall Rigidbody2D가 Static이 아닙니다.");
            }
        }

        if (_localBounds.size.x <= 0f || _localBounds.size.y <= 0f)
        {
            errors.Add($"{name}: LocalBounds 크기가 유효하지 않습니다.");
        }

        if (_connectors == null || _connectors.Length < 2 || _connectors.Length > 4)
        {
            errors.Add($"{name}: Connector는 2~4개여야 합니다.");
        }
        else
        {
            ValidateConnectors(errors);
        }

        switch (_roomType)
        {
            case RoomType.Start:
                {
                    if (_playerSpawnPoint == null)
                    {
                        errors.Add($"{name}: PlayerSpawnPoint가 없습니다.");
                    }
                    break;                    
                }

            case RoomType.Normal:
            case RoomType.Elite:
                {
                    
                    if (_enemySpawnPoints == null || _enemySpawnPoints.Length == 0)
                    {
                        errors.Add($"{name}: EnemySpawnPoint가 없습니다.");
                    }
                    else
                    {
                        for (int i = 0; i < _enemySpawnPoints.Length; i++)
                        {
                            if (_enemySpawnPoints[i] == null)
                            {
                                errors.Add($"{name}: EnemySpawnPoints[{i}]가 비어 있습니다.");
                            }
                        }
                    }
                    break;
                }

            case RoomType.End:
                {
                    if (_portalSpawnPoint == null)
                    {
                        errors.Add($"{name}: PortalSpawnPoint가 없습니다.");
                    }
                    break;                    
                }
        }
    }
    private void ValidateConnectors(List<string> errors)
    {
        HashSet<ConnectorDirection> directions = new HashSet<ConnectorDirection>();

        foreach (RoomConnector connector in _connectors)
        {
            if (connector == null)
            {
                errors.Add($"{name}: Connector 배열에 null이 있습니다.");
                continue;
            }

            if (!directions.Add(connector.Direction))
            {
                errors.Add($"{name}: {connector.Direction} Connector가 중복됩니다.");
            }

            float directionDot = Vector2.Dot(connector.WorldDirection, connector.Direction.ToVector2());
            if (directionDot < Direction_Tolerance)
            {
                errors.Add($"{name}/{connector.name}: Transform.up과 방향 설정이 일치하지 않습니다.");
            }

            BoundsInt area = connector.CarveArea;

            if (!connector.HasValidCarveArea)
            {
                errors.Add($"{name}/{connector.name}: CarveArea가 설정되지 않았습니다.");
                continue;
            }

            if (area.size.z != 1)
            {
                errors.Add($"{name}/{connector.name}: CarveArea의 Z 크기는 1이어야 합니다.");
            }

            bool correctWidth = connector.Direction.IsHorizontal() ? area.size.y == 10 : area.size.x == 10; // 입구쯤 되는 곳의 영역이 10칸인지 체크. 매직넘버 제거할 필요가 있음.

            if (!correctWidth)
            {
                errors.Add($"{name}/{connector.name}: 입구 폭이 10칸이 아닙니다.");
            }

            if (_wallTilemap != null &&!connector.ContainsWallTile(_wallTilemap))
            {
                errors.Add($"{name}/{connector.name}: CarveArea 안에 Wall 타일이 없습니다.");
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(_localBounds.center, _localBounds.size);
    }
}
