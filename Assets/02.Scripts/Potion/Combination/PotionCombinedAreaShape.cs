using UnityEngine;

public sealed class PotionCombinedAreaShape : MonoBehaviour
{
    private const int EllipseSegments = 32;

    [SerializeField] private PolygonCollider2D _combinationCollider;
    [SerializeField, Min(0f)] private float _ellipseMinCenterDistance = 0.1f;

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
        CircleCollider2D firstCircle = first.AreaCollider;
        CircleCollider2D secondCircle = second.AreaCollider;
        Vector2 firstCenter = firstCircle.bounds.center;
        Vector2 secondCenter = secondCircle.bounds.center;
        Vector2 offset = secondCenter - firstCenter;
        float centerDistance = offset.magnitude;
        Vector2 direction = centerDistance >= _ellipseMinCenterDistance
            ? offset / centerDistance
            : Vector2.right;

        float firstRadius = Mathf.Max(firstCircle.bounds.extents.x, firstCircle.bounds.extents.y);
        float secondRadius = Mathf.Max(secondCircle.bounds.extents.x, secondCircle.bounds.extents.y);
        float radius = Mathf.Max(firstRadius, secondRadius);
        float semiMajor;
        float semiMinor;
        if (centerDistance < _ellipseMinCenterDistance)
        {
            semiMajor = radius + centerDistance * 0.5f;
            semiMinor = semiMajor;
        }
        else
        {
            semiMajor = centerDistance * 0.5f + radius;
            semiMinor = Mathf.Sqrt(radius * (centerDistance + radius));
        }

        Vector2 center = (firstCenter + secondCenter) * 0.5f;
        float angle = Mathf.Atan2(direction.y, direction.x);
        float cosAngle = Mathf.Cos(angle);
        float sinAngle = Mathf.Sin(angle);
        Vector2[] points = new Vector2[EllipseSegments];
        for (int i = 0; i < EllipseSegments; i++)
        {
            float radians = Mathf.PI * 2f * i / EllipseSegments;
            float x = Mathf.Cos(radians) * semiMajor;
            float y = Mathf.Sin(radians) * semiMinor;
            Vector2 worldPoint = center + new Vector2(
                x * cosAngle - y * sinAngle,
                x * sinAngle + y * cosAngle);
            points[i] = first.transform.InverseTransformPoint(worldPoint);
        }

        first.AreaEffect.DisableForCombination();
        second.AreaEffect.DisableForCombination();
        firstCircle.enabled = false;
        secondCircle.enabled = false;
        _combinationCollider.points = points;
        _combinationCollider.enabled = true;

        CreateEllipseVisual(first, second, visualData, centerDistance, center, direction);
        first.AreaEffect.enabled = true;
    }

    private void CreateEllipseVisual(PotionGroundArea first, PotionGroundArea second,
        PotionVisualData visualData, float centerDistance, Vector2 center, Vector2 direction)
    {
        SpriteRenderer firstRenderer = first.GetComponent<SpriteRenderer>();
        SpriteRenderer secondRenderer = second.GetComponent<SpriteRenderer>();
        if (firstRenderer == null || secondRenderer == null || firstRenderer.sprite == null) return;

        float visualDiameter = Mathf.Max(
            firstRenderer.bounds.size.x, firstRenderer.bounds.size.y,
            secondRenderer.bounds.size.x, secondRenderer.bounds.size.y);
        float visualWidth = centerDistance < _ellipseMinCenterDistance
            ? visualDiameter
            : centerDistance + visualDiameter;

        firstRenderer.enabled = false;
        secondRenderer.enabled = false;
        GameObject visualObject = new("CombinationEllipseVisual");
        Transform visualTransform = visualObject.transform;
        visualTransform.SetParent(first.transform, true);
        visualTransform.position = new Vector3(center.x, center.y, first.transform.position.z);
        visualTransform.rotation = Quaternion.Euler(0f, 0f,
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

        Vector2 spriteSize = firstRenderer.sprite.bounds.size;
        Vector3 parentScale = first.transform.lossyScale;
        visualTransform.localScale = new Vector3(
            visualWidth / Mathf.Max(0.001f, spriteSize.x * Mathf.Abs(parentScale.x)),
            visualDiameter / Mathf.Max(0.001f, spriteSize.y * Mathf.Abs(parentScale.y)),
            1f);

        SpriteRenderer ellipseRenderer = visualObject.AddComponent<SpriteRenderer>();
        ellipseRenderer.sprite = firstRenderer.sprite;
        ellipseRenderer.color = visualData.AreaColor;
        ellipseRenderer.sharedMaterial = firstRenderer.sharedMaterial;
        ellipseRenderer.sortingLayerID = firstRenderer.sortingLayerID;
        ellipseRenderer.sortingOrder = firstRenderer.sortingOrder;
    }
}
