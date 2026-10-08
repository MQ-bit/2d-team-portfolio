using System;
using UnityEngine;

[CreateAssetMenu(fileName = "PotionStatusVisuals", menuName = "Potion/Status Visual Data")]
public sealed class PotionStatusVisualData : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        [SerializeField] private PotionStatusEffectTracker _status;
        [SerializeField] private PotionAreaSurfaceStyle _surfaceStyle;
        [SerializeField] private Color _color = Color.white;
        [SerializeField] private Color _accentColor = Color.white;
        [SerializeField, Range(0f, 1f)] private float _opacity = 0.32f;
        [SerializeField, Min(1f)] private float _sizeMultiplier = 1.7f;
        [InspectorName("Core Spread")]
        [Tooltip("대상 중심에서 퍼지는 기운의 폭입니다.")]
        [SerializeField, Range(0.1f, 0.85f)] private float _innerRadius = 0.55f;
        [InspectorName("Diffusion Softness")]
        [SerializeField, Range(0.01f, 0.2f)] private float _edgeSoftness = 0.14f;
        [SerializeField, Min(0f)] private float _flowSpeed = 0.7f;
        [SerializeField] private Vector3 _offset;
        [Tooltip("잔류 상태가 적용되기 전에도 해당 상태의 장판 안에서는 표시합니다.")]
        [SerializeField] private bool _showInArea;

        public PotionStatusEffectTracker Status => _status;
        public PotionAreaSurfaceStyle SurfaceStyle => _surfaceStyle;
        public Color Color => _color;
        public Color AccentColor => _accentColor;
        public float Opacity => _opacity;
        public float SizeMultiplier => _sizeMultiplier;
        public float InnerRadius => _innerRadius;
        public float EdgeSoftness => _edgeSoftness;
        public float FlowSpeed => _flowSpeed;
        public Vector3 Offset => _offset;
        public bool ShowInArea => _showInArea;
    }

    [SerializeField] private Entry[] _effects = Array.Empty<Entry>();

    public Entry[] Effects => _effects;

    [Header("Combination Aura")]
    [SerializeField, Range(0f, 1f)] private float _combinationOpacity = 0.65f;
    [SerializeField, Min(1f)] private float _combinationSizeMultiplier = 1.75f;
    [SerializeField, Range(0.1f, 0.85f)] private float _combinationCoreSpread = 0.6f;
    [SerializeField, Min(0f)] private float _combinationFlowSpeed = 1.1f;

    public float CombinationOpacity => _combinationOpacity;
    public float CombinationSizeMultiplier => _combinationSizeMultiplier;
    public float CombinationCoreSpread => _combinationCoreSpread;
    public float CombinationFlowSpeed => _combinationFlowSpeed;
}
