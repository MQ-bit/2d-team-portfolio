using UnityEngine;
using UnityEngine.Rendering;

// A short-lived visual snapshot, independent of the consumed healing floor.
public sealed class PotionPurifyingLifeVfx : MonoBehaviour
{
    private const float Duration = 1.6f;
    private static readonly int AnimationId = Shader.PropertyToID("_HealingMistAnimation");
    private readonly ComparisonAreaMaskRenderer _mask = new();
    private MeshRenderer _renderer;
    private Mesh _mesh;
    private Material _material;
    private MaterialPropertyBlock _properties;
    private float _startedAt;

    internal static void Spawn(Collider2D area, SpriteRenderer sorting, PotionAreaFootprint footprint)
    {
        Shader shader = Resources.Load<Shader>("PotionPurifyingLife");
        if (shader == null || !shader.isSupported)
        {
            Debug.LogWarning("Healing vaporization shader unavailable; skipping its visual.");
            return;
        }
        Bounds bounds = area.bounds;
        var go = new GameObject("PurifyingLifeMist");
        go.transform.position = bounds.center;
        go.transform.localScale = new Vector3(bounds.size.x, bounds.size.y, 1f);
        var visual = go.AddComponent<PotionPurifyingLifeVfx>();
        visual._startedAt = Time.time;
        visual._material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        visual._mesh = new Mesh { name = "HealingMistQuads", hideFlags = HideFlags.HideAndDontSave };
        visual._mesh.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f),
            new Vector3(-0.5f, 0f, 0f), new Vector3(0.5f, 0f, 0f),
            new Vector3(-0.5f, 1.1f, 0f), new Vector3(0.5f, 1.1f, 0f)
        };
        visual._mesh.uv = new[]
        {
            Vector2.zero, Vector2.right, Vector2.up, Vector2.one,
            Vector2.zero, Vector2.right, Vector2.up, Vector2.one
        };
        visual._mesh.SetUVs(1, new[]
        {
            Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero,
            Vector2.right, Vector2.right, Vector2.right, Vector2.right
        });
        visual._mesh.triangles = new[] { 0, 2, 1, 1, 2, 3, 4, 6, 5, 5, 6, 7 };
        visual._mesh.RecalculateBounds();
        go.AddComponent<MeshFilter>().sharedMesh = visual._mesh;
        visual._renderer = go.AddComponent<MeshRenderer>();
        visual._renderer.sharedMaterial = visual._material;
        visual._renderer.shadowCastingMode = ShadowCastingMode.Off;
        visual._renderer.receiveShadows = false;
        if (sorting != null)
        {
            visual._renderer.sortingLayerID = sorting.sortingLayerID;
            visual._renderer.sortingOrder = sorting.sortingOrder + 2;
        }
        visual._properties = new MaterialPropertyBlock();
        visual._mask.SetFootprint(footprint);
        visual._mask.Apply(visual._properties);
        visual.Update();
        Destroy(go, Duration);
    }

    private void Update()
    {
        if (_renderer == null) return;
        _properties.SetVector(AnimationId, new Vector4(
            Mathf.Clamp01((Time.time - _startedAt) / Duration), Time.time - _startedAt, 0f, 0f));
        _renderer.SetPropertyBlock(_properties);
    }

    private void OnDestroy()
    {
        _mask.Release();
        if (_mesh != null) Destroy(_mesh);
        if (_material != null) Destroy(_material);
    }
}
