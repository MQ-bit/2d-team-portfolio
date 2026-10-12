using System;
using UnityEngine;

public class PlayerDrinkingModule : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private PotionRunCatalog _potionRunCatalog;
    [SerializeField] private PotionEffectReceiver _effectReceiver;
    [SerializeField] private PlayerInventoryModule _inventoryModule;
    [SerializeField] private PlayerStatus _status;

    private PlayerData.DrinkSettings Settings => _status.Data.Drink;
    private float _drinkTimer;

    public bool IsDrinking { get; private set; }
    public event Action<bool> OnDrinkStateChanged; //UI 연결용 이벤트
    public float DrinkProgress
    {
        get
        {
            if (!IsDrinking)
            {
                return 0f;
            }

            return Mathf.Clamp01(_drinkTimer / Settings.Duration);
        }
    }

    private void Awake()
    {
        if (_inventoryModule == null)
        {
            _inventoryModule = GetComponent<PlayerInventoryModule>();
        }
        if (_effectReceiver == null)
        {
            _effectReceiver = GetComponent<PotionEffectReceiver>();
        }
        if (_status == null)
        {
            _status = GetComponent<PlayerStatus>();
        }
        if (_status == null || _status.Data == null) { enabled = false; return; }

        if (_potionRunCatalog == null)
        {
            _potionRunCatalog = FindAnyObjectByType<PotionRunCatalog>();
        }
    }

    private void Update()
    {
        if (!IsDrinking) return;

        _drinkTimer += Time.deltaTime;
        if (_drinkTimer >= Settings.Duration)
        {
            CompleteDrink();
        }
    }

    private void OnDisable()
    {
        CancelDrink();
    }

    public bool TryBeginDrink()
    {
        if (IsDrinking || _inventoryModule == null)
        {
            return false;
        }
        PotionData selectedPotion = _inventoryModule.SelectedPotion;

        if (selectedPotion == null) return false;

        if (PotionEffectApplier.FindTarget(transform) == null)
        {
            return false;
        }

        _drinkTimer = 0f;
        SetDrinking(true);

        return true;
    }

    public void CancelDrink()
    {
        if (!IsDrinking) return;

        ResetState();
    }

    private void CompleteDrink()
    {
        if (PotionEffectApplier.FindTarget(transform) == null)
        {
            ResetState();
            return;
        }

        if (!_inventoryModule.TryConsumeSelectedItem(out PotionItem consumedPotion))
        {
            ResetState();
            return;
        }

        _potionRunCatalog.TryIdentify(consumedPotion);

        PotionEffectApplier.ApplyDrinkEffect(consumedPotion.PotionData, _effectReceiver);
        _status.DecreaseThirst(Settings.BaseWithdrawalDecrease);

        ScoreManager.Instance?.TryRecordPotionConsumed();
        ResetState();
    }

    private void ResetState()
    {
        _drinkTimer = 0f;
        SetDrinking(false);
    }

    private void SetDrinking(bool isDrinking)
    {
        if (IsDrinking != isDrinking)
        {
            IsDrinking = isDrinking;
            OnDrinkStateChanged?.Invoke(isDrinking);
        }
    }
}
