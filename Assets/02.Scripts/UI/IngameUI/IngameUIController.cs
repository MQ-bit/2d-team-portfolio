using UnityEngine;

public class IngameUIController : MonoBehaviour
{
    [Header("Presenter References")]
    [SerializeField] private PlayerStatusPresenter _statPresneter;
    [SerializeField] private PotionCodexPresenter _potionCodexPresenter;
    [SerializeField] private DashCooldownPresenter _dashCooldownPresenter;

    [SerializeField] private Sprite _testSprite; //TODO : PlayerData가 담긴 ScriptableObject를 구현하면 이거 제거


    private void Awake()
    {
        if (_statPresneter == null)
        {
            _statPresneter = GetComponentInChildren<PlayerStatusPresenter>();
        }
        if (_potionCodexPresenter == null)
        {
            _potionCodexPresenter = GetComponentInChildren<PotionCodexPresenter>();
        }
        if (_dashCooldownPresenter == null)
        {
            _dashCooldownPresenter = GetComponentInChildren<DashCooldownPresenter>();
        }
    }

    private void Start()
    {
        Initalize();
    }

    private void Initalize()
    {
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            _statPresneter.Initialize(player.PlayerStatus, _testSprite);
            _potionCodexPresenter.Initialize(player.InventoryModule);
            _dashCooldownPresenter.Initialize(player.MovementModule);
        }
        else
        {
            Debug.Log($"{gameObject.name} : Can't find target PlayerController!");
        }
    }
}
