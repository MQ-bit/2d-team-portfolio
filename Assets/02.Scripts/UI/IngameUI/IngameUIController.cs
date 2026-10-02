using UnityEngine;

public class IngameUIController : MonoBehaviour
{
    [Header("Input Reader")]
    [SerializeField] private InputReader _inputReaderSO;

    [Header("Another GameObject")]
    [SerializeField] private PlayerController _player;

    [Header("Presenter References")]
    [SerializeField] private PlayerStatusPresenter _statPresneter;
    [SerializeField] private PotionCodexPresenter _potionCodexPresenter;
    [SerializeField] private DashCooldownPresenter _dashCooldownPresenter;
    [SerializeField] private MeleeAttackPresenter _meleeAttackPresenter;

    [Header("Potion Information")]
    [SerializeField] private GameObject _potionInformationHUD;
    [SerializeField] private float _pauseTimeScale = 0.2f;

    private bool _isPotionInformationOpen;
    private float _previousTimeScale = 1f;

    [Header("Fallback Icon")]
    [SerializeField] private Sprite _testSprite; //TODO : PlayerData가 담긴 ScriptableObject를 구현하면 이거 제거


    private void Awake()
    {
        GetComponents();
    }

    private void OnEnable()
    {
        if (_inputReaderSO != null)
        {
            _inputReaderSO.OnPotionInformationTabToggled += TogglePotionInformation;
        }
    }

    private void OnDisable()
    {
        if (_inputReaderSO != null)
        {
            _inputReaderSO.OnPotionInformationTabToggled -= TogglePotionInformation;
        }

        if (_isPotionInformationOpen)
        {
            ClosePotionInformation();
        }
    }


    private void GetComponents()
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
        if (_meleeAttackPresenter == null)
        {
            _meleeAttackPresenter = GetComponentInChildren<MeleeAttackPresenter>();
        }
    }

    private void Start()
    {
        Initalize();
    }

    private void Initalize()
    {
        _player = FindAnyObjectByType<PlayerController>();
        if (_player != null)
        {
            _statPresneter.Initialize(_player.PlayerStatus, _testSprite);
            _potionCodexPresenter.Initialize(_player.InventoryModule);
            _dashCooldownPresenter.Initialize(_player.MovementModule);
            _meleeAttackPresenter.Initialize(_player.MeleeModule);
        }
        else
        {
            Debug.Log($"{gameObject.name} : Can't find target PlayerController!");
        }
    }
    private void TogglePotionInformation()
    {
        if (_isPotionInformationOpen)
        {
            ClosePotionInformation();
        }
        else
        {
            OpenPotionInformation();
        }
    }

    private void OpenPotionInformation()
    {
        if (_potionInformationHUD == null)
        {
            return;
        }

        _previousTimeScale = Time.timeScale;
        _isPotionInformationOpen = true;

        _player?.SetGamePlayInputEnabled(false);
        _potionInformationHUD.SetActive(true);
        Time.timeScale = _pauseTimeScale;
    }

    private void ClosePotionInformation()
    {
        if (!_isPotionInformationOpen)
        {
            return;
        }

        _isPotionInformationOpen = false;
        _player?.SetGamePlayInputEnabled(true);
        if (_potionInformationHUD != null)
        {
            _potionInformationHUD.SetActive(false);
        }

        Time.timeScale = _previousTimeScale;
    }
}
