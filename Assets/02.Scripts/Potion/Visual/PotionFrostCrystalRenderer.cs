using UnityEngine;
using UnityEngine.Rendering;

// Static faceted crystals, batched in one mesh and rooted in the ground footprint.
internal sealed class PotionFrostCrystalRenderer
{
    private const int CrystalCount = 30;
    private const int MistCount = 14;
    private static readonly int AccentId = Shader.PropertyToID("_IceAccent");
    private static readonly int PoisonId = Shader.PropertyToID("_IcePoison");
    private static readonly int AnimationId = Shader.PropertyToID("_IceAnimation");
    private static readonly int SurfaceAnimationId = Shader.PropertyToID("_Animation");
    private static readonly int SurfaceId = Shader.PropertyToID("_Surface");
    private static readonly int ClipMaskId = Shader.PropertyToID("_AreaClipMask");
    private static readonly int ClipRectId = Shader.PropertyToID("_AreaClipRect");
    private static readonly int ClipEnabledId = Shader.PropertyToID("_AreaClipEnabled");
    private MeshRenderer _renderer;
    private Mesh _mesh;
    private Material _material;
    private MaterialPropertyBlock _properties;
    private MaterialPropertyBlock _groundProperties;
    private PotionAreaFootprint _footprint;
    private uint _seed;
    private bool _hasSeed;

    internal void SetSeed(uint seed)
    {
        _seed = seed;
        _hasSeed = true;
    }

    internal void Bind(SpriteRenderer ground, Object context)
    {
        if (_material == null)
        {
            Shader shader = Resources.Load<Shader>("PotionFrostCrystals");
            if (shader == null || !shader.isSupported)
            {
                Debug.LogWarning("Ice crystal shader unavailable; keeping the frost surface.", context);
                return;
            }
            _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }
        if (_renderer == null)
        {
            var go = new GameObject("FrostCrystals");
            var filter = go.AddComponent<MeshFilter>();
            _renderer = go.AddComponent<MeshRenderer>();
            _renderer.shadowCastingMode = ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _mesh = new Mesh { name = "PotionIceFacets", hideFlags = HideFlags.HideAndDontSave };
            filter.sharedMesh = _mesh;
            _properties = new MaterialPropertyBlock();
            _groundProperties = new MaterialPropertyBlock();
        }
        if (!_hasSeed)
        {
            _seed = unchecked((uint)ground.GetInstanceID());
            _hasSeed = true;
        }
        Transform target = _renderer.transform;
        target.SetParent(ground.transform.parent, false);
        target.localPosition = ground.transform.localPosition;
        target.localRotation = ground.transform.localRotation;
        target.localScale = ground.transform.localScale;
        _renderer.sharedMaterial = _material;
        _renderer.sortingLayerID = ground.sortingLayerID;
        _renderer.sortingOrder = ground.sortingOrder + 1;
        BuildMesh();
        _renderer.enabled = false;
    }

    internal void SetFootprint(PotionAreaFootprint footprint)
    {
        if (ReferenceEquals(_footprint, footprint)) return;
        _footprint = footprint;
        if (_renderer != null) BuildMesh();
    }

    private float Sample(int index, uint channel)
    {
        uint value = unchecked(_seed + (uint)index * 747796405u + channel * 2891336453u);
        value = unchecked((value ^ (value >> 16)) * 2246822519u);
        return ((value ^ (value >> 13)) & 0x00ffffffu) / 16777216f;
    }

    private void BuildMesh()
    {
        var vertices = new Vector3[CrystalCount * 9 + MistCount * 4];
        var colors = new Color[vertices.Length];
        var roots = new Vector2[vertices.Length];
        var mistData = new Vector3[vertices.Length];
        var indices = new int[CrystalCount * 9 + MistCount * 6];
        int vertex = 0;
        for (int i = 0; i < CrystalCount; i++)
        {
            bool rim = i < 22;
            float angle = rim ? i * (Mathf.PI * 2f / 22f) : (i - 22) * 2.399963f;
            angle += Sample(0, 1) * Mathf.PI * 2f + (Sample(i, 2) - 0.5f) * 0.14f;
            Vector2 outward = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            float radius = rim ? Mathf.Lerp(0.68f, 0.79f, Sample(i, 3))
                : Mathf.Lerp(0.32f, 0.6f, Sample(i, 3));
            Vector2 root = outward * (radius * 0.5f);
            if (_footprint != null && !_footprint.Contains(_renderer.transform.TransformPoint(root)))
            {
                // Leave blocked portions empty rather than drawing crystals through walls.
                vertex += 9;
                continue;
            }
            float length = rim ? Mathf.Lerp(0.075f, 0.14f, Sample(i, 4))
                : Mathf.Lerp(0.05f, 0.09f, Sample(i, 4));
            float width = Mathf.Lerp(0.018f, 0.032f, Sample(i, 5));
            Vector2 axis = (outward + Vector2.up * 0.5f).normalized;
            Vector2 across = new Vector2(-axis.y, axis.x);
            Vector2 left = root - across * width;
            Vector2 right = root + across * width;
            Vector2 tip = root + axis * length;
            Vector2 ridge = root + axis * (length * 0.28f);
            // Keep each whole shard inside the gameplay footprint as well as its root.
            if (_footprint != null && (!_footprint.Contains(_renderer.transform.TransformPoint(tip))
                || !_footprint.Contains(_renderer.transform.TransformPoint(left))
                || !_footprint.Contains(_renderer.transform.TransformPoint(right))))
            {
                vertex += 9;
                continue;
            }
            AddVertex(left, root, new Color(0.36f, 0.67f, 0.74f, 0.42f), vertices, colors, roots, indices, ref vertex);
            AddVertex(tip, root, new Color(0.92f, 1f, 1f, 0.72f), vertices, colors, roots, indices, ref vertex);
            AddVertex(ridge, root, new Color(0.72f, 0.94f, 1f, 0.68f), vertices, colors, roots, indices, ref vertex);
            AddVertex(tip, root, new Color(0.92f, 1f, 1f, 0.72f), vertices, colors, roots, indices, ref vertex);
            AddVertex(right, root, new Color(0.23f, 0.52f, 0.64f, 0.34f), vertices, colors, roots, indices, ref vertex);
            AddVertex(ridge, root, new Color(0.43f, 0.76f, 0.85f, 0.58f), vertices, colors, roots, indices, ref vertex);
            AddVertex(left, root, new Color(0.36f, 0.67f, 0.74f, 0.18f), vertices, colors, roots, indices, ref vertex);
            AddVertex(ridge, root, new Color(0.72f, 0.94f, 1f, 0.45f), vertices, colors, roots, indices, ref vertex);
            AddVertex(right, root, new Color(0.23f, 0.52f, 0.64f, 0.18f), vertices, colors, roots, indices, ref vertex);
        }
        for (int i = 0; i < MistCount; i++)
        {
            float angle = i * 2.399963f + Sample(0, 7) * Mathf.PI * 2f;
            float radius = Mathf.Sqrt((i + 0.5f) / MistCount) * 0.66f;
            Vector2 root = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (radius * 0.5f);
            int first = CrystalCount * 9 + i * 4;
            if (_footprint != null && !_footprint.Contains(_renderer.transform.TransformPoint(root))) continue;
            float width = Mathf.Lerp(0.11f, 0.17f, Sample(i, 8));
            float height = Mathf.Lerp(0.24f, 0.36f, Sample(i, 9));
            for (int corner = 0; corner < 4; corner++)
            {
                float x = (corner % 2) * 2f - 1f;
                float y = corner / 2;
                vertices[first + corner] = root + new Vector2(x * width, y * height);
                colors[first + corner] = Color.white;
                roots[first + corner] = root;
                mistData[first + corner] = new Vector3(x, y, 1f);
            }
            int triangle = CrystalCount * 9 + i * 6;
            indices[triangle] = first;
            indices[triangle + 1] = first + 2;
            indices[triangle + 2] = first + 1;
            indices[triangle + 3] = first + 1;
            indices[triangle + 4] = first + 2;
            indices[triangle + 5] = first + 3;
        }
        _mesh.Clear();
        _mesh.vertices = vertices;
        _mesh.colors = colors;
        _mesh.uv = roots;
        _mesh.SetUVs(1, mistData);
        _mesh.triangles = indices;
        _mesh.bounds = new Bounds(new Vector3(0f, 0.1f, 0f), new Vector3(1.4f, 1.6f, 0.1f));
    }

    private static void AddVertex(Vector2 position, Vector2 root, Color color, Vector3[] vertices,
        Color[] colors, Vector2[] roots, int[] indices, ref int index)
    {
        vertices[index] = position;
        colors[index] = color;
        roots[index] = root;
        indices[index] = index;
        index++;
    }

    internal void Apply(SpriteRenderer ground, Color accent, float visibility)
    {
        if (_renderer == null) return;
        _renderer.enabled = ground.enabled && visibility > 0f;
        if (!_renderer.enabled) return;
        ground.GetPropertyBlock(_groundProperties);
        PotionAreaSurfaceRenderer.CopyCombinationProperties(_groundProperties, _properties);
        Vector4 animation = _groundProperties.GetVector(SurfaceAnimationId);
        bool thermalShock = Mathf.Abs(_groundProperties.GetVector(SurfaceId).w
            - (float)PotionAreaSurfaceStyle.ThermalShock) < 0.5f;
        _renderer.sortingOrder = ground.sortingOrder + (thermalShock ? 2 : 1);
        _properties.SetColor(AccentId, accent);
        _properties.SetFloat(PoisonId, Mathf.Abs(_groundProperties.GetVector(SurfaceId).w
            - (float)PotionAreaSurfaceStyle.FrostPoison) < 0.5f ? 1f : 0f);
        // Share the floor mask; rising mist is clipped by its projected ground support.
        _properties.SetFloat(ClipEnabledId, _groundProperties.GetFloat(ClipEnabledId));
        _properties.SetVector(ClipRectId, _groundProperties.GetVector(ClipRectId));
        _properties.SetTexture(ClipMaskId, _groundProperties.GetTexture(ClipMaskId) ?? Texture2D.whiteTexture);
        _properties.SetVector(AnimationId, new Vector4(
            Mathf.Lerp(0.15f, 1f, animation.y), visibility, animation.x, thermalShock ? 1f : 0f));
        _renderer.SetPropertyBlock(_properties);
    }

    internal void Hide()
    {
        if (_renderer != null) _renderer.enabled = false;
    }

    internal void Release()
    {
        if (_renderer != null) Object.Destroy(_renderer.gameObject);
        if (_mesh != null) Object.Destroy(_mesh);
        if (_material != null) Object.Destroy(_material);
    }
}
