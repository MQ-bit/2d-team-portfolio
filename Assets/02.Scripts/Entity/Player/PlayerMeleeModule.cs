using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMeleeModule : MonoBehaviour
{
    [Header("Componenet References")]
    [SerializeField] private Camera _worldCamera; //어짜피 싱글톤인데 CameraController 참조하는건 어떤지 체크
    [SerializeField] private Transform _attackOrigin;
    [SerializeField] private PlayerStatus _status;

    [Header("Melee Settings")]
    [SerializeField] private float _attackDamage = 10f;
    [SerializeField] private float _attackRange = 2f;
    [SerializeField] private float _attackAngle = 90f;
    [SerializeField] private float _attackCooltime = 0.5f;
    [SerializeField] private float _swingDuration = 0.3f;
    [SerializeField] private float _knockbackForce = 5f;
    [SerializeField] private LayerMask _targetLayer = ~0;

    private readonly HashSet<IDamageable> _damagedTarget = new HashSet<IDamageable>();
    private float _currentAttackTime;
    private float _swingEndTime;
    private Vector2 _lastAttackDirection = Vector2.right;

    public bool IsMeleeModeActivated { get; private set; }

    public bool IsSwinging => Time.time < _swingEndTime;

    public event Action<bool> OnMeleeModeChanged;   //UI용 이벤트

    private void Awake()
    {
        InitiateComponents();
        _currentAttackTime = 0f;
    }

    private void InitiateComponents()
    {
        if (_status == null)
        {
            _status = GetComponent<PlayerStatus>();
        }
        if (_worldCamera == null)
        {
            _worldCamera = Camera.main;
        }
        if (_attackOrigin == null)
        {
            _attackOrigin = transform;
        }
    }

    private void OnDisable()
    {
        CancelSwing();
        SetMeleeMode(false);
    }

    public bool CanAttack()
    {
        return IsMeleeModeActivated && !IsSwinging
            && Time.time - _currentAttackTime >= _attackCooltime
            && _status != null && _status.CanMove;
    }

    public bool TryToggleMeleeMode()
    {
        if (IsSwinging)
        {
            return false;
        }

        SetMeleeMode(!IsMeleeModeActivated);
        return true;
    }

    public bool TryAttack(Vector2 mousePosition)
    {
        if (!CanAttack())
        {
            return false;
        }

        Vector2 attackDirection = GetAttackDirection(mousePosition);
        _lastAttackDirection = attackDirection;
        _currentAttackTime = Time.time;
        _swingEndTime = Time.time + _swingDuration;

        ApplyAttack(attackDirection);
        return true;
    }

    public void CancelSwing()
    {
        _swingEndTime = 0f;
    }

    private void SetMeleeMode(bool isActive)
    {
        if (IsMeleeModeActivated == isActive) return;

        IsMeleeModeActivated = isActive;
        OnMeleeModeChanged?.Invoke(isActive);
    }

    private Vector2 GetAttackDirection(Vector2 mousePosition)
    {
        if (_worldCamera == null)
        {
            Debug.LogWarning($"{gameObject.name} : _world camera dosen't Exist!");
            return Vector2.right;
        }

        Vector3 origin = _attackOrigin.position;
        Vector3 worldPos = _worldCamera.ScreenToWorldPoint(mousePosition);

        Vector2 direction = (Vector2)(worldPos - origin);

        return direction.normalized; //Mathf.Epsilon을 이용한
    }

    private void ApplyAttack(Vector2 attackDirection)
    {
        Vector2 origin = _attackOrigin.position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, _attackRange, _targetLayer);

        _damagedTarget.Clear();

        foreach (Collider2D hit in hits)
        {
            if (hit.transform == transform) continue;

            if (!IsInsideAttackAngle(origin, attackDirection, hit))
            {
                continue;
            }

            IDamageable damageable = hit.GetComponent<IDamageable>();

            if (damageable == null || damageable.IsDead || !_damagedTarget.Add(damageable))
            {
                continue;
            }

            damageable.DecreaseHealth(_attackDamage);

            if (!hit.TryGetComponent(out IKnockbackable knockbackable))
            {
                continue;
            }

            Vector2 knockbackDirection = (Vector2)hit.bounds.center - origin; //Epsilon을 통한 디렉션 극솟값 보정..?
            if (knockbackDirection.sqrMagnitude < Mathf.Epsilon)
            {
                knockbackDirection = attackDirection;
            }

            knockbackDirection.Normalize();
            knockbackable.ApplyKnockback(knockbackDirection, _knockbackForce);
        }
    }

    private bool IsInsideAttackAngle(Vector2 origin, Vector2 attackDirection, Collider2D target)
    {
        Vector2 closestPoint = target.ClosestPoint(origin);
        Vector2 directionToTarget = closestPoint - origin;

        if (directionToTarget.sqrMagnitude <= Mathf.Epsilon)
        {
            directionToTarget = (Vector2)target.bounds.center - origin;
        }

        if (directionToTarget.sqrMagnitude <= Mathf.Epsilon)
        {
            return true;
        }

        float angle = Vector2.Angle(attackDirection, directionToTarget);

        return angle <= _attackAngle * 0.5f;
    }

    private void OnDrawGizmos()
    {
        Transform originTransform = _attackOrigin != null ? _attackOrigin : transform;

        Vector3 origin = originTransform.position;
        Vector2 centerDirection = _lastAttackDirection.sqrMagnitude > Mathf.Epsilon ? _lastAttackDirection.normalized : Vector2.right;

        const int segmentCount = 24;
        float halfAngle = _attackAngle * 0.5f;

        Gizmos.color = Color.red;

        Vector3 leftDirection = Quaternion.Euler(0f, 0f, -halfAngle) * centerDirection;
        Vector3 previousPoint = origin + leftDirection * _attackRange;

        Gizmos.DrawLine(origin, previousPoint);

        for (int i = 1; i <= segmentCount; i++)
        {
            float ratio = i / (float)segmentCount;
            float angle = Mathf.Lerp(-halfAngle, halfAngle, ratio);

            Vector3 direction = Quaternion.Euler(0f, 0f, angle) * centerDirection;
            Vector3 currentPoint = origin + direction * _attackRange;

            Gizmos.DrawLine(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }
        Gizmos.DrawLine(origin, previousPoint);
    }
}
