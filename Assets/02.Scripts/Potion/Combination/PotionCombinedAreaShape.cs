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

    public void Create(PotionGroundArea first, PotionGroundArea second,
        PotionVisualData visualData)
    {
        Create(first, second, visualData, 0f);
    }

    public void Create(PotionGroundArea first, PotionGroundArea second,
        PotionVisualData visualData, float spreadDuration)
    {
        CircleCollider2D firstCircle = first.AreaCollider;
        CircleCollider2D secondCircle = second.AreaCollider;
        Vector2 firstCenter = firstCircle.bounds.center;
        Vector2 secondCenter = secondCircle.bounds.center;
        var shape = new PotionPuddleShape(firstCenter, firstCircle.bounds.extents.x,
            secondCenter, secondCircle.bounds.extents.x,
            _connectionWidthRatio, _connectionBlendRatio);
        var spread = new PotionCombinationSpread(shape, _combinationCollider, Time.time,
            spreadDuration, first.Data.VisualData.AreaColor, second.Data.VisualData.AreaColor);
        Vector2[] points = new Vector2[shape.Outline.Length];
        for (int i = 0; i < points.Length; i++)
        {
            points[i] = _combinationCollider.transform.InverseTransformPoint(shape.Outline[i])
                - (Vector3)_combinationCollider.offset;
        }

        firstCircle.enabled = false;
        secondCircle.enabled = false;
        _combinationCollider.pathCount = 1;
        _combinationCollider.SetPath(0, points);
        _combinationCollider.enabled = true;
        first.AreaEffect.PrepareOriginalForSpread(spread, true, first.Owner);
        second.AreaEffect.PrepareOriginalForSpread(spread, false, second.Owner);

        first.RetainAreaVfxForSpread(spread);
        second.RetainAreaVfxForSpread(spread);
        CreatePuddleVisual(first, second, visualData, shape);
        first.AreaVisual.SetCombinationSpread(spread);
        if (visualData.AreaVfxPrefab != null)
        {
            GameObject vfx = first.SetAreaVfx(visualData, shape.BoundsSize.x, shape.BoundsSize.y);
            vfx.transform.position = new Vector3(shape.BoundsCenter.x, shape.BoundsCenter.y,
                first.transform.position.z);
            vfx.transform.rotation = Quaternion.identity;
            vfx.GetComponent<VFXBurstField>().ConfigurePuddle(shape);
            vfx.GetComponent<VFXBurstField>().ConfigureSpread(spread, true);
        }
        first.AreaEffect.enabled = true;
    }

    private void CreatePuddleVisual(PotionGroundArea first, PotionGroundArea second,
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
        first.AreaVisual.Bind(renderer, visualData, transition: true);
        first.AreaVisual.SetPuddleShape(shape);
        firstRenderer.enabled = false;
        secondRenderer.enabled = false;
    }
}
