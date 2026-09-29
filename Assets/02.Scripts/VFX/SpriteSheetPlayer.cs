using System.Collections.Generic;
using UnityEngine;

// 스프라이트 시트의 한 줄(row) 또는 개별 프레임 텍스처 목록을 루프 재생한다.
// 에디터 모드에서도 재생되도록 ExecuteAlways (VFXPlatoons 에디터 스크립트가 루프 갱신을 요청함).
[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteSheetPlayer : MonoBehaviour
{
    public static readonly List<SpriteSheetPlayer> Active = new List<SpriteSheetPlayer>();

    [Header("시트 모드 (sheet 가 있으면 사용)")]
    public Texture2D sheet;
    public int row;          // 0 = 맨 위 줄
    public int cellSize = 64;

    [Header("프레임 모드")]
    public Texture2D[] frames;

    public float fps = 15f;
    public float pixelsPerUnit = 64f;
    public float timeOffset;

    Sprite[] _sprites;
    SpriteRenderer _sr;
    float _startTime;

    void OnEnable()
    {
        Active.Add(this);
        _sr = GetComponent<SpriteRenderer>();
        _startTime = Application.isPlaying ? Time.time : 0f; // 플레이 중엔 활성화 시점부터 첫 프레임으로 시작
        Build();
    }

    void OnDisable()
    {
        Active.Remove(this);
        Release();
    }

    void Build()
    {
        Release();
        _sprites = CreateSprites();
        Tick();
    }

    // 시트/프레임 설정으로 재생용 스프라이트 배열을 만든다. 호출한 쪽이 다 쓰면 파괴해야 한다.
    public Sprite[] CreateSprites()
    {
        var list = new List<Sprite>();
        var pivot = new Vector2(0.5f, 0.5f);
        if (sheet != null)
        {
            int cols = sheet.width / cellSize;
            float y = sheet.height - (row + 1) * cellSize;
            for (int c = 0; c < cols; c++)
                list.Add(Sprite.Create(sheet, new Rect(c * cellSize, y, cellSize, cellSize), pivot, pixelsPerUnit));
        }
        else if (frames != null)
        {
            foreach (var t in frames)
                if (t != null) list.Add(Sprite.Create(t, new Rect(0, 0, t.width, t.height), pivot, pixelsPerUnit));
        }
        foreach (var s in list) s.hideFlags = HideFlags.DontSave;
        return list.ToArray();
    }

    void Release()
    {
        if (_sprites == null) return;
        foreach (var s in _sprites)
            if (s != null) DestroyImmediate(s);
        _sprites = null;
    }

    void Update() => Tick();

    void Tick()
    {
        if (_sprites == null || _sprites.Length == 0 || _sr == null) return;
        float t = (Application.isPlaying ? Time.time - _startTime : Time.realtimeSinceStartup) + timeOffset;
        _sr.sprite = _sprites[(int)(t * fps) % _sprites.Length];
    }
}
