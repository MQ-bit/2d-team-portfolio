using System.Net.NetworkInformation;
using UnityEngine;

public class PlayerDrinkingModule : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private PotionEffectReceiver _effectReceiver;
    [SerializeField] private PlayerInventoryModule _inventoryModule;
    
    [Header("Drink Settings")]
    [SerializeField] private float _drinkDuration = 1f; //TODO: Scriptable Object로 초기화하는 로직 반영
    [SerializeField] private float _drinkTimer = 0f; //TODO: Progress를 확인하고자 SerializeField로 노출함. 디버그 끝나고 롤백시키기

    public bool IsDrinking {get; private set;}

    public float DrinkProgress
    {
        get
        {
            if (!IsDrinking)
            {
                return 0f;
            }

            return Mathf.Clamp01(_drinkTimer / _drinkDuration);
        }
    }

    private void Awake()
    {
        if(_inventoryModule == null)
        {
            _inventoryModule = GetComponent<PlayerInventoryModule>();
        }
        if(_effectReceiver == null)
        {
            _effectReceiver = GetComponent<PotionEffectReceiver>();
        }
    }

    private void Update()
    {
        if(!IsDrinking) return;

        _drinkTimer += Time.deltaTime;
        if(_drinkTimer >= _drinkDuration)
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
        if(IsDrinking || _inventoryModule == null)
        {
            return false;
        }
        PotionData selectedPotion = _inventoryModule.SelectedPotion;
        
        if(selectedPotion == null) return false;

        if(PotionEffectResolver.FindTarget(transform) == null)
        {
            return false;
        }

        _drinkTimer = 0f;
        IsDrinking = true;

        return true;
    }

    public void CancelDrink()
    {
        if(!IsDrinking) return;

        ResetState();
    }

    private void CompleteDrink()
    {
        if(PotionEffectResolver.FindTarget(transform) == null)
        {
            ResetState();
            return;
        }

        if(!_inventoryModule.TryConsumeSelectedPotion(out PotionData consumedPotion))
        {
            ResetState();
            return;
        }

        PotionEffectResolver.ApplyDrinkEffect(consumedPotion, _effectReceiver); //TODO: 굳이 이 클래스를 Static으로 둘 필요가 있는지 고려. 뭔가 Receiver가 Potion효과적용 + Status Thirsty 증가 를 해줬으면 좋을지도
        ResetState();
    }

    private void ResetState()
    {
        IsDrinking = false;
        _drinkTimer = 0f;
    }
}