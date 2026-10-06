using UnityEngine;

public class RoomEncounterController : MonoBehaviour
{
    [SerializeField] private RoomGateController _doorPrefab;

    private RoomGateController _entryDoor;
    private RoomGateController _exitDoor;

    public bool IsInitialized { get; private set; }

    public bool TryInitialize(GeneratedRoomNode node, out string error)
    {
        DisposeDoors();

        if (node?.Room == null || node.Entry == null || node.Exit == null || _doorPrefab == null)
        {
            error = $"{name}: Encounter 초기화 정보가 부족합니다.";
            return false;
        }

        RoomEncounterSocket entrySocket = node.Entry.GetComponent<RoomEncounterSocket>();
        RoomEncounterSocket exitSocket = node.Exit.GetComponent<RoomEncounterSocket>();

        if (entrySocket == null || exitSocket == null || entrySocket.DoorAnchor == null || exitSocket.DoorAnchor == null)
        {
            error = $"{name}: 선택된 Connector의 Socket이 유효하지 않습니다.";
            return false;
        }

        _entryDoor = CreateDoor(entrySocket.DoorAnchor, "EntryDoor");

        _exitDoor = CreateDoor(exitSocket.DoorAnchor, "ExitDoor");

        if (_entryDoor == null || _exitDoor == null)
        {
            DisposeDoors();
            error = $"{name}: Door 생성에 실패했습니다.";
            return false;
        }

        IsInitialized = true;
        SetWaiting();

        error = null;
        return true;
    }

    public void SetWaiting()
    {
        if (!IsInitialized)
        {
            return;
        }

        _entryDoor.SetOpen(true);
        _exitDoor.SetOpen(false);
    }

    public void SetCombat()
    {
        if (!IsInitialized)
        {
            return;
        }

        _entryDoor.SetOpen(false);
        _exitDoor.SetOpen(false);
    }

    public void SetCleared()
    {
        if (!IsInitialized)
        {
            return;
        }

        _entryDoor.SetOpen(true);
        _exitDoor.SetOpen(true);
    }

    private RoomGateController CreateDoor(Transform anchor, string objectName)
    {
        RoomGateController door = Instantiate(_doorPrefab, anchor.position, anchor.rotation, anchor);

        if (door != null)
        {
            door.name = objectName;
        }

        return door;
    }

    private void DisposeDoors()
    {
        IsInitialized = false;

        if (_entryDoor != null)
        {
            Destroy(_entryDoor.gameObject); //TODO : Destroy를 안하는 로직 고려.
        }

        if (_exitDoor != null)
        {
            Destroy(_exitDoor.gameObject);
        }

        _entryDoor = null;
        _exitDoor = null;
    }
}