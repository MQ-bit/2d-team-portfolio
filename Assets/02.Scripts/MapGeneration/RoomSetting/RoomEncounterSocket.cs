using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RoomConnector))]
public sealed class RoomEncounterSocket : MonoBehaviour
{
    [SerializeField] private Transform _doorAnchor;

    [Header("Wave Trigger")]
    [SerializeField] private Vector2 _triggerLocalCenter;
    [SerializeField] private Vector2 _triggerSize = new Vector2(8f, 3f);
    [SerializeField] private float _triggerLocalAngle;

    public Transform DoorAnchor => _doorAnchor;
    public Vector3 TriggerWorldCenter => transform.TransformPoint(_triggerLocalCenter);
    public Quaternion TriggerWorldRotation => transform.rotation * Quaternion.Euler(0f, 0f, _triggerLocalAngle);

    public Vector2 TriggerWorldSize
    {
        get
        {
            Vector3 scale = transform.lossyScale;

            return new Vector2(_triggerSize.x * Mathf.Abs(scale.x), _triggerSize.y * Mathf.Abs(scale.y));
        }
    }

    public void CollectValidationErrors(RoomDefinition room, List<string> errors)
    {
        if (_doorAnchor == null)
        {
            errors.Add($"{name}: DoorAnchor가 없습니다.");
        }

        if (_triggerSize.x <= 0f || _triggerSize.y <= 0f)
        {
            errors.Add($"{name}: Trigger 크기가 유효하지 않습니다.");
        }

        if (room != null && !room.WorldBounds.Contains(TriggerWorldCenter))
        {
            errors.Add($"{name}: Wave Trigger 중심이 방 Bounds 안에 없습니다.");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.matrix = Matrix4x4.TRS(TriggerWorldCenter, TriggerWorldRotation, Vector3.one);

        Gizmos.DrawWireCube(Vector3.zero, new Vector3(TriggerWorldSize.x, TriggerWorldSize.y, 0f));

        if (_doorAnchor != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.DrawWireSphere(_doorAnchor.position, 0.3f);
        }
    }
}