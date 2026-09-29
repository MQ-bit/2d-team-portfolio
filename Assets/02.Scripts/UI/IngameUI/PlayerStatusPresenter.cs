using System;
using UnityEngine;

/// <summary>
/// MVP 패턴을 적용함. PlayerStatus.cs가 model, PlayerStatusView가 View가 됨.
/// </summary>
public class PlayerStatusPresenter : MonoBehaviour
{
    [SerializeField] private PlayerStatusView _view;

    private PlayerStatus _targetStatus;
    private bool _isSubscribed;

    private void Awake()
    {
        if (_view == null)
        {
            _view = GetComponentInChildren<PlayerStatusView>();
        }
    }

    public void Initialize(PlayerStatus targetStatus, Sprite portrait)
    {
        if (targetStatus == null)
        {
            throw new ArgumentNullException(nameof(targetStatus));
        }

        if (_view == null)
        {
            throw new InvalidOperationException($"{nameof(_targetStatus)}가 연결되지 않음!");
        }

        UnlinkEventsAll();

        _targetStatus = targetStatus;
        _view.Initialize(portrait);

        LinkEventsAll();
        RefreshAllElements();
    }

    private void LinkEventsAll()
    {
        if (_targetStatus == null || _isSubscribed) return;

        _targetStatus.OnHealthChanged += HandleHealthChanged;
        _targetStatus.OnThirstChanged += HandleThirstChanged;

        _isSubscribed = true;
    }

    private void UnlinkEventsAll()
    {
        if (_targetStatus == null || _isSubscribed) return;

        _targetStatus.OnHealthChanged -= HandleHealthChanged;
        _targetStatus.OnThirstChanged -= HandleThirstChanged;

        _isSubscribed = false;
    }

    private void RefreshAllElements()
    {
        if (_targetStatus == null) return;

        _view.SetHealth(_targetStatus.CurrentHealth, _targetStatus.MaxHealth);
        _view.SetThirst(_targetStatus.CurrentThirst, _targetStatus.MaxThirst);
    }

    private void HandleHealthChanged(float current, float maximum)
    {
        _view.SetHealth(current, maximum);
    }

    private void HandleThirstChanged(float current, float maximum)
    {
        _view.SetThirst(current, maximum);
    }

    private void OnDestroy()
    {
        UnlinkEventsAll();
        _targetStatus = null;
    }

}
