using System.Collections.Generic;
using UnityEngine;

public class CombatRoomProgressController : MonoBehaviour
{
    private class RoomRuntime
    {
        public GeneratedRoomNode Node { get; }
        public RoomEncounterController Encounter { get; }
        public RoomProgressState Progress { get; set; }

        public RoomRuntime(GeneratedRoomNode node, RoomEncounterController encounter)
        {
            Node = node;
            Encounter = encounter;
            Progress = RoomProgressState.Unvisited;
        }
    }

    private readonly Dictionary<GeneratedRoomNode, RoomRuntime> _roomByNode = new Dictionary<GeneratedRoomNode, RoomRuntime>();

    private readonly List<RoomRuntime> _combatRooms = new List<RoomRuntime>();

    private RoomRuntime _activeRoom;

    public GeneratedRoomNode CurrentRoom { get; private set; }
    public GeneratedRoomNode ActiveRoom => _activeRoom?.Node;
    public bool IsCompleted { get; private set; }
    public int CombatRoomCount => _combatRooms.Count;

    public bool TryInitialize(GeneratedStage stage, out GeneratedRoomNode firstPreparedRoom, out string error)
    {
        Clear();
        firstPreparedRoom = null;

        if (stage?.RoomNodes == null || stage.RoomNodes.Count == 0)
        {
            error = "GeneratedStage에 RoomNodes가 없습니다.";
            return false;
        }

        foreach (GeneratedRoomNode node in stage.RoomNodes)
        {
            if (!IsCombatRoom(node))
            {
                continue;
            }

            RoomEncounterController encounter = node.Room.GetComponent<RoomEncounterController>();

            if (encounter == null)
            {
                error = $"{node.Room.name}: {nameof(RoomEncounterController)}가 없습니다.";
                return false;
            }

            if (!encounter.TryInitialize(node, out string encounterError))
            {
                error = encounterError;
                return false;
            }

            RoomRuntime runtime = new RoomRuntime(node, encounter);

            _roomByNode.Add(node, runtime);
            _combatRooms.Add(runtime);
        }

        if (_combatRooms.Count == 0)
        {
            error = "생성된 Stage에 전투 방이 없습니다.";
            return false;
        }

        foreach (RoomRuntime room in _combatRooms)
        {
            room.Progress = RoomProgressState.Unvisited;
            room.Encounter.SetWaiting();
        }

        RoomRuntime firstRoom = _combatRooms[0];
        firstRoom.Progress = RoomProgressState.Prepared;
        firstPreparedRoom = firstRoom.Node;

        error = null;
        return true;
    }

    public bool TryBeginCombat(GeneratedRoomNode node)
    {
        if (_activeRoom != null || node == null ||
            !_roomByNode.TryGetValue(node, out RoomRuntime runtime) ||
            runtime.Progress != RoomProgressState.Prepared)
        {
            return false;
        }

        runtime.Progress = RoomProgressState.InCombat;
        runtime.Encounter.SetCombat();

        _activeRoom = runtime;
        CurrentRoom = node;

        return true;
    }

    public bool TryClearRoom(GeneratedRoomNode node, out GeneratedRoomNode nextPreparedRoom)
    {
        nextPreparedRoom = null;

        if (node == null || !_roomByNode.TryGetValue(node, out RoomRuntime runtime))
        {
            return false;
        }

        bool isPreparedRoom = (runtime.Progress == RoomProgressState.Prepared) && _activeRoom == null;
        bool isActiveRoom = (runtime.Progress == RoomProgressState.InCombat) && _activeRoom == runtime;

        if (!isPreparedRoom && !isActiveRoom)
        {
            return false;
        }

        runtime.Progress = RoomProgressState.Cleared;
        runtime.Encounter.SetCleared();

        if (_activeRoom == runtime)
        {
            _activeRoom = null;
        }

        foreach (RoomRuntime room in _combatRooms)
        {
            if (room.Progress != RoomProgressState.Unvisited)
            {
                continue;
            }

            room.Progress = RoomProgressState.Prepared;
            nextPreparedRoom = room.Node;
            return true;
        }

        IsCompleted = true;
        return true;
    }

    public bool TryGetRoomProgress(GeneratedRoomNode node, out RoomProgressState progress)
    {
        if (node != null && _roomByNode.TryGetValue(node, out RoomRuntime runtime))
        {
            progress = runtime.Progress;
            return true;
        }

        progress = RoomProgressState.Unvisited;
        return false;
    }

    public void Clear()
    {
        _roomByNode.Clear();
        _combatRooms.Clear();

        _activeRoom = null;
        CurrentRoom = null;
        IsCompleted = false;
    }

    private static bool IsCombatRoom(GeneratedRoomNode node)
    {
        if (node?.Room == null)
        {
            return false;
        }

        return node.Room.Type == RoomType.Normal || node.Room.Type == RoomType.Elite;
    }
}