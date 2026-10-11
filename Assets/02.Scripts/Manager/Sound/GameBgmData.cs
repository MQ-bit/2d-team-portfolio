using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameBgmData", menuName = "Sound/Game BGM Data")]
public sealed class GameBgmData : ScriptableObject
{
    [Tooltip("같은 ID로 등록한 곡은 하나의 재생 목록으로 사용한다.")]
    [SerializeField] private GameSoundData.BgmEntry[] _entries = Array.Empty<GameSoundData.BgmEntry>();

    public GameSoundData.BgmEntry Get(GameBgmId id)
    {
        if (_entries == null) return null;
        foreach (GameSoundData.BgmEntry entry in _entries)
            if (entry != null && entry.Id == id) return entry;
        return null;
    }

    public GameSoundData.BgmEntry[] GetTracks(GameBgmId id)
    {
        var tracks = new List<GameSoundData.BgmEntry>();
        if (_entries != null)
        {
            foreach (GameSoundData.BgmEntry entry in _entries)
                if (entry != null && entry.Id == id) tracks.Add(entry);
        }

        return tracks.ToArray();
    }
}
