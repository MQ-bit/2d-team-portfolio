using UnityEngine;

// 근접/원거리 적이 공통으로 쓰는 수치. 체력은 Health 컴포넌트에서 관리한다.
public abstract class EnemyData : ScriptableObject
{
    [Header("Movement")]
    [SerializeField, Min(0f)] private float _moveSpeed = 2.5f;

    [Header("Detection")]
    [SerializeField, Min(0f)] private float _detectRange = 6f;

    public float MoveSpeed => _moveSpeed;
    public float DetectRange => _detectRange;
}
