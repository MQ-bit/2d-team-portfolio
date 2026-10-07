using System;
using UnityEngine;

public class DashCooldownPresenter : MonoBehaviour
{
    [SerializeField] private CooltimeCounterView _view;

    private PlayerMovementModule _movementModule;
    private bool _isSubscribed;

    private void Awake()
    {
        if (_view == null)
        {
            _view = GetComponentInChildren<CooltimeCounterView>();
        }
    }

    public void Initialize(PlayerMovementModule module)
    {
        if (module == null)
        {
            throw new ArgumentNullException(nameof(module));
        }

        if (_view == null)
        {
            throw new InvalidOperationException($"{name} : CooltimeCounter가 연결되지 않음!");
        }

        UnlinkEventsAll();
        _movementModule = module;
        LinkEventsAll();
        _view.StopCoolTimeEffect();
    }

    private void LinkEventsAll()
    {
        if (_movementModule == null || _isSubscribed)
        {
            return;
        }

        _movementModule.OnDashStarted += HandleDashStarted;
        _isSubscribed = true;
    }

    private void UnlinkEventsAll()
    {
        if (_movementModule == null || !_isSubscribed)
        {
            return;
        }

        _movementModule.OnDashStarted -= HandleDashStarted;
        _isSubscribed = false;
    }

    private void HandleDashStarted(float cooldownSeconds)
    {
        _view.StartCount(cooldownSeconds);
    }

    private void OnDestroy()
    {
        UnlinkEventsAll();
        _movementModule = null;
    }
}
