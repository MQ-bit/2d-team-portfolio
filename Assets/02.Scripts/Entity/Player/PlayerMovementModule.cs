using System;
using System.Collections;
using UnityEngine;

public class PlayerMovementModule : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private PlayerStatus _playerStatus;
    [SerializeField] private Rigidbody2D _rigid;

    private PlayerData.DashSettings Settings => _playerStatus.Data.Dash;

    //Dash 관련 변수
    private Coroutine _dashCoroutine;
    private bool _isDashing;
    private float _nextDashAvailableTime;
    private readonly WaitForFixedUpdate _waitForFixedUpdate = new WaitForFixedUpdate();
    private EntityBodyCollision _bodyCollision;

    public bool IsDashing => _isDashing;
    public float DashDuration => Settings.Duration; // 구르기 애니메이션 길이를 대시 시간에 맞추는 용도
    public Vector2 LastDashDirection { get; private set; } = Vector2.down; // 대시 이펙트 방향용

    public bool CanDash => !_isDashing && Time.time >= _nextDashAvailableTime;

    public event Action<float> OnDashStarted;


    private void Awake()
    {
        if (_playerStatus == null)
        {
            _playerStatus = GetComponent<PlayerStatus>();
        }

        if (_rigid == null)
        {
            _rigid = GetComponent<Rigidbody2D>();
        }
        _bodyCollision = new EntityBodyCollision(_rigid, true);
        if (_playerStatus == null || _playerStatus.Data == null) enabled = false;
    }

    private void OnEnable()
    {
        _bodyCollision.Register();
        if (_playerStatus != null)
        {
            _playerStatus.OnDeath += HandleDeath;
        }
    }

    private void OnDisable()
    {
        _bodyCollision.Unregister();
        if (_playerStatus != null)
        {
            _playerStatus.OnDeath -= HandleDeath;
        }
        CancelDash();
    }

    private void FixedUpdate() => _bodyCollision.UpdateContactDamage();

    public void MovePosition(Vector2 moveInput)
    {
        if (_isDashing || !_playerStatus.CanMove)
        {
            return;
        }

        Vector2 movement = moveInput * _playerStatus.Speed * Time.fixedDeltaTime;
        _bodyCollision.MoveTo(_rigid.position + movement);
    }

    public bool TryStartDash(Vector2 moveInput)
    {
        if (!CanDash || !_playerStatus.CanMove || moveInput == Vector2.zero)
        {
            return false;
        }

        _isDashing = true;
        _nextDashAvailableTime = Time.time + Settings.Cooldown;

        LastDashDirection = moveInput.normalized;

        _dashCoroutine = StartCoroutine(DashCoroutine(LastDashDirection));
        OnDashStarted?.Invoke(Settings.Cooldown);
        return true;
    }

    private IEnumerator DashCoroutine(Vector2 direction)
    {
        float dashRemainingTime = Settings.Duration;
        while (dashRemainingTime > 0f)
        {
            yield return _waitForFixedUpdate;

            float stepTime = Mathf.Min(Time.fixedDeltaTime, dashRemainingTime);
            Vector2 movement = direction * _playerStatus.DashSpeed * stepTime;

            _bodyCollision.MoveTo(_rigid.position + movement);
            dashRemainingTime -= stepTime;
        }

        FinishDash();
    }

    public void CancelDash()
    {
        if (!_isDashing)
        {
            return;
        }

        if (_dashCoroutine != null)
        {
            StopCoroutine(_dashCoroutine);
        }

        FinishDash();
    }

    private void FinishDash()
    {
        _dashCoroutine = null;
        _isDashing = false;
    }

    private void HandleDeath()
    {
        CancelDash();
        if (_rigid != null)
        {
            _rigid.linearVelocity = Vector2.zero;
        }
    }
}
