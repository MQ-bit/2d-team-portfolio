using System;
using UnityEngine;
using UnityEngine.InputSystem;

// VFX 테스트 씬 전용: Ctrl+Shift+[ / ] 로 장판에 넣을 VFX를 하나씩 넘겨 본다.
public class VFXFieldTestController : MonoBehaviour
{
    [Serializable]
    private struct Entry
    {
        public GameObject Prefab;
        public Color GroundColor;
        public bool HideGround;
        public bool RapidBurst; // 켜면 루프 대신 폭발처럼 짧은 간격으로 따라락 터지고 끝난다
    }

    [SerializeField] private VFXBurstField _field;
    [SerializeField] private TextMesh _label;
    [SerializeField] private Entry[] _entries;
    // 에디터 Game 뷰는 VSync가 꺼져 있으면 프레임 제한 없이 돌아 GPU를 계속 점유한다.
    [SerializeField] private int _targetFrameRate = 60;

    private int _index;

    private void Start()
    {
        Application.targetFrameRate = _targetFrameRate;
        Show(0);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.ctrlKey.isPressed || !keyboard.shiftKey.isPressed)
        {
            return;
        }

        if (keyboard.leftBracketKey.wasPressedThisFrame)
        {
            Show(_index - 1);
        }
        else if (keyboard.rightBracketKey.wasPressedThisFrame)
        {
            Show(_index + 1);
        }
    }

    private void Show(int index)
    {
        int count = _entries.Length;
        if (count == 0 || _field == null)
        {
            return;
        }

        _index = (index % count + count) % count;
        Entry entry = _entries[_index];
        _field.SetGroundColor(entry.GroundColor);
        _field.SetGroundVisible(!entry.HideGround);
        _field.SetRapidBurst(entry.RapidBurst);
        _field.SetVfx(entry.Prefab);

        if (_label != null)
        {
            _label.text = $"{entry.Prefab.name}   ({_index + 1} / {count})\n<size=32>Ctrl+Shift+[ / ]</size>";
        }
    }
}