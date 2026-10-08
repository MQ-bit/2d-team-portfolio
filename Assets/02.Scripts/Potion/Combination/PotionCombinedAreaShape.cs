using UnityEngine;

public sealed class PotionCombinedAreaShape : MonoBehaviour
{
    [SerializeField] private PolygonCollider2D _combinationCollider;

    [Tooltip("두 웅덩이를 잇는 부분의 두께. 낮을수록 가운데가 잘록해진다.")]
    [SerializeField, Range(0.2f, 0.9f)] private float _connectionWidthRatio = 0.55f;
    [Tooltip("두 웅덩이와 연결부가 부드럽게 섞이는 정도")]
    [SerializeField, Range(0.01f, 0.5f)] private float _connectionBlendRatio = 0.25f;

    public bool IsReady => _combinationCollider != null;
    public PolygonCollider2D CombinationCollider => _combinationCollider;

    private void Awake()
    {
        if (_combinationCollider == null)
        {
            _combinationCollider = GetComponent<PolygonCollider2D>();
        }

        if (_combinationCollider != null)
        {
            _combinationCollider.isTrigger = true;
            _combinationCollider.enabled = false;
        }
    }

    public void Create(PotionAreaController first, PotionAreaController second,
        PotionVisualData visualData)
    {
        Create(first, second, visualData, 0f);
    }

    public void Create(PotionAreaController first, PotionAreaController second,
        PotionVisualData visualData, float spreadDuration)
    {
        Collider2D firstCollider = first.AreaCollider;
        Collider2D secondCollider = second.AreaCollider;
        Vector2 firstCenter = first.OriginalCenter;
        Vector2 secondCenter = second.OriginalCenter;
        // Thermal shock uses a stable spatial ordering, independent of throw order.
        bool reverseShape = visualData.AreaSurfaceStyle == PotionAreaSurfaceStyle.ThermalShock
            && (firstCenter.x > secondCenter.x || (firstCenter.x == secondCenter.x && firstCenter.y > secondCenter.y));
        var shape = new PotionPuddleShape(reverseShape ? secondCenter : firstCenter,
            reverseShape ? second.OriginalRadius : first.OriginalRadius,
            reverseShape ? firstCenter : secondCenter,
            reverseShape ? first.OriginalRadius : second.OriginalRadius,
            _connectionWidthRatio, _connectionBlendRatio);
        _combinationCollider.enabled = false;
        // Preserve original centers/radii; a clipped collider's bounds are not a lobe.
        Rect bounds = PotionAreaFootprint.BoundsOf(shape.Outline);
        var walls = PotionWallQuery.FindPolygons(bounds, first.WallLayers);
        if (second.WallLayers != first.WallLayers)
            walls.AddRange(PotionWallQuery.FindPolygons(bounds, second.WallLayers));
        PotionAreaFootprint footprint = walls.Count == 0
            ? PotionAreaFootprint.Unclipped(shape.Outline)
            : PotionAreaClipper.Subtract(shape.Outline, firstCenter, walls);
        if (footprint == null || !footprint.Contains(secondCenter)) return;
        var spread = new PotionCombinationSpread(shape, _combinationCollider, Time.time,
            spreadDuration,
            reverseShape ? second.Data.VisualData.AreaColor : first.Data.VisualData.AreaColor,
            reverseShape ? first.Data.VisualData.AreaColor : second.Data.VisualData.AreaColor);
        firstCollider.enabled = false;
        secondCollider.enabled = false;
        footprint.ApplyTo(_combinationCollider);
        first.AreaEffect.PrepareOriginalForSpread(spread, !reverseShape, first.Owner);
        second.AreaEffect.PrepareOriginalForSpread(spread, reverseShape, second.Owner);

        first.RetainAreaVfxForSpread(spread);
        second.RetainAreaVfxForSpread(spread);
        first.SetCombinedFootprint(footprint);
        second.SetCombinedFootprint(footprint);
        CreatePuddleVisual(first, second, visualData, shape);
        first.AreaVisual.SetFootprint(footprint);
        first.AreaVisual.SetCombinationSpread(spread);
        if (visualData.AreaVfxPrefab != null)
        {
            GameObject vfx = first.SetAreaVfx(visualData, shape.BoundsSize.x, shape.BoundsSize.y);
            vfx.transform.position = new Vector3(shape.BoundsCenter.x, shape.BoundsCenter.y,
                first.transform.position.z);
            vfx.transform.rotation = Quaternion.identity;
            vfx.GetComponent<VFXBurstField>().ApplyCombinationPalette(visualData.AreaColor);
            vfx.GetComponent<VFXBurstField>().ConfigurePuddle(shape);
            vfx.GetComponent<VFXBurstField>().ConfigureSpread(spread, true);
        }
        first.AreaEffect.enabled = true;
    }

    private void CreatePuddleVisual(PotionAreaController first, PotionAreaController second,
        PotionVisualData visualData, PotionPuddleShape shape)
    {
        SpriteRenderer firstRenderer = first.AreaRenderer;
        SpriteRenderer secondRenderer = second.AreaRenderer;
        if (firstRenderer == null || secondRenderer == null || firstRenderer.sprite == null) return;

        GameObject visualObject = new("CombinationPuddleVisual");
        Transform visualTransform = visualObject.transform;
        visualTransform.SetParent(first.transform, true);
        visualTransform.position = new Vector3(shape.BoundsCenter.x, shape.BoundsCenter.y,
            first.transform.position.z);
        visualTransform.rotation = Quaternion.identity;
        Sprite sprite = visualData.AreaSprite != null ? visualData.AreaSprite : firstRenderer.sprite;
        Vector2 spriteSize = sprite.bounds.size;
        Vector3 parentScale = first.transform.lossyScale;
        visualTransform.localScale = new Vector3(
            shape.BoundsSize.x / Mathf.Max(0.001f, spriteSize.x * Mathf.Abs(parentScale.x)),
            shape.BoundsSize.y / Mathf.Max(0.001f, spriteSize.y * Mathf.Abs(parentScale.y)), 1f);

        SpriteRenderer renderer = visualObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = visualData.AreaColor;
        renderer.sharedMaterial = first.AreaVisual.OriginalMaterial;
        renderer.sortingLayerID = firstRenderer.sortingLayerID;
        renderer.sortingOrder = firstRenderer.sortingOrder;
        if (visualData.AreaSurfaceStyle == PotionAreaSurfaceStyle.ThermalShock)
        {
            uint seed = unchecked((uint)Mathf.RoundToInt(shape.BoundsCenter.x * 1000f) * 747796405u
                ^ (uint)Mathf.RoundToInt(shape.BoundsCenter.y * 1000f) * 2891336453u
                ^ (uint)Mathf.RoundToInt(shape.BoundsSize.x * 1000f) * 2246822519u
                ^ (uint)Mathf.RoundToInt(shape.BoundsSize.y * 1000f) * 3266489917u);
            first.AreaVisual.SetCombinationSeed(seed);
        }
        first.AreaVisual.Bind(renderer, visualData, transition: true);
        first.AreaVisual.SetPuddleShape(shape);
        firstRenderer.enabled = false;
        secondRenderer.enabled = false;
    }
}
