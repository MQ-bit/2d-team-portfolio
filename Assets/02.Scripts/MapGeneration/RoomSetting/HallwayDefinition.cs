using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class HallwayDefinition : MonoBehaviour
{
    private const float Distance_Tolerance = 0.001f;
    private const float Expected_Length = 30f; // 복도 길이의 기대값

    [Header("Hallway")]
    [SerializeField] private HallwayOrientation _orientation;
    [SerializeField] private Bounds _localBounds;

    [Header("Connectors")]
    [SerializeField] private RoomConnector[] _connectors = Array.Empty<RoomConnector>();

    [Header("Collider")]
    [SerializeField] private TilemapCollider2D _wallCollider;

    public HallwayOrientation Orientation => _orientation;
    public Bounds LocalBounds => _localBounds;
    public IReadOnlyList<RoomConnector> Connectors => _connectors;

    public Bounds WorldBounds
    {
        get
        {
            Vector3 scale = transform.lossyScale;
            Vector3 absoluteScale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));

            return new Bounds(transform.TransformPoint(_localBounds.center), Vector3.Scale(_localBounds.size, absoluteScale));
        }
    }

    /// <summary>
    /// Direction에 해당하는 자신의 Connector를 가져옵니다.
    /// </summary>
    /// <param name="direction"> 원하는 Connector의 방향 </param>
    /// <param name="connector"> 찾은 Connector </param>
    /// <returns></returns>
    public bool TryGetConnector(ConnectorDirection direction, out RoomConnector connector)
    {
        foreach (RoomConnector candidate in _connectors)
        {
            if (candidate == null || candidate.Direction != direction)
            {
                continue;
            }

            connector = candidate;
            return true;
        }

        connector = null;
        return false;
    }

    public void CollectValidationErrors(List<string> errors)
    {
        if (errors == null)
        {
            return;
        }

        if (_localBounds.size.x <= 0f || _localBounds.size.y <= 0f)
        {
            errors.Add($"{name}: LocalBounds 크기가 유효하지 않습니다.");
        }

        if (_connectors == null || _connectors.Length != 2)
        {
            errors.Add($"{name}: Hallway Connector는 정확히 2개여야 합니다.");
            return;
        }

        RoomConnector first = _connectors[0];
        RoomConnector second = _connectors[1];

        if (first == null || second == null)
        {
            errors.Add($"{name}: Hallway Connector 참조가 비어 있습니다.");
            return;
        }

        if (first.Direction.OppositeDirection() != second.Direction)
        {
            errors.Add($"{name}: Hallway Connector가 반대 방향이 아닙니다.");
        }

        bool correctOrientation = _orientation switch
        {
            HallwayOrientation.Horizontal => first.Direction.IsHorizontal() && second.Direction.IsHorizontal(),
            HallwayOrientation.Vertical => !first.Direction.IsHorizontal() && !second.Direction.IsHorizontal(),
            _ => false
        };

        if (!correctOrientation)
        {
            errors.Add($"{name}: Hallway 방향과 Connector 방향이 다릅니다.");
        }

        Vector3 firstLocalPosition = transform.InverseTransformPoint(first.WorldPosition);
        Vector3 secondLocalPosition = transform.InverseTransformPoint(second.WorldPosition);

        float distance = Vector3.Distance(firstLocalPosition, secondLocalPosition);

        if (Mathf.Abs(distance - Expected_Length) > Distance_Tolerance)
        {
            errors.Add($"{name}: Connector 간격이 30이 아닙니다. 현재 값: {distance}");
        }

        if (_wallCollider == null)
        {
            errors.Add($"{name}: Wall TilemapCollider2D가 없습니다.");
        }
        else
        {
            if (_wallCollider.compositeOperation != Collider2D.CompositeOperation.Merge)
            {
                errors.Add($"{name}: TilemapCollider2D의 Composite Operation이 Merge가 아닙니다.");
            }

            Rigidbody2D rigidbody2D = _wallCollider.GetComponent<Rigidbody2D>();

            if (rigidbody2D == null || rigidbody2D.bodyType != RigidbodyType2D.Static)
            {
                errors.Add($"{name}: Wall Rigidbody2D가 Static이 아닙니다.");
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(_localBounds.center, _localBounds.size);
    }

}
