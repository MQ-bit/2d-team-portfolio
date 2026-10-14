using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public sealed class PotionCombinationRecordArrow : MaskableGraphic
{
    [SerializeField] private RectTransform _sourceFrame;
    [SerializeField] private RectTransform _resultFrame;
    [SerializeField, Min(1f)] private float _lineThickness = 2f;
    [SerializeField, Min(2f)] private float _arrowHeadSize = 10f;
    [SerializeField, Min(0f)] private float _edgeGap = 6f;

    private readonly Vector3[] _sourceCorners = new Vector3[4];
    private readonly Vector3[] _resultCorners = new Vector3[4];
    private Vector2 _previousStart;
    private Vector2 _previousEnd;
    private bool _hasPoints;

    private void LateUpdate()
    {
        bool hasPoints = TryGetPoints(out Vector2 start, out Vector2 end);
        if (hasPoints != _hasPoints || start != _previousStart || end != _previousEnd)
        {
            _hasPoints = hasPoints;
            _previousStart = start;
            _previousEnd = end;
            SetVerticesDirty();
        }
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if (!TryGetPoints(out Vector2 start, out Vector2 end)) return;

        Vector2 elbow = new Vector2(start.x, end.y);
        Vector2 direction = end.x >= start.x ? Vector2.right : Vector2.left;
        float headSize = Mathf.Min(_arrowHeadSize, Mathf.Abs(end.x - start.x));
        Vector2 headBase = end - direction * headSize;
        AddSegment(mesh, start, elbow);
        AddSegment(mesh, elbow, headBase);

        int index = mesh.currentVertCount;
        mesh.AddVert(end, color, Vector2.zero);
        mesh.AddVert(headBase + Vector2.up * headSize * 0.6f, color, Vector2.zero);
        mesh.AddVert(headBase - Vector2.up * headSize * 0.6f, color, Vector2.zero);
        mesh.AddTriangle(index, index + 1, index + 2);
    }

    private bool TryGetPoints(out Vector2 start, out Vector2 end)
    {
        start = end = Vector2.zero;
        if (_sourceFrame == null || _resultFrame == null) return false;

        _sourceFrame.GetWorldCorners(_sourceCorners);
        _resultFrame.GetWorldCorners(_resultCorners);
        start = rectTransform.InverseTransformPoint((_sourceCorners[1] + _sourceCorners[2]) * 0.5f);
        Vector2 resultCenter = rectTransform.InverseTransformPoint((_resultCorners[0] + _resultCorners[2]) * 0.5f);
        bool fromLeft = start.x < resultCenter.x;
        Vector3 edge = fromLeft
            ? (_resultCorners[0] + _resultCorners[1]) * 0.5f
            : (_resultCorners[2] + _resultCorners[3]) * 0.5f;
        end = rectTransform.InverseTransformPoint(edge);
        start += Vector2.up * _edgeGap;
        end += (fromLeft ? Vector2.left : Vector2.right) * _edgeGap;
        return true;
    }

    private void AddSegment(VertexHelper mesh, Vector2 start, Vector2 end)
    {
        Vector2 offset = Vector2.Perpendicular(end - start).normalized * (_lineThickness * 0.5f);
        int index = mesh.currentVertCount;
        mesh.AddVert(start - offset, color, Vector2.zero);
        mesh.AddVert(start + offset, color, Vector2.zero);
        mesh.AddVert(end + offset, color, Vector2.zero);
        mesh.AddVert(end - offset, color, Vector2.zero);
        mesh.AddTriangle(index, index + 1, index + 2);
        mesh.AddTriangle(index, index + 2, index + 3);
    }
}
