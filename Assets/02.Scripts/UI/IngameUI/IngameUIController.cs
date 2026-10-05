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
    [SerializeField] private ThirstFeedbackPresenter _thirstFeedbackPresenter;

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
        if (_thirstFeedbackPresenter == null)
        {
            _thirstFeedbackPresenter = GetComponentInChildren<ThirstFeedbackPresenter>();
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
            bool hasPlayerStatus = _player.TryGetComponent(out PlayerStatus status);
            bool hasPlayerInventory = _player.TryGetComponent(out PlayerInventoryModule inventory);
            bool hasPlayerMovement = _player.TryGetComponent(out PlayerMovementModule movement);
            bool hasPlayerMelee = _player.TryGetComponent(out PlayerMeleeModule melee);
            bool hasPlayerDialogueModule = _player.TryGetComponent(out PlayerDialogueModule dialogueModule);

            if (!hasPlayerStatus)
            {
                Debug.LogError($"{gameObject.name} : Can't find PlayerStatus in target PlayerController!");
            }
            if (!hasPlayerInventory)
            {
                Debug.LogError($"{gameObject.name} : Can't find PlayerInventoryModule in target PlayerController!");
            }
            if (!hasPlayerMovement)
            {
                Debug.LogError($"{gameObject.name} : Can't find PlayerMovementModule in target PlayerController!");
            }
            if (!hasPlayerMelee)
            {
                Debug.LogError($"{gameObject.name} : Can't find PlayerMeleeModule in target PlayerController!");
            }
            if (!hasPlayerDialogueModule)
            {
                Debug.LogError($"{gameObject.name} : Can't find PlayerDialogueModule in target PlayerController!");
            }

            _statPresneter.Initialize(status, _testSprite);
            _potionCodexPresenter.Initialize(inventory);
            _dashCooldownPresenter.Initialize(movement);
            _meleeAttackPresenter.Initialize(melee);
            InitializeThirstFeedbackPresenter(status, dialogueModule);
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

    private void InitializeThirstFeedbackPresenter(PlayerStatus status, PlayerDialogueModule dialogueModule)
    {
        if (_thirstFeedbackPresenter == null) // 이 기능을 사용하지 않는 다른 씬은 그대로 통과.
        {
            return;
        }

        if (status == null)
        {
            Debug.LogError($"{nameof(IngameUIController)}: {nameof(PlayerStatus)}을 찾지 못했습니다.");
            return;
        }

        if (dialogueModule == null)
        {
            Debug.LogError($"{nameof(IngameUIController)}: {nameof(PlayerDialogueModule)}을 찾지 못했습니다.");
            return;
        }

        dialogueModule.Initialize(status);
        _thirstFeedbackPresenter.Initialize(status, dialogueModule);
    }
}
