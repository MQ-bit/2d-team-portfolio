using UnityEngine;
using UnityEngine.Rendering;

// Batch flame strips in one mesh. Layout and clipping are computed once, never per pixel.
internal sealed class PotionFireVortexRenderer
{
    private const int InteriorCount = 28;
    private const int RimCount = 6;
    private const int FlameCount = InteriorCount + RimCount;
    private const int Segments = 8;
    private const int VerticesPerFlame = (Segments + 1) * 2;
    private static readonly int AccentId = Shader.PropertyToID("_FireAccent");
    private static readonly int AnimationId = Shader.PropertyToID("_FireAnimation");
    private static readonly int GroundColorId = Shader.PropertyToID("_FireGroundColor");
    private static readonly int SurfaceAnimationId = Shader.PropertyToID("_Animation");
    private static readonly int SurfaceColorId = Shader.PropertyToID("_AreaColor");
    private static readonly int SurfaceId = Shader.PropertyToID("_Surface");
    private static readonly int CurrentId = Shader.PropertyToID("_FireCurrent");
    private static Material _sharedMaterial;
    private static ShaderVariantCollection _variants;
    private MeshRenderer _renderer;
    private Mesh _mesh;
    private MaterialPropertyBlock _properties;
    private MaterialPropertyBlock _groundProperties;
    private PotionAreaFootprint _footprint;
    private Vector3[] _vertices;
    private Vector2[] _uv;
    private Vector4[] _flameData;
    private int[] _indices;
    private uint _seed;
    private bool _hasSeed;
    private float _startedAt;

    internal void SetSeed(uint seed)
    {
        _seed = seed;
        _hasSeed = true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetResources()
    {
        if (_sharedMaterial != null) Object.Destroy(_sharedMaterial);
        if (_variants != null) Object.Destroy(_variants);
        _sharedMaterial = null;
        _variants = null;
    }

    // Warm the simple shader during scene startup rather than the first potion impact.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Prewarm() => EnsureResources();

    private static bool EnsureResources()
    {
        if (_sharedMaterial != null) return true;
        Shader shader = Resources.Load<Shader>("PotionFireVortex");
        if (shader == null || !shader.isSupported) return false;
        _sharedMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        _variants = new ShaderVariantCollection { hideFlags = HideFlags.HideAndDontSave };
        _variants.Add(new ShaderVariantCollection.ShaderVariant(shader, PassType.ScriptableRenderPipeline));
        _variants.WarmUp();
        return true;
    }

    internal void Bind(SpriteRenderer ground, Object context)
    {
        if (!EnsureResources())
        {
            Debug.LogWarning("Fire shader unavailable; keeping the ground surface.", context);
            return;
        }
        if (_renderer == null)
        {
            var go = new GameObject("FireVortexFlames");
            var filter = go.AddComponent<MeshFilter>();
            _renderer = go.AddComponent<MeshRenderer>();
            _renderer.shadowCastingMode = ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _mesh = new Mesh { name = "PotionFlameStrips", hideFlags = HideFlags.HideAndDontSave };
            filter.sharedMesh = _mesh;
            _properties = new MaterialPropertyBlock();
            _groundProperties = new MaterialPropertyBlock();
            _vertices = new Vector3[FlameCount * VerticesPerFlame];
            _uv = new Vector2[_vertices.Length];
            _flameData = new Vector4[_vertices.Length];
            _indices = new int[FlameCount * Segments * 6];
        }
        if (!_hasSeed)
        {
            _seed = unchecked((uint)ground.GetInstanceID());
            _hasSeed = true;
        }
        // Snapshot the normalized ground transform, independent of animated sprite bounds.
        Transform target = _renderer.transform;
        target.SetParent(ground.transform.parent, false);
        target.localPosition = ground.transform.localPosition;
        target.localRotation = ground.transform.localRotation;
        target.localScale = ground.transform.localScale;
        _renderer.sharedMaterial = _sharedMaterial;
        _renderer.sortingLayerID = ground.sortingLayerID;
        _renderer.sortingOrder = ground.sortingOrder + 1;
        _startedAt = Time.time;
        BuildMesh();
        _renderer.enabled = false;
    }

    internal void SetFootprint(PotionAreaFootprint footprint)
    {
        if (ReferenceEquals(_footprint, footprint)) return;
        _footprint = footprint;
        if (_renderer != null) BuildMesh();
    }

    private static float Sample(uint seed, uint index, uint channel)
    {
        uint value = unchecked(seed + index * 747796405u + channel * 2891336453u);
        value = unchecked((value ^ (value >> 16)) * 2246822519u);
        value = unchecked((value ^ (value >> 13)) * 3266489917u);
        return ((value ^ (value >> 16)) & 0x00ffffffu) / 16777216f;
    }

    private void BuildMesh()
    {
        int triangle = 0;
        for (int i = 0; i < FlameCount; i++)
        {
            bool rim = i >= InteriorCount;
            float variation = Sample(_seed, (uint)i, 1);
            float angle = rim
                ? (i - InteriorCount) * (Mathf.PI * 2f / RimCount) + Sample(_seed, 0, 4) * 6.28f
                    + (variation - 0.5f) * 0.3f
                // A staggered spiral fills the interior without random clumps or empty patches.
                : i * 2.399963f + Sample(_seed, 0, 4) * Mathf.PI * 2f
                    + (Sample(_seed, (uint)i, 2) - 0.5f) * 0.24f;
            // Negative local Y faces the viewer: gently favor the front flames.
            float frontWeight = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(-Mathf.Sin(angle) * 1.4f));
            float radius = rim ? Mathf.Lerp(Mathf.Lerp(0.68f, 0.78f, frontWeight), 0.9f,
                    Sample(_seed, (uint)i, 6))
                : Mathf.Sqrt((i + 0.5f + (Sample(_seed, (uint)i, 3) - 0.5f) * 0.5f)
                    / InteriorCount) * 0.86f;
            var root = new Vector3(Mathf.Cos(angle) * radius * 0.5f,
                Mathf.Sin(angle) * radius * 0.5f, 0f);
            bool visible = _footprint == null || _footprint.Contains(_renderer.transform.TransformPoint(root));
            // Keep the perimeter low so the flames do not form a tall wall.
            float rimProfile = rim ? Mathf.Lerp(1f, 0.55f, frontWeight) : 0f;
            var data = new Vector4(variation, rimProfile,
                Sample(_seed, (uint)i, 5) * Mathf.PI * 2f, visible ? 1f : 0f);
            int first = i * VerticesPerFlame;
            for (int row = 0; row <= Segments; row++)
            {
                for (int side = 0; side < 2; side++)
                {
                    int vertex = first + row * 2 + side;
                    _vertices[vertex] = root;
                    // Extend slightly below the root for a soft contact patch on the ground.
                    _uv[vertex] = new Vector2(side * 2f - 1f, -0.12f + row * (1.67f / Segments));
                    _flameData[vertex] = data;
                }
                if (row == Segments) continue;
                int start = first + row * 2;
                _indices[triangle++] = start;
                _indices[triangle++] = start + 2;
                _indices[triangle++] = start + 1;
                _indices[triangle++] = start + 1;
                _indices[triangle++] = start + 2;
                _indices[triangle++] = start + 3;
            }
        }
        _mesh.Clear();
        _mesh.vertices = _vertices;
        _mesh.uv = _uv;
        _mesh.SetUVs(1, _flameData);
        _mesh.triangles = _indices;
        // Include vertex-shader expansion above roots in the culling bounds.
        _mesh.bounds = new Bounds(new Vector3(0f, 0.6f, 0f), new Vector3(1.25f, 2.5f, 0.1f));
    }

    internal void Apply(SpriteRenderer ground, Color accent, float time, float visibility)
    {
        if (_renderer == null) return;
        _renderer.enabled = ground.enabled && visibility > 0f;
        if (!_renderer.enabled) return;
        ground.GetPropertyBlock(_groundProperties);
        PotionAreaSurfaceRenderer.CopyCombinationProperties(_groundProperties, _properties);
        float appearance = _groundProperties.HasProperty(SurfaceAnimationId)
            ? _groundProperties.GetVector(SurfaceAnimationId).y : 1f;
        Color groundColor = _groundProperties.HasProperty(SurfaceColorId)
            ? _groundProperties.GetColor(SurfaceColorId) : new Color(0.58f, 0.055f, 0.025f);
        _properties.SetColor(AccentId, accent);
        _properties.SetColor(GroundColorId, groundColor);
        bool blazingCurrent = _groundProperties.HasProperty(SurfaceId)
            && Mathf.Abs(_groundProperties.GetVector(SurfaceId).w - (float)PotionAreaSurfaceStyle.BlazingCurrent) < 0.5f;
        // Share the ground's clock/seed so a discharge also energizes nearby flames.
        Vector4 animation = _groundProperties.GetVector(SurfaceAnimationId);
        _properties.SetVector(CurrentId, new Vector4(blazingCurrent ? 1f : 0f,
            animation.w, 0.68f, 0.85f));
        _properties.SetVector(AnimationId, new Vector4(time, visibility,
            Mathf.Lerp(0.15f, 1f, Mathf.Clamp01(appearance)), Mathf.Max(0f, Time.time - _startedAt)));
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
    }
}
