using System;
using UnityEngine;

public class MeleeAttackPresenter : MonoBehaviour
{
    [SerializeField] private MeleeAttackView _view;

    private PlayerMeleeModule _meleeModule;
    private bool _isSubscribed;

    private void Awake()
    {
        if (_view == null)
        {
            _view = GetComponentInChildren<MeleeAttackView>();
        }
    }

    public void Initialize(PlayerMeleeModule module)
    {
        if (module == null)
        {
            throw new ArgumentNullException(nameof(module));
        }

        if (_view == null)
        {
            throw new InvalidOperationException($"{name}: Melee Module is not assigned!");
        }

        UnlinkEventsAll();
        _meleeModule = module;

        LinkEventsAll();
        _view.SetMeleeAttackEnabled(_meleeModule.IsMeleeModeActivated);
    }

    private void LinkEventsAll()
    {
        if (_meleeModule == null || _isSubscribed) return;
        _meleeModule.OnMeleeModeChanged += HandleMeleeModeChanged;
        _isSubscribed = true;
    }

    private void UnlinkEventsAll()
    {
        if (_meleeModule == null || !_isSubscribed) return;
        _meleeModule.OnMeleeModeChanged -= HandleMeleeModeChanged;
        _isSubscribed = false;
    }

    private void HandleMeleeModeChanged(bool isEnabled)
    {
        _view.SetMeleeAttackEnabled(isEnabled);
    }

    private void OnDestroy()
    {
        UnlinkEventsAll();
        _meleeModule = null;
    }
}
