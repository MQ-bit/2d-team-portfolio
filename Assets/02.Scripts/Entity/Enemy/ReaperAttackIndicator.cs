using System.Collections.Generic;
using UnityEngine;

// 돌진과 포션 낙하가 공유하는 바닥 경고. 월드 크기로 생성해 적의 스케일에 영향을 받지 않는다.
public sealed class ReaperAttackIndicator : MonoBehaviour
{
    [SerializeField] private Color _color = new Color(0.56f, 0.16f, 0.38f, 1f);
    [SerializeField] private Color _runeColor = new Color(0.65f, 0.39f, 0.85f, 1f);
    [SerializeField] private Color _chargedColor = new Color(0.94f, 0.8f, 0.68f, 1f);
    [SerializeField, Range(0f, 1f)] private float _backgroundAlpha = 0.12f;
    [SerializeField, Range(0f, 1f)] private float _fillAlpha = 0.32f;
    [SerializeField, Min(0.01f)] private float _outlineWidth = 0.045f;
    [Header("Potion landing warning")]
    [SerializeField] private Sprite _landingAreaSprite;
    [SerializeField] private Sprite _landingMarkerSprite;
    private SpriteRenderer _landingArea, _landingMarker;
    private MeshRenderer _fillRenderer, _ornamentRenderer;
    private bool _isPotionLanding;
    private Color _potionAreaColor;
    private const int Segments = 48;
    private readonly Vector3[] _boundary = new Vector3[Segments];
    private readonly Vector3[] _raggedBoundary = new Vector3[Segments];
    private readonly Vector3[] _fillVertices = new Vector3[Segments + 1];
    private readonly Color[] _colors = new Color[Segments + 1];
    private Mesh _background, _fill;
    private MeshRenderer _backgroundRenderer;
    private Material _material;
    private LineRenderer _outline;
    private LineRenderer _outlineShadow;
    private Mesh _ornaments;
    private Color[] _ornamentColors;
    private float[] _ornamentProgress;
    private bool _isDash;
    private bool _isSteal;
    private Color _stealAccentColor;
    private Vector3[] _runeVertices, _animatedRuneVertices;
    private Mesh _inwardMotes;
    private readonly Vector3[] _moteVertices = new Vector3[48];
    private readonly Color[] _moteColors = new Color[48];
    private float _length, _radius;

    private void Awake()
    {
        _material = new Material(Shader.Find("Sprites/Default"));
        _background = CreateMesh(gameObject, "Warning background", 0);
        _backgroundRenderer = GetComponent<MeshRenderer>();
        GameObject fill = new GameObject("Charge fill");
        fill.transform.SetParent(transform, false);
        _fill = CreateMesh(fill, "Warning fill", 1);
        _fillRenderer = fill.GetComponent<MeshRenderer>();
        GameObject outline = new GameObject("Warning outline");
        outline.transform.SetParent(transform, false);
        _outline = outline.AddComponent<LineRenderer>();
        _outline.sharedMaterial = _material;
        _outline.useWorldSpace = false;
        _outline.loop = true;
        _outline.widthMultiplier = _outlineWidth;
        _outline.positionCount = Segments;
        _outline.sortingLayerName = "FieldUI";
        _outline.sortingOrder = 2;
        GameObject shadow = new GameObject("Dark border");
        shadow.transform.SetParent(transform, false);
        _outlineShadow = shadow.AddComponent<LineRenderer>();
        _outlineShadow.sharedMaterial = _material;
        _outlineShadow.useWorldSpace = false;
        _outlineShadow.loop = true;
        _outlineShadow.widthMultiplier = _outlineWidth * 2.4f;
        _outlineShadow.positionCount = Segments;
        _outlineShadow.sortingLayerName = "FieldUI";
        _outlineShadow.sortingOrder = 1;
        _outlineShadow.startColor = _outlineShadow.endColor = new Color(0.09f, 0.045f, 0.12f, 0.9f);
        GameObject ornaments = new GameObject("Runes and direction marks");
        ornaments.transform.SetParent(transform, false);
        _ornaments = new Mesh { name = "Occult warning ornaments" };
        _ornaments.MarkDynamic();
        ornaments.AddComponent<MeshFilter>().sharedMesh = _ornaments;
        MeshRenderer ornamentRenderer = ornaments.AddComponent<MeshRenderer>();
        _ornamentRenderer = ornamentRenderer;
        ornamentRenderer.sharedMaterial = _material;
        ornamentRenderer.sortingLayerName = "FieldUI";
        ornamentRenderer.sortingOrder = 3;
    }

    private Mesh CreateMesh(GameObject obj, string meshName, int order)
    {
        Mesh mesh = new Mesh { name = meshName };
        mesh.MarkDynamic();
        obj.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = obj.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = _material;
        renderer.sortingLayerName = "FieldUI";
        renderer.sortingOrder = order;
        mesh.vertices = new Vector3[Segments + 1];
        int[] triangles = new int[Segments * 3];
        for (int i = 0; i < Segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = (i + 1) % Segments + 1;
        }
        mesh.triangles = triangles;
        return mesh;
    }

    public void ApplyPalette(ReaperData data)
    {
        _color = data.IndicatorColor;
        _runeColor = data.IndicatorRuneColor;
        _chargedColor = data.IndicatorChargedColor;
    }

    public void ApplyStealPalette(ReaperData data)
    {
        _color = data.StealIndicatorColor;
        _runeColor = data.StealRuneColor;
        _stealAccentColor = data.StealAccentColor;
        _chargedColor = data.StealChargedColor;
    }

    public void SetDash(Vector2 origin, Vector2 direction, float length, float radius)
    {
        _isPotionLanding = false;
        _isSteal = false;
        transform.position = origin;
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        if (_isDash && Mathf.Approximately(_length, length) && Mathf.Approximately(_radius, radius)) return;
        _isDash = true;
        _length = length;
        _radius = radius;
        for (int i = 0; i < Segments; i++)
        {
            // 직선 부분에도 꼭짓점을 둬서 찢어진 가장자리를 만든다.
            int section = i / 12;
            float t = (i % 12) / 12f;
            if (section == 0)
            {
                _boundary[i] = new Vector3(length * t, -radius, 0f);
            }
            else if (section == 2)
            {
                _boundary[i] = new Vector3(length * (1f - t), radius, 0f);
            }
            else
            {
                float angle = (section == 1 ? -0.5f : 0.5f) * Mathf.PI + t * Mathf.PI;
                _boundary[i] = new Vector3((section == 1 ? length : 0f) + Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius, 0f);
            }
            Vector3 center = Vector3.right * (length * 0.5f);
            float inset = 0.83f + ((i * 17 + 3) % 11) * 0.017f;
            _raggedBoundary[i] = center + (_boundary[i] - center) * inset;
        }
        RefreshShape();
    }

    public void SetArea(Vector2 center, float radius)
    {
        _isPotionLanding = false;
        _isSteal = false;
        transform.position = center;
        transform.rotation = Quaternion.identity;
        _isDash = false;
        _radius = radius;
        for (int i = 0; i < Segments; i++)
        {
            float angle = i * Mathf.PI * 2f / Segments;
            _boundary[i] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * 0.5f, 0f);
        }
        RefreshShape();
    }

    public void SetPotionArea(Vector2 center, float radius, Color areaColor)
    {
        SetArea(center, radius);
        if (_landingAreaSprite == null) return;
        _isPotionLanding = true;
        _potionAreaColor = areaColor;
        _backgroundRenderer.enabled = _fillRenderer.enabled = false;
        _ornamentRenderer.enabled = _outline.enabled = _outlineShadow.enabled = true;
        _outlineShadow.startColor = _outlineShadow.endColor = new Color(0.12f, 0.015f, 0.01f, 0.85f);
        _outlineShadow.widthMultiplier = _outlineWidth * 3.2f;
        BuildOrnaments();
        if (_landingArea == null) _landingArea = CreateLandingSprite("Potion danger area", _landingAreaSprite, 1);
        // 투척 미리보기와 같은 월드 지름 및 2:1 타원 비율.
        ScaleLandingSprite(_landingArea, radius * 2f, radius);
        SetProgress(0f);
    }

    private SpriteRenderer CreateLandingSprite(string objectName, Sprite sprite, int order)
    {
        GameObject obj = new GameObject(objectName);
        obj.transform.SetParent(transform, false);
        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sharedMaterial = _material;
        renderer.sortingLayerName = "FieldUI";
        renderer.sortingOrder = order;
        return renderer;
    }

    private void ScaleLandingSprite(SpriteRenderer renderer, float width, float height)
    {
        Vector2 size = renderer.sprite.bounds.size;
        Vector3 parentScale = transform.lossyScale;
        renderer.transform.localScale = new Vector3(
            width / Mathf.Max(0.001f, size.x * Mathf.Abs(parentScale.x)),
            height / Mathf.Max(0.001f, size.y * Mathf.Abs(parentScale.y)), 1f);
    }

    public void SetStealArea(Vector2 center, float radius)
    {
        // 판정 테두리는 고정하고 내부만 수축시킨다.
        SetArea(center, radius);
        _isSteal = true;
        if (_inwardMotes == null)
        {
            GameObject motes = new GameObject("Inward soul fragments");
            motes.transform.SetParent(transform, false);
            _inwardMotes = new Mesh { name = "Steal inward motes" };
            _inwardMotes.MarkDynamic();
            motes.AddComponent<MeshFilter>().sharedMesh = _inwardMotes;
            MeshRenderer renderer = motes.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _material;
            renderer.sortingLayerName = "FieldUI";
            renderer.sortingOrder = 4;
            _inwardMotes.vertices = _moteVertices;
            int[] triangles = new int[72];
            for (int i = 0; i < 12; i++)
            {
                int v = i * 4, t = i * 6;
                triangles[t] = v; triangles[t + 1] = v + 1; triangles[t + 2] = v + 2;
                triangles[t + 3] = v; triangles[t + 4] = v + 2; triangles[t + 5] = v + 3;
            }
            _inwardMotes.triangles = triangles;
        }
        RefreshShape();
    }

    private void RefreshShape()
    {
        _outline.enabled = _fillRenderer.enabled = _ornamentRenderer.enabled = true;
        if (_landingArea != null) _landingArea.enabled = false;
        if (_landingMarker != null) _landingMarker.enabled = false;
        // 돌진에는 둥근 바탕을 깔지 않고 거친 충전 표시와 칼자국만 남긴다.
        _backgroundRenderer.enabled = !_isDash;
        _outlineShadow.enabled = !_isDash;
        _fillVertices[0] = _isDash ? Vector3.right * (_length * 0.5f) : Vector3.zero;
        for (int i = 0; i < Segments; i++) _fillVertices[i + 1] = _boundary[i];
        _background.vertices = _fillVertices;
        SetColors(_background, _isSteal ? 0.055f : _backgroundAlpha);
        _background.RecalculateBounds();
        _outline.SetPositions(_isDash ? _raggedBoundary : _boundary);
        _outlineShadow.SetPositions(_boundary);
        _outlineShadow.startColor = _outlineShadow.endColor = new Color(0.09f, 0.045f, 0.12f, _isDash ? 0.45f : 0.9f);
        _outline.widthMultiplier = _outlineWidth * (_isDash ? 1.25f : 1f);
        BuildOrnaments();
        SetProgress(0f);
    }

    private void BuildOrnaments()
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        var thresholds = new List<float>();
        float stroke = _outlineWidth * (_isPotionLanding ? 1.15f : _isDash ? 0.95f : 0.55f);
        void Line(Vector2 a, Vector2 b, float threshold)
        {
            Vector2 normal = new Vector2(-(b - a).y, (b - a).x).normalized * stroke * 0.5f;
            int first = vertices.Count;
            vertices.Add(a + normal); vertices.Add(b + normal);
            vertices.Add(b - normal); vertices.Add(a - normal);
            triangles.Add(first); triangles.Add(first + 1); triangles.Add(first + 2);
            triangles.Add(first); triangles.Add(first + 2); triangles.Add(first + 3);
            for (int i = 0; i < 4; i++) thresholds.Add(threshold);
        }
        void Diamond(Vector2 center, Vector2 axis, Vector2 cross, float size, float threshold)
        {
            Vector2 a = center + axis * size, b = center + cross * size * 0.6f;
            Vector2 c = center - axis * size, d = center - cross * size * 0.6f;
            Line(a, b, threshold); Line(b, c, threshold); Line(c, d, threshold); Line(d, a, threshold);
        }
        if (_isPotionLanding)
        {
            Vector2 Point(float angle, float scale) => new Vector2(Mathf.Cos(angle) * _radius * scale,
                Mathf.Sin(angle) * _radius * scale * 0.5f);
            // 12개의 끊어진 경고 링과 안쪽을 가리키는 날카로운 표식. 중앙은 비워둔다.
            for (int i = 0; i < 12; i++)
            {
                float angle = i * Mathf.PI * 2f / 12f;
                float threshold = (i + 1f) / 12f;
                for (int j = 0; j < 4; j++)
                    Line(Point(angle + j * 0.085f, 0.9f), Point(angle + (j + 1) * 0.085f, 0.9f), threshold);
                float toothAngle = angle + 0.17f;
                Vector2 tip = Point(toothAngle, 0.73f);
                Line(Point(toothAngle - 0.06f, 0.84f), tip, threshold);
                Line(tip, Point(toothAngle + 0.06f, 0.84f), threshold);
                if (i % 2 == 0)
                {
                    Vector2 bend = Point(toothAngle + 0.03f, 0.61f);
                    Line(Point(toothAngle - 0.01f, 0.67f), bend, threshold);
                    Line(bend, Point(toothAngle - 0.04f, 0.51f), threshold);
                }
            }
        }
        else if (_isDash)
        {
            // 일정한 화살표 대신 길이가 다른 사선 흉터와 끊어진 균열을 넣는다.
            int count = Mathf.Clamp(Mathf.FloorToInt(_length / 0.45f), 1, 15);
            for (int i = 0; i < count; i++)
            {
                float t = (i + 0.5f) / count;
                float x = _length * t;
                float size = Mathf.Min(_length / count * 0.38f, _radius * 0.35f);
                float side = i % 2 == 0 ? 1f : -1f;
                float reach = _radius * (0.38f + ((i * 7) % 5) * 0.07f);
                Vector2 bend = new Vector2(x + size * 0.3f, side * _radius * 0.1f);
                Line(new Vector2(x - size, -side * reach * 0.55f), bend, t);
                Line(bend, new Vector2(x + size, side * reach), t);
                if (i % 3 != 0)
                    Line(bend, new Vector2(x - size * 0.6f, side * reach * 0.58f), t);
            }
            float head = Mathf.Min(_radius * 0.45f, _length * 0.3f);
            Line(new Vector2(_length - head, -_radius * 0.42f), new Vector2(_length + _radius * 0.38f, 0f), 0.9f);
            Line(new Vector2(_length + _radius * 0.38f, 0f), new Vector2(_length - head * 0.6f, _radius * 0.28f), 0.9f);
        }
        else
        {
            Vector2 Point(float angle, float scale) => new Vector2(Mathf.Cos(angle) * _radius * scale,
                Mathf.Sin(angle) * _radius * scale * 0.5f);
            // 끊어진 안쪽 원과 각진 룬 문양. 바깥 테두리는 실제 위험 범위를 표시한다.
            for (int i = 0; i < 24; i++)
            {
                float angle = i * Mathf.PI * 2f / 24f;
                for (int j = 0; j < 2; j++)
                    Line(Point(angle + j * 0.07f, 0.72f), Point(angle + (j + 1) * 0.07f, 0.72f), i / 24f);
            }
            for (int i = 0; i < 12; i++)
            {
                float angle = i * Mathf.PI * 2f / 12f;
                Vector2 center = Point(angle, 0.86f);
                Vector2 radial = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * 0.5f);
                Vector2 tangent = new Vector2(-Mathf.Sin(angle), Mathf.Cos(angle) * 0.5f);
                float size = Mathf.Min(_radius * 0.09f, 0.25f);
                Diamond(center, radial, tangent, size, i / 12f);
                Line(center - radial * size * 1.2f, center + radial * size * 1.2f, i / 12f);
            }
            float seal = Mathf.Min(_radius * 0.23f, 0.6f);
            Diamond(Vector2.zero, Vector2.right, Vector2.up * 0.5f, seal, 0.4f);
            Diamond(Vector2.zero, Vector2.up * 0.5f, Vector2.right, seal, 0.7f);
            Line(new Vector2(-seal * 0.35f, 0f), new Vector2(seal * 0.35f, 0f), 0.9f);
        }
        _ornaments.Clear();
        _ornaments.SetVertices(vertices);
        _ornaments.SetTriangles(triangles, 0);
        _ornamentProgress = thresholds.ToArray();
        _ornamentColors = new Color[vertices.Count];
        _runeVertices = vertices.ToArray();
        _animatedRuneVertices = new Vector3[vertices.Count];
        _ornaments.RecalculateBounds();
    }

    public void SetProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);
        if (_isPotionLanding)
        {
            float landingPulse = 0.5f + 0.5f * Mathf.Sin(progress * Mathf.PI * (4f + progress * 8f));
            float urgency = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.7f, 1f, progress));
            Color area = _potionAreaColor;
            area.a = Mathf.Lerp(0.28f, 0.5f, progress) + urgency * landingPulse * 0.14f;
            _landingArea.enabled = true;
            _landingArea.color = area;
            Color edge = Color.Lerp(new Color(0.75f, 0.075f, 0.035f, 0.75f),
                new Color(1f, 0.24f, 0.08f, 1f), progress);
            _outline.startColor = _outline.endColor = edge;
            _outline.widthMultiplier = _outlineWidth * (1.25f + urgency * landingPulse * 0.5f);
            for (int i = 0; i < _ornamentColors.Length; i++)
            {
                bool charged = progress >= _ornamentProgress[i];
                Color mark = charged ? edge : new Color(0.6f, 0.06f, 0.025f, 1f);
                mark.a = charged ? 0.8f + urgency * landingPulse * 0.2f : 0.4f;
                _ornamentColors[i] = mark;
            }
            _ornaments.colors = _ornamentColors;
            return;
        }
        if (_isSteal) { SetStealProgress(progress); return; }
        float end = Mathf.Lerp(-_radius, _length + _radius, progress);
        _fillVertices[0] = _isDash ? new Vector3(Mathf.Min(_length * 0.5f, end), 0f, 0f) : Vector3.zero;
        for (int i = 0; i < Segments; i++)
        {
            Vector3 vertex = _isDash ? _raggedBoundary[i] : _boundary[i];
            if (_isDash) vertex.x = Mathf.Min(vertex.x, end);
            else vertex *= Mathf.Sqrt(progress);
            _fillVertices[i + 1] = vertex;
        }
        _fill.vertices = _fillVertices;
        float pulse = Mathf.Sin(Time.time * 12f) * 0.5f + 0.5f;
        SetColors(_fill, _fillAlpha + (progress > 0.8f ? pulse * 0.08f : 0f));
        _fill.RecalculateBounds();
        Color chargedColor = _isDash ? Color.Lerp(_chargedColor, _color, 0.35f) : _chargedColor;
        Color outline = Color.Lerp(_runeColor, chargedColor, progress * progress);
        outline.a = Mathf.Lerp(0.55f, 1f, progress);
        _outline.startColor = _outline.endColor = outline;
        for (int i = 0; i < _ornamentColors.Length; i++)
        {
            bool charged = progress >= _ornamentProgress[i];
            Color rune = charged ? Color.Lerp(_runeColor, chargedColor, progress) : _runeColor;
            rune.a = charged ? 0.75f + (progress > 0.8f ? pulse * 0.2f : 0f) : 0.3f;
            _ornamentColors[i] = rune;
        }
        _ornaments.colors = _ornamentColors;
    }

    private void SetStealProgress(float progress)
    {
        float urgency = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.7f, 1f, progress));
        float pulse = 0.5f + 0.5f * Mathf.Sin(progress * Mathf.PI * (6f + progress * 6f));
        Color glow = Color.Lerp(Color.Lerp(_runeColor, _stealAccentColor, progress * 0.65f),
            _chargedColor, urgency);
        glow.a = 0.65f + urgency * (0.2f + pulse * 0.15f);
        _outline.startColor = _outline.endColor = glow;
        _outline.widthMultiplier = _outlineWidth * (1f + urgency * pulse * 0.35f);

        // 크기 변화는 내부 문양에만 적용한다. 타원 비율은 판정과 동일하게 유지한다.
        float angle = -progress * Mathf.PI * 0.65f;
        float c = Mathf.Cos(angle), s = Mathf.Sin(angle);
        float scale = Mathf.Lerp(1f, 0.48f, progress);
        for (int i = 0; i < _runeVertices.Length; i++)
        {
            Vector3 v = _runeVertices[i];
            float y = v.y * 2f;
            _animatedRuneVertices[i] = new Vector3((v.x * c - y * s) * scale,
                (v.x * s + y * c) * scale * 0.5f, 0f);
            float tint = 0.5f + 0.5f * Mathf.Sin(_ornamentProgress[i] * Mathf.PI * 4f - progress * Mathf.PI * 2f);
            Color rune = Color.Lerp(Color.Lerp(_runeColor, _stealAccentColor, tint), _chargedColor, urgency * 0.55f);
            rune.a = 0.32f + progress * 0.3f + urgency * pulse * 0.25f;
            _ornamentColors[i] = rune;
        }
        _ornaments.vertices = _animatedRuneVertices;
        _ornaments.colors = _ornamentColors;
        _ornaments.RecalculateBounds();

        // 소환처럼 차오르는 장판 대신 중심에 작은 흡수핵을 남긴다.
        _fillVertices[0] = Vector3.zero;
        for (int i = 0; i < Segments; i++)
            _fillVertices[i + 1] = _boundary[i] * (0.12f + urgency * pulse * 0.04f);
        _fill.vertices = _fillVertices;
        SetColors(_fill, 0.12f + urgency * 0.12f);
        _fill.RecalculateBounds();

        for (int i = 0; i < 12; i++)
        {
            float travel = Mathf.Repeat(progress * 1.7f + i / 12f, 1f);
            float a = i * Mathf.PI * 2f / 12f - travel * 0.7f;
            float distance = _radius * Mathf.Lerp(0.88f, 0.08f, travel);
            Vector3 center = new Vector3(Mathf.Cos(a) * distance, Mathf.Sin(a) * distance * 0.5f, 0f);
            float size = _radius * 0.035f * (1f - travel * 0.5f);
            int first = i * 4;
            _moteVertices[first] = center + Vector3.right * size;
            _moteVertices[first + 1] = center + Vector3.up * size * 0.5f;
            _moteVertices[first + 2] = center - Vector3.right * size;
            _moteVertices[first + 3] = center - Vector3.up * size * 0.5f;
            Color mote = Color.Lerp(i % 2 == 0 ? _stealAccentColor : _runeColor,
                _chargedColor, travel * urgency);
            mote.a = Mathf.Sin(travel * Mathf.PI) * (0.6f + urgency * 0.4f);
            for (int j = 0; j < 4; j++) _moteColors[first + j] = mote;
        }
        _inwardMotes.vertices = _moteVertices;
        _inwardMotes.colors = _moteColors;
        _inwardMotes.RecalculateBounds();
    }

    private void SetColors(Mesh mesh, float alpha)
    {
        Color color = _color;
        color.a *= alpha;
        for (int i = 0; i < _colors.Length; i++) _colors[i] = color;
        mesh.colors = _colors;
    }

    private void OnDestroy()
    {
        if (_background != null) Destroy(_background);
        if (_fill != null) Destroy(_fill);
        if (_ornaments != null) Destroy(_ornaments);
        if (_inwardMotes != null) Destroy(_inwardMotes);
        if (_material != null) Destroy(_material);
    }
}
