using System.Collections.Generic;
using UnityEngine;

namespace Entity.Inventory
{
    // 아이템용 검사: 기존 투척 검사를 재사용하고 착지에는 포션 크기를 반영한다.
    public sealed class PotionWallCollision
    {
        private readonly global::PotionWallCollision _trajectoryCollision = new();
        private readonly List<Collider2D> _overlaps = new();
        private readonly List<RaycastHit2D> _hits = new();

        private LayerMask _wallLayers;
        private ContactFilter2D _filter;
        private float _radius;
        private Vector2 _colliderOffset;

        public void Configure(Collider2D collider, Transform item, LayerMask wallLayers)
        {
            _wallLayers = wallLayers;
            _filter = PotionWallQuery.Filter(wallLayers);
            if (collider is CircleCollider2D circle)
            {
                Vector3 scale = circle.transform.lossyScale;
                _radius = circle.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
                _colliderOffset = circle.transform.TransformPoint(circle.offset) - item.position;
            }
            else if (collider != null)
            {
                Bounds bounds = collider.bounds;
                _radius = ((Vector2)bounds.extents).magnitude;
                _colliderOffset = bounds.center - item.position;
            }
            else
            {
                _radius = 0.2f;
                _colliderOffset = Vector2.zero;
            }
            _radius = Mathf.Max(0.01f, _radius);
        }

        public bool TryGetSpawnPosition(Vector3 origin, float padding, out Vector3 position)
        {
            position = origin;
            if (IsPositionClear(origin)) return true;

            Vector2 center = (Vector2)origin + _colliderOffset;
            _overlaps.Clear();
            Physics2D.OverlapPoint(center, _filter, _overlaps);
            bool centerInsideWall = false;
            foreach (Collider2D wall in _overlaps)
            {
                if (PotionWallQuery.IsWall(wall, _wallLayers)) centerInsideWall = true;
            }

            // 가까운 거리부터 탐색한다. 같은 거리에서는 아래쪽 바닥을 먼저 확인한다.
            // 탐색 범위를 제한해 멀리 떨어진 방으로 생성점이 이동하지 않게 한다.
            float step = Mathf.Max(0.025f, _radius * 0.25f);
            float maxDistance = Mathf.Max(1f, _radius * 4f);
            int rings = Mathf.CeilToInt(maxDistance / step);
            const int directionCount = 32;
            for (int ring = 1; ring <= rings; ring++)
            {
                float distance = Mathf.Min(ring * step, maxDistance);
                for (int i = 0; i < directionCount; i++)
                {
                    float angle = -Mathf.PI * 0.5f + i * Mathf.PI * 2f / directionCount;
                    Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    Vector3 candidate = origin + (Vector3)(direction * distance);
                    if (!IsPositionClear(candidate)) continue;

                    // 시작점 중심이 벽 밖이라면 벽 반대편으로 보정하지 않는다.
                    if (!centerInsideWall && !IsCenterPathClear(center, direction, distance)) continue;

                    Vector3 padded = candidate + (Vector3)(direction * Mathf.Max(0.01f, padding));
                    if (!IsPositionClear(padded)) continue;
                    if (!IsCenterPathClear((Vector2)candidate + _colliderOffset, direction,
                            Mathf.Max(0.01f, padding)))
                    {
                        continue;
                    }

                    position = padded;
                    return true;
                }
            }
            return false;
        }

        private bool IsCenterPathClear(Vector2 start, Vector2 direction, float distance)
        {
            _hits.Clear();
            Physics2D.Raycast(start, direction, _filter, _hits, distance);
            foreach (RaycastHit2D hit in _hits)
            {
                if (PotionWallQuery.IsWall(hit.collider, _wallLayers)) return false;
            }
            return true;
        }

        public bool TryTrace(Vector3 start, Vector3 target, float arcHeight,
            float fromProgress, float toProgress, float padding, out PotionThrowPrediction hit)
        {
            float clearance = _radius + _colliderOffset.magnitude + Mathf.Max(0.01f, padding);
            return _trajectoryCollision.TryTrace(start, target, arcHeight, fromProgress,
                toProgress, _wallLayers, clearance, out hit);
        }

        public Vector3 GetLandingPosition(PotionThrowPrediction hit, Vector3 lastSafeGround)
        {
            return hit.CanCreateArea && IsPositionClear(hit.AreaPosition)
                ? hit.AreaPosition : lastSafeGround;
        }

        public bool IsPositionClear(Vector3 position)
        {
            _overlaps.Clear();
            Physics2D.OverlapCircle((Vector2)position + _colliderOffset, _radius, _filter, _overlaps);
            foreach (Collider2D wall in _overlaps)
            {
                if (PotionWallQuery.IsWall(wall, _wallLayers)) return false;
            }
            return true;
        }

        public bool IsPathClear(Vector3 start, Vector3 end)
        {
            Vector2 movement = end - start;
            float distance = movement.magnitude;
            if (distance <= Mathf.Epsilon) return true;

            _hits.Clear();
            Physics2D.CircleCast((Vector2)start + _colliderOffset, _radius,
                movement / distance, _filter, _hits, distance);
            foreach (RaycastHit2D hit in _hits)
            {
                if (PotionWallQuery.IsWall(hit.collider, _wallLayers)) return false;
            }
            return true;
        }
    }
}
