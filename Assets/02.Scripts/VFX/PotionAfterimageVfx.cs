using System.Collections.Generic;
using UnityEngine;

// 기화 반응 직후 바닥에 남는 잔상. 기화가 아닌 쪽 장판 범위(벽에 잘린 장판 영역) 안의 무작위 위치에 스프라이트를 뿌리고,
// 설정한 시간 동안 가장 진하게 유지한 뒤 서서히 사라지게 한다.
internal static class PotionAfterimageVfx
{
    // countMultiplier: 조합 장판처럼 원소가 둘일 때 각 원소 몫을 줄이는 배율.
    internal static void Spawn(PotionAfterimageSettings settings, PotionElement element, PotionAreaFootprint area,
        SpriteRenderer sortingRenderer, float countMultiplier = 1f)
    {
        if (settings == null || area == null || !settings.TryGetSet(element, out PotionAfterimageSettings.ElementSet set))
        {
            return;
        }

        int count = set.RandomCount(countMultiplier);
        if (count <= 0)
        {
            return;
        }

        var root = new GameObject("VaporizationAfterimage");
        var fade = root.AddComponent<PotionAfterimageFade>();
        Sprite[] sprites = set.Sprites;
        for (int i = 0; i < count; i++)
        {
            Sprite sprite = sprites[Random.Range(0, sprites.Length)];
            if (sprite == null)
            {
                continue;
            }

            var decal = new GameObject("Afterimage");
            decal.transform.SetParent(root.transform, false);
            decal.transform.position = area.SamplePoint();
            float scale = set.RandomScale();
            decal.transform.localScale = new Vector3(scale, scale, 1f);

            var renderer = decal.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = set.Tint;
            renderer.flipX = settings.RandomFlip && Random.value < 0.5f;
            if (sortingRenderer != null)
            {
                renderer.sortingLayerID = sortingRenderer.sortingLayerID;
                renderer.sortingOrder = sortingRenderer.sortingOrder + settings.SortingOrderOffset;
            }

            fade.Add(renderer);
        }

        fade.Begin(settings.HoldDuration, settings.FadeDuration);
    }
}

// 잔상 묶음을 한 번에 페이드시키고 끝나면 스스로 지운다. 생성 순간이 가장 진하다.
internal sealed class PotionAfterimageFade : MonoBehaviour
{
    private readonly List<SpriteRenderer> _renderers = new List<SpriteRenderer>();
    private readonly List<float> _baseAlphas = new List<float>();
    private float _hold;
    private float _fade;
    private float _elapsed;

    internal void Add(SpriteRenderer renderer)
    {
        _renderers.Add(renderer);
        _baseAlphas.Add(renderer.color.a);
    }

    internal void Begin(float hold, float fade)
    {
        _hold = hold;
        _fade = Mathf.Max(0.01f, fade);
        _elapsed = 0f;
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        if (_elapsed >= _hold + _fade)
        {
            Destroy(gameObject);
            return;
        }

        float alphaScale = _elapsed <= _hold ? 1f : 1f - (_elapsed - _hold) / _fade;
        for (int i = 0; i < _renderers.Count; i++)
        {
            Color color = _renderers[i].color;
            color.a = _baseAlphas[i] * alphaScale;
            _renderers[i].color = color;
        }
    }
}
