using System;
using System.Collections;
using UnityEngine;

public class PlayerMovementModule : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private PlayerStatus _playerStatus;
    [SerializeField] private Rigidbody2D _rigid;

    [Header("Dash Settings")]
    [SerializeField] private float _dashDuration = 0.2f;
    [SerializeField] private float _dashCooltime = 1f;

    //Dash 관련 변수
    private Coroutine _dashCoroutine;
    private bool _isDashing;
    private float _nextDashAvailableTime;
    private readonly WaitForFixedUpdate _waitForFixedUpdate = new WaitForFixedUpdate();

    public bool IsDashing => _isDashing;
    public float DashDuration => _dashDuration; // 구르기 애니메이션 길이를 대시 시간에 맞추는 용도

    public bool CanDash => !_isDashing && Time.time >= _nextDashAvailableTime;

    public event Action<float> OnDashStarted;

    private void OnDisable()
    {
        CancelDash();
    }

    public void MovePosition(Vector2 moveInput)
    {
        if (_isDashing || !_playerStatus.CanMove)
        {
            return;
        }

        Vector2 movement = moveInput * _playerStatus.Speed * Time.fixedDeltaTime;
        _rigid.MovePosition(_rigid.position + movement);
    }

    public bool TryStartDash(Vector2 moveInput)
    {
        if (!CanDash || !_playerStatus.CanMove || moveInput == Vector2.zero)
        {
            return false;
        }

        _isDashing = true;
        _nextDashAvailableTime = Time.time + _dashCooltime;

        _dashCoroutine = StartCoroutine(DashCoroutine(moveInput.normalized));
        OnDashStarted?.Invoke(_dashCooltime);
        return true;
    }

    private IEnumerator DashCoroutine(Vector2 direction)
    {
        float dashRemainingTime = _dashDuration;
        while (dashRemainingTime > 0f)
        {
            yield return _waitForFixedUpdate;

            float stepTime = Mathf.Min(Time.fixedDeltaTime, dashRemainingTime);
            Vector2 movement = direction * _playerStatus.DashSpeed * stepTime;

            _rigid.MovePosition(_rigid.position + movement);
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
}
