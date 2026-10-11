using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// 몬스터 공통 길찾기(플로우 필드).
// 맵을 격자로 나눠 벽/소품이 있는 칸을 막아두고, 목표(플레이어) 칸에서 퍼져나가는 거리 지도를 만들어 모든 몬스터가 같이 쓴다.
// 몸 크기 단계마다 막힌 칸과 거리 지도를 따로 둬서, 큰 몬스터(엘리트)는 벽에서 더 떨어진 길로 다닌다.
// 목표가 다른 칸으로 옮겨갈 때만 거리 지도를 다시 계산하므로 몬스터 수가 늘어도 비용이 거의 같다.
// 씬에 따로 배치하지 않아도 처음 쓰일 때 스스로 생성되고, 씬의 타일맵 범위로 격자를 만든다.
// 맵이 바뀌면(랜덤 맵 생성 등) RebuildGrid()를 호출한다.
public class EnemyPathfinder : MonoBehaviour
{
    private const float Unreachable = float.MaxValue;
    private const float DiagonalCost = 1.4142135f;

    private static readonly Vector2Int[] NeighborOffsets =
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
        new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
    };

    private static EnemyPathfinder _instance;
    public static EnemyPathfinder Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<EnemyPathfinder>();
                if (_instance == null)
                {
                    _instance = new GameObject(nameof(EnemyPathfinder)).AddComponent<EnemyPathfinder>();
                }
            }

            return _instance;
        }
    }

    [Header("Grid Settings")]
    [Tooltip("격자 한 칸 크기. 타일(1유닛)의 절반이면 칸 중심이 타일 중심과 경계에 모두 놓인다.")]
    [SerializeField, Min(0.1f)] private float _cellSize = 0.5f;
    [Tooltip("몸 크기 단계(반지름). 몬스터는 자기 몸보다 크거나 같은 가장 작은 단계의 길을 쓴다. 단계마다 벽에서 그만큼 떨어진 칸만 지나간다.")]
    [SerializeField] private float[] _clearanceRadii = { 0.5f, 1f };
    [Tooltip("몸 반지름에 더하는 여유. 경로가 벽에 스치지 않게 한다. 칸 중심만 검사하므로 칸 반쪽 대각선(칸 크기 x 0.71)보다 작으면 그 값으로 올려 쓴다.")]
    [SerializeField, Min(0f)] private float _clearanceMargin = 0.15f;

    [Header("Path Settings")]
    [Tooltip("경로를 몇 칸 앞까지 내다보고, 그중 직선으로 갈 수 있는 가장 먼 칸으로 향한다. 클수록 경로가 매끄럽다.")]
    [SerializeField, Min(1)] private int _lookAheadCells = 8;
    [Tooltip("목표로부터 이 거리(유닛, 경로 기준)까지만 길을 계산한다. 그보다 먼 몬스터는 목표 쪽으로 직진한다.")]
    [SerializeField, Min(1f)] private float _maxSearchDistance = 60f;
    [Tooltip("목표가 칸을 옮겨도 거리 지도는 최대 이 간격(초)으로만 다시 계산한다. 마지막 접근은 직선 추적이라 약간 늦어도 티가 안 난다.")]
    [SerializeField, Min(0f)] private float _fieldUpdateInterval = 0.1f;

    [Header("Debug")]
    [SerializeField] private bool _drawGizmos;
    [Tooltip("기즈모로 보여줄 몸 크기 단계 번호 (0 = 가장 작은 단계)")]
    [SerializeField, Min(0)] private int _gizmoLayerIndex;

    // 몸 크기 단계 하나. 막힌 칸과 거리 지도를 단계마다 따로 갖는다.
    private sealed class ClearanceLayer
    {
        public float Radius;
        public bool[] Blocked;
        public float[] Distance;
        public int TargetIndex = -1;
        public float NextUpdateTime;
    }

    private readonly HashSet<Collider2D> _obstacles = new HashSet<Collider2D>();
    private readonly List<Collider2D> _overlapResults = new List<Collider2D>();
    private readonly List<RaycastHit2D> _castResults = new List<RaycastHit2D>();
    private readonly List<ClearanceLayer> _layers = new List<ClearanceLayer>();
    private readonly CellHeap _open = new CellHeap();

    // 칸 중심으로만 막힘을 검사하므로, 칸 안 어디에 있어도 몸이 벽에 닿지 않으려면 여유가 칸 반쪽 대각선 이상이어야 한다.
    private float ClearanceMargin => Mathf.Max(_clearanceMargin, _cellSize * 0.71f);

    private bool _hasGrid;
    private bool _isGridBuilt;
    private Vector2 _origin;
    private int _width;
    private int _height;

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    // 씬의 타일맵 범위로 격자를 다시 만들고, 몸 크기 단계마다 칸이 정적 콜라이더(벽, 소품)와 겹치는지 검사한다.
    public void RebuildGrid()
    {
        _isGridBuilt = true;
        _obstacles.Clear();
        _layers.Clear();
        _hasGrid = TryGetMapBounds(out Bounds bounds);
        if (!_hasGrid)
        {
            return;
        }

        // 칸 중심이 타일 경계와 타일 중심에 놓이도록 반 칸 밀어서 시작한다.
        _origin = new Vector2(Mathf.Floor(bounds.min.x), Mathf.Floor(bounds.min.y)) - Vector2.one * (_cellSize * 0.5f);
        _width = Mathf.CeilToInt((bounds.max.x - _origin.x) / _cellSize) + 1;
        _height = Mathf.CeilToInt((bounds.max.y - _origin.y) / _cellSize) + 1;

        List<float> radii = new List<float>();
        if (_clearanceRadii != null)
        {
            radii.AddRange(_clearanceRadii);
        }
        if (radii.Count == 0)
        {
            radii.Add(0.5f);
        }
        radii.Sort();

        foreach (float radius in radii)
        {
            _layers.Add(new ClearanceLayer
            {
                Radius = Mathf.Max(0.01f, radius),
                Blocked = new bool[_width * _height],
                Distance = new float[_width * _height]
            });
        }

        ContactFilter2D filter = new ContactFilter2D { useTriggers = false };
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                int index = GetIndex(x, y);
                Vector2 center = GetCellCenter(x, y);

                // 작은 단계에서 막힌 칸은 큰 단계에서도 막혀 있으므로 다시 검사하지 않는다.
                bool isBlocked = false;
                foreach (ClearanceLayer layer in _layers)
                {
                    if (!isBlocked)
                    {
                        isBlocked = OverlapsObstacle(center, layer.Radius + ClearanceMargin, filter);
                    }

                    layer.Blocked[index] = isBlocked;
                }
            }
        }
    }

    // from에서 target 쪽으로 가려면 지금 향해야 할 지점. 격자가 없거나 길이 없으면 false.
    // bodyRadius는 몬스터 몸 반지름으로, 그 몸이 지나갈 수 있는 길만 고른다.
    // isDirect가 true면 목표까지 직선으로 갈 수 있으므로, 호출한 쪽은 목표의 현재 위치로 바로 가면 된다.
    public bool TryGetNextWaypoint(Vector2 from, Vector2 target, float bodyRadius, out Vector2 waypoint, out bool isDirect)
    {
        waypoint = target;
        isDirect = false;

        if (!EnsureGrid() || !TryGetCell(from, out Vector2Int fromCell))
        {
            return false;
        }

        ClearanceLayer layer = GetLayer(bodyRadius);
        if (HasLineOfSight(from, target) && HasClearPath(layer, from, target, true))
        {
            isDirect = true;
            return true;
        }

        UpdateDistanceField(layer, target);

        int current = GetIndex(fromCell.x, fromCell.y);
        if (layer.Distance[current] == Unreachable)
        {
            // 넉백 등으로 벽 쪽 여유 공간에 밀려 들어가 있으면 가장 가까운 갈 수 있는 칸부터 시작한다.
            current = FindNearestReachable(layer, fromCell);
            if (current < 0)
            {
                return false;
            }
        }

        // 거리가 줄어드는 칸을 따라 몇 칸 앞까지 나아가면서, 직선으로 갈 수 있는 가장 먼 칸을 고른다.
        int next = GetDownhillNeighbor(layer, current);
        waypoint = GetCellCenter(next >= 0 ? next : current);

        for (int step = 0; step < _lookAheadCells && next >= 0; step++)
        {
            Vector2 nextCenter = GetCellCenter(next);
            if (step > 0 && !HasClearPath(layer, from, nextCenter, false))
            {
                break;
            }

            waypoint = nextCenter;
            next = GetDownhillNeighbor(layer, next);
        }

        return true;
    }

    // 실제 벽/소품 콜라이더에 가려지지 않는지(몸 크기 여유 없이). 원거리 공격 판정용.
    public bool HasLineOfSight(Vector2 from, Vector2 to)
    {
        if (!EnsureGrid())
        {
            return true;
        }

        ContactFilter2D filter = new ContactFilter2D { useTriggers = false };
        Physics2D.Linecast(from, to, filter, _castResults);
        foreach (RaycastHit2D hit in _castResults)
        {
            if (_obstacles.Contains(hit.collider))
            {
                return false;
            }
        }

        return true;
    }

    private bool EnsureGrid()
    {
        if (!_isGridBuilt)
        {
            RebuildGrid();
        }

        return _hasGrid;
    }

    private bool OverlapsObstacle(Vector2 center, float radius, ContactFilter2D filter)
    {
        Physics2D.OverlapCircle(center, radius, filter, _overlapResults);
        bool overlaps = false;
        foreach (Collider2D hit in _overlapResults)
        {
            if (IsStaticObstacle(hit))
            {
                _obstacles.Add(hit);
                overlaps = true;
            }
        }

        return overlaps;
    }

    // 몸 반지름보다 크거나 같은 가장 작은 단계. 몸이 모든 단계보다 크면 가장 큰 단계.
    private ClearanceLayer GetLayer(float bodyRadius)
    {
        foreach (ClearanceLayer layer in _layers)
        {
            if (layer.Radius >= bodyRadius * 0.95f)
            {
                return layer;
            }
        }

        return _layers[_layers.Count - 1];
    }

    // 몸이 지나갈 수 있는 칸만 거쳐서 직선으로 갈 수 있는지. 출발점 근처는 검사하지 않는다(벽에 붙어 있어도 출발할 수 있게).
    // ignoreTargetArea가 true면 도착점 근처도 검사하지 않는다(벽에 붙어 선 목표에게 마지막으로 다가갈 수 있게).
    // 이때 벽을 사이에 둔 경우는 HasLineOfSight로 따로 거른다.
    private bool HasClearPath(ClearanceLayer layer, Vector2 from, Vector2 to, bool ignoreTargetArea)
    {
        Vector2 delta = to - from;
        float length = delta.magnitude;
        float stepLength = _cellSize * 0.5f;
        int steps = Mathf.CeilToInt(length / stepLength);
        float targetArea = ignoreTargetArea ? layer.Radius + ClearanceMargin + _cellSize : 0f;

        for (int i = 1; i <= steps; i++)
        {
            float distance = Mathf.Min(i * stepLength, length);
            if (distance < _cellSize || length - distance < targetArea)
            {
                continue;
            }

            if (TryGetCell(from + delta * (distance / length), out Vector2Int cell) && layer.Blocked[GetIndex(cell.x, cell.y)])
            {
                return false;
            }
        }

        return true;
    }

    // 목표 칸에서부터 모든 칸까지의 최단 거리를 채운다(다익스트라). 목표 칸이 바뀔 때만 다시 계산한다.
    private void UpdateDistanceField(ClearanceLayer layer, Vector2 target)
    {
        if (!TryGetCell(target, out Vector2Int targetCell))
        {
            targetCell = new Vector2Int(Mathf.Clamp(targetCell.x, 0, _width - 1), Mathf.Clamp(targetCell.y, 0, _height - 1));
        }

        int targetIndex = GetIndex(targetCell.x, targetCell.y);
        if (targetIndex == layer.TargetIndex || (layer.TargetIndex >= 0 && Time.time < layer.NextUpdateTime))
        {
            return;
        }

        layer.TargetIndex = targetIndex;
        layer.NextUpdateTime = Time.time + _fieldUpdateInterval;
        float maxDistance = _maxSearchDistance / _cellSize;

        float[] distances = layer.Distance;
        for (int i = 0; i < distances.Length; i++)
        {
            distances[i] = Unreachable;
        }

        _open.Clear();
        SeedTarget(layer, targetCell);

        while (_open.TryPop(out int index, out float distance))
        {
            if (distance > distances[index])
            {
                continue;
            }

            int x = index % _width;
            int y = index / _width;
            for (int i = 0; i < NeighborOffsets.Length; i++)
            {
                Vector2Int offset = NeighborOffsets[i];
                if (!CanStep(layer, x, y, offset))
                {
                    continue;
                }

                int neighbor = GetIndex(x + offset.x, y + offset.y);
                float newDistance = distance + (i < 4 ? 1f : DiagonalCost);
                if (newDistance <= maxDistance && newDistance < distances[neighbor])
                {
                    distances[neighbor] = newDistance;
                    _open.Push(neighbor, newDistance);
                }
            }
        }
    }

    // 목표가 벽에 붙어 서 있으면 큰 몸 단계에서는 목표 칸이 막혀 있다.
    // 그때는 목표 주변에서 몸이 설 수 있는 칸들을, 목표까지의 거리만큼 앞선 출발점으로 삼는다.
    private void SeedTarget(ClearanceLayer layer, Vector2Int targetCell)
    {
        int targetIndex = GetIndex(targetCell.x, targetCell.y);
        if (!layer.Blocked[targetIndex])
        {
            layer.Distance[targetIndex] = 0f;
            _open.Push(targetIndex, 0f);
            return;
        }

        int range = Mathf.CeilToInt((layer.Radius + ClearanceMargin) * 2f / _cellSize) + 1;
        for (int dy = -range; dy <= range; dy++)
        {
            for (int dx = -range; dx <= range; dx++)
            {
                int x = targetCell.x + dx;
                int y = targetCell.y + dy;
                if (x < 0 || y < 0 || x >= _width || y >= _height)
                {
                    continue;
                }

                int index = GetIndex(x, y);
                if (layer.Blocked[index])
                {
                    continue;
                }

                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                layer.Distance[index] = distance;
                _open.Push(index, distance);
            }
        }
    }

    // 대각선은 양옆 두 칸이 모두 비어 있을 때만 지나간다(벽 모서리를 깎고 지나가지 않게).
    private bool CanStep(ClearanceLayer layer, int x, int y, Vector2Int offset)
    {
        int nx = x + offset.x;
        int ny = y + offset.y;
        if (nx < 0 || ny < 0 || nx >= _width || ny >= _height || layer.Blocked[GetIndex(nx, ny)])
        {
            return false;
        }

        if (offset.x != 0 && offset.y != 0)
        {
            return !layer.Blocked[GetIndex(x + offset.x, y)] && !layer.Blocked[GetIndex(x, y + offset.y)];
        }

        return true;
    }

    private int GetDownhillNeighbor(ClearanceLayer layer, int index)
    {
        int x = index % _width;
        int y = index / _width;
        int best = -1;
        float bestDistance = layer.Distance[index];

        foreach (Vector2Int offset in NeighborOffsets)
        {
            if (!CanStep(layer, x, y, offset))
            {
                continue;
            }

            int neighbor = GetIndex(x + offset.x, y + offset.y);
            if (layer.Distance[neighbor] < bestDistance)
            {
                bestDistance = layer.Distance[neighbor];
                best = neighbor;
            }
        }

        return best;
    }

    private int FindNearestReachable(ClearanceLayer layer, Vector2Int cell)
    {
        int maxRing = Mathf.CeilToInt((layer.Radius + ClearanceMargin) * 2f / _cellSize) + 1;
        for (int ring = 1; ring <= maxRing; ring++)
        {
            int best = -1;
            float bestDistance = Unreachable;
            for (int dy = -ring; dy <= ring; dy++)
            {
                for (int dx = -ring; dx <= ring; dx++)
                {
                    int x = cell.x + dx;
                    int y = cell.y + dy;
                    if (x < 0 || y < 0 || x >= _width || y >= _height)
                    {
                        continue;
                    }

                    int index = GetIndex(x, y);
                    if (!layer.Blocked[index] && layer.Distance[index] < bestDistance)
                    {
                        bestDistance = layer.Distance[index];
                        best = index;
                    }
                }
            }

            if (best >= 0)
            {
                return best;
            }
        }

        return -1;
    }

    // 움직이는 물체(플레이어, 몬스터, 투사체)와 트리거는 장애물이 아니다.
    private static bool IsStaticObstacle(Collider2D collider)
    {
        if (collider.isTrigger)
        {
            return false;
        }

        Rigidbody2D body = collider.attachedRigidbody;
        return body == null || body.bodyType == RigidbodyType2D.Static;
    }

    private static bool TryGetMapBounds(out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;

        foreach (TilemapRenderer tilemapRenderer in FindObjectsByType<TilemapRenderer>(FindObjectsSortMode.None))
        {
            if (!hasBounds)
            {
                bounds = tilemapRenderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(tilemapRenderer.bounds);
            }
        }

        return hasBounds;
    }

    private bool TryGetCell(Vector2 position, out Vector2Int cell)
    {
        cell = new Vector2Int(Mathf.FloorToInt((position.x - _origin.x) / _cellSize),
            Mathf.FloorToInt((position.y - _origin.y) / _cellSize));
        return cell.x >= 0 && cell.y >= 0 && cell.x < _width && cell.y < _height;
    }

    private int GetIndex(int x, int y) => y * _width + x;

    private Vector2 GetCellCenter(int x, int y) => _origin + new Vector2((x + 0.5f) * _cellSize, (y + 0.5f) * _cellSize);

    private Vector2 GetCellCenter(int index) => GetCellCenter(index % _width, index / _width);

    private void OnDrawGizmosSelected()
    {
        if (!_drawGizmos || !_hasGrid || _layers.Count == 0)
        {
            return;
        }

        ClearanceLayer layer = _layers[Mathf.Clamp(_gizmoLayerIndex, 0, _layers.Count - 1)];
        Vector3 size = new Vector3(_cellSize, _cellSize, 0f) * 0.9f;
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                int index = GetIndex(x, y);
                if (layer.Blocked[index])
                {
                    Gizmos.color = new Color(1f, 0f, 0f, 0.35f);
                }
                else if (layer.Distance[index] == Unreachable)
                {
                    continue;
                }
                else
                {
                    Gizmos.color = new Color(0f, 1f, 0f, 0.1f);
                }

                Gizmos.DrawCube(GetCellCenter(x, y), size);
            }
        }
    }

    // 다익스트라용 최소 힙. 같은 칸이 여러 번 들어갈 수 있고, 꺼낼 때 더 짧은 거리가 이미 있으면 버린다.
    private sealed class CellHeap
    {
        private readonly List<int> _indices = new List<int>();
        private readonly List<float> _priorities = new List<float>();

        public void Clear()
        {
            _indices.Clear();
            _priorities.Clear();
        }

        public void Push(int index, float priority)
        {
            _indices.Add(index);
            _priorities.Add(priority);

            int child = _indices.Count - 1;
            while (child > 0)
            {
                int parent = (child - 1) / 2;
                if (_priorities[parent] <= _priorities[child])
                {
                    break;
                }

                Swap(parent, child);
                child = parent;
            }
        }

        public bool TryPop(out int index, out float priority)
        {
            if (_indices.Count == 0)
            {
                index = -1;
                priority = 0f;
                return false;
            }

            index = _indices[0];
            priority = _priorities[0];

            int last = _indices.Count - 1;
            _indices[0] = _indices[last];
            _priorities[0] = _priorities[last];
            _indices.RemoveAt(last);
            _priorities.RemoveAt(last);

            int parent = 0;
            while (true)
            {
                int left = parent * 2 + 1;
                int right = left + 1;
                int smallest = parent;

                if (left < _indices.Count && _priorities[left] < _priorities[smallest])
                {
                    smallest = left;
                }
                if (right < _indices.Count && _priorities[right] < _priorities[smallest])
                {
                    smallest = right;
                }
                if (smallest == parent)
                {
                    break;
                }

                Swap(parent, smallest);
                parent = smallest;
            }

            return true;
        }

        private void Swap(int a, int b)
        {
            (_indices[a], _indices[b]) = (_indices[b], _indices[a]);
            (_priorities[a], _priorities[b]) = (_priorities[b], _priorities[a]);
        }
    }
}
