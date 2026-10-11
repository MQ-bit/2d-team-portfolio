using UnityEngine;

// 근접/원거리 적이 공통으로 쓰는 수치. 체력은 EnemyStatus(EntityHealth)가 관리한다.
public abstract class EnemyData : ScriptableObject
{
    [Header("Health")]
    [SerializeField, Min(1f)] private float _maxHealth = 100f;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float _moveSpeed = 2.5f;

    [Header("Detection")]
    [SerializeField, Min(0f)] private float _detectRange = 6f;

    [Header("Visual")]
    [Tooltip("스프라이트(렌더러) 크기 배율. 충돌 범위는 그대로고 보이는 크기만 바뀐다.")]
    [SerializeField, Min(0.1f)] private float _visualScale = 1f;
    [Tooltip("스프라이트 색. 흰색이면 원본 색 그대로.")]
    [SerializeField] private Color _tintColor = Color.white;

    public float MaxHealth => _maxHealth;
    public float MoveSpeed => _moveSpeed;
    public float DetectRange => _detectRange;

    // 엘리트처럼 데이터마다 다른 외형을 렌더러에 입힌다. 컨트롤러의 Awake에서 한 번 호출한다.
    public void ApplyVisual(SpriteRenderer spriteRenderer)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.color = _tintColor;
        spriteRenderer.transform.localScale *= _visualScale;
    }
}
