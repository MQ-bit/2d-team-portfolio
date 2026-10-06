using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public class RoomWaveStartTrigger : MonoBehaviour
{
    [SerializeField] private BoxCollider2D _trigger;

    private GeneratedRoomNode _node;
    private Action<GeneratedRoomNode> _callback;
    private bool _armed;

    private void Awake()
    {
        if (_trigger == null)
        {
            _trigger = GetComponent<BoxCollider2D>();
        }

        _trigger.isTrigger = true;
        Disarm();
    }

    public void Bind(GeneratedRoomNode node, RoomEncounterSocket socket, Action<GeneratedRoomNode> callback)
    {
        Disarm();

        _node = node;
        _callback = callback;

        transform.SetPositionAndRotation(socket.TriggerWorldCenter, socket.TriggerWorldRotation);

        transform.localScale = Vector3.one;
        _trigger.offset = Vector2.zero;
        _trigger.size = socket.TriggerWorldSize;
    }

    public void Arm()
    {
        if (_node == null || _callback == null)
        {
            return;
        }

        _armed = true;
        _trigger.enabled = true;
        gameObject.SetActive(true);
    }

    public void Disarm()
    {
        _armed = false;

        if (_trigger != null)
        {
            _trigger.enabled = false;
        }
    }

    public void Unbind()
    {
        Disarm();
        _node = null;
        _callback = null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_armed)
        {
            return;
        }

        PlayerController player = other.GetComponent<PlayerController>(); //TODO : 태그방식? 아니면 인터페이스?

        if (player == null)
        {
            return;
        }

        GeneratedRoomNode requestedNode = _node;
        Action<GeneratedRoomNode> callback = _callback;

        Disarm();
        callback?.Invoke(requestedNode);
    }
}