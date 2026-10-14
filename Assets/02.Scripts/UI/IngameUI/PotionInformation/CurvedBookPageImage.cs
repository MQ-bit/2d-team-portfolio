using UnityEngine;
using UnityEngine.UI;

// A cylindrical paper surface projected into Canvas coordinates. Two graphics
// share the surface, drawing only their visible side with separate UI captures.
public sealed class CurvedBookPageImage : RawImage
{
    private const int Columns = 48;
    private const int Rows = 8;
    private float _progress, _curl = 1.4f, _perspective = 0.14f;
    private bool _forward = true, _backFace;

    public void Configure(float progress, float curl, float perspective, bool forward, bool backFace)
    {
        _progress = Mathf.Clamp01(progress);
        _curl = curl;
        _perspective = perspective;
        _forward = forward;
        _backFace = backFace;
        SetVerticesDirty();
    }

    public void CopySurfaceFrom(CurvedBookPageImage source)
    {
        Configure(source._progress, source._curl, source._perspective, source._forward, source._backFace);
    }

    private float Bend => _curl * Mathf.Sin(_progress * Mathf.PI);
    private float Angle(float s) => _progress * Mathf.PI + Bend * (0.5f - s);

    public Vector2 Project(float s, float vertical)
    {
        float bend = Bend;
        float start = Angle(0f);
        float angle = Angle(s);
        float x, lift;
        if (Mathf.Abs(bend) < 0.0001f)
        {
            x = s * Mathf.Cos(start);
            lift = s * Mathf.Sin(start);
        }
        else
        {
            x = (Mathf.Sin(start) - Mathf.Sin(angle)) / bend;
            lift = (Mathf.Cos(angle) - Mathf.Cos(start)) / bend;
        }
        float projection = 1f / (1f - lift * _perspective);
        Rect rect = rectTransform.rect;
        return new Vector2((_forward ? 1f : -1f) * x * rect.width * projection,
            (vertical * rect.height + lift * rect.width * 0.045f) * projection);
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        float bend = Bend;
        float fold = Mathf.Abs(bend) > 0.0001f ? (Angle(0f) - Mathf.PI * 0.5f) / bend : -1f;
        for (int column = 0; column < Columns; column++)
        {
            float from = column / (float)Columns;
            float to = (column + 1) / (float)Columns;
            // Split precisely at the fold so both faces meet without a gap.
            if (fold > from && fold < to)
            {
                AddStrip(mesh, from, fold);
                AddStrip(mesh, fold, to);
            }
            else
            {
                AddStrip(mesh, from, to);
            }
        }
    }

    private void AddStrip(VertexHelper mesh, float from, float to)
    {
        bool facesBack = Mathf.Cos(Angle((from + to) * 0.5f)) < 0f;
        if (facesBack != _backFace) return;
        for (int row = 0; row < Rows; row++)
        {
            float bottom = row / (float)Rows;
            float top = (row + 1) / (float)Rows;
            int first = mesh.currentVertCount;
            AddVertex(mesh, from, bottom);
            AddVertex(mesh, from, top);
            AddVertex(mesh, to, top);
            AddVertex(mesh, to, bottom);
            mesh.AddTriangle(first, first + 1, first + 2);
            mesh.AddTriangle(first + 2, first + 3, first);
        }
    }

    private void AddVertex(VertexHelper mesh, float s, float v)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.position = Project(s, v - 0.5f);
        float shade = Mathf.Lerp(0.64f, 1f, Mathf.Abs(Mathf.Cos(Angle(s))));
        vertex.color = new Color(color.r * shade, color.g * shade, color.b * shade, color.a);
        float u = _forward != _backFace ? s : 1f - s;
        vertex.uv0 = new Vector2(uvRect.x + u * uvRect.width, uvRect.y + v * uvRect.height);
        mesh.AddVert(vertex);
    }
}
