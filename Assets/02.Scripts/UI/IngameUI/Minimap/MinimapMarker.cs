using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 미니맵에 표시할 월드 오브젝트에 부착한다.
/// </summary>
public class MinimapMarker : MonoBehaviour
{
    [Header("Visual")]
    [Tooltip("비어 있으면 Color를 사용하는 기본 네모로 표시한다.")]
    [SerializeField] private Sprite _icon;

    [SerializeField] private Color _color = Color.white;

    [SerializeField, Min(1f)] private float _markerSize = 16f;

    [Tooltip("값이 높을수록 다른 마커보다 위에 표시된다.")]
    [SerializeField] private int _renderPriority;

    [Header("Runtime")]
    [SerializeField] private bool _visible = true;

    public int Id => gameObject.GetInstanceID();
    public Sprite Icon => _icon;
    public Color Color => _color;
    public float Size => _markerSize;
    public int RenderPriority => _renderPriority;
    public Vector3 WorldPosition => transform.position;

    public bool IsVisible => _visible && isActiveAndEnabled && gameObject.activeInHierarchy;

    private void OnEnable()
    {
        if (_visible)
        {
            MinimapMarkerRegistry.Register(this);
        }
    }

    private void OnDisable()
    {
        MinimapMarkerRegistry.Unregister(this);
    }

    private void OnDestroy()
    {
        MinimapMarkerRegistry.Unregister(this);
    }

    /// <summary>
    /// GameObject를 비활성화하지 않고 마커만 표시하거나 숨김.
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (_visible == visible) return;

        _visible = visible;

        if (!isActiveAndEnabled) return;
        if (_visible)
        {
            MinimapMarkerRegistry.Register(this);
        }
        else
        {
            MinimapMarkerRegistry.Unregister(this);
        }
    }
}