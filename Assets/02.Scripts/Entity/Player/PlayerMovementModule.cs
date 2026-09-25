using System.Collections;
using UnityEngine;

public class PlayerMovementModule : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private PlayerStatus _playerStatus;
    [SerializeField] private Rigidbody2D _rigid;

    //Dash 관련 변수
    private Coroutine _dashCoroutine;
    private bool _isDashing;
    private float _nextDashAvailableTime;
    WaitForFixedUpdate _waitForFixedUpdate = new WaitForFixedUpdate();

    public bool IsDashing => _isDashing;

    public bool CanDash => !_isDashing && Time.time >= _nextDashAvailableTime;

    private void OnDisable()
    {
        CancelDash();
    }

    public void MovePosition(Vector2 moveInput)
    {
        if (_isDashing)
        {
            return;
        }

        Vector2 movement = moveInput * _playerStatus.Speed * Time.fixedDeltaTime;
        _rigid.MovePosition(_rigid.position + movement);
    }

    public bool TryStartDash(Vector2 moveInput)
    {
        if (!CanDash || moveInput == Vector2.zero)
        {
            return false;
        }

        _isDashing = true;
        _nextDashAvailableTime = Time.time + _playerStatus.DashCooltime;

        _dashCoroutine = StartCoroutine(DashCoroutine(moveInput.normalized));
        return true;
    }

    private IEnumerator DashCoroutine(Vector2 direction)
    {
        float dashRemainingTime = _playerStatus.DashDuration;
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