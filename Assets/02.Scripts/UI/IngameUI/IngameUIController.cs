using UnityEngine;
using UnityEngine.SceneManagement;

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

    [SerializeField] private AudioClip _potionInformationOpenSound;
    [SerializeField, Range(0f, 1f)] private float _potionInformationOpenVolume = 1f;

    private bool _isPotionInformationOpen;

    [Header("Fallback Icon")]
    [SerializeField] private Sprite _testSprite; //TODO : PlayerData가 담긴 ScriptableObject를 구현하면 이거 제거

    [Header("Pause")]
    [SerializeField] private PausePresenter _pausePresenter;
    [SerializeField] private float _pauseMenuTimeScale = 0f;
    [SerializeField] private string _titleSceneName;

    private bool _isPauseOpen;
    private GameTimeController _gameTimeController;
    private readonly object _potionInformationTimeOwner = new();
    private readonly object _pauseTimeOwner = new();

    private void Awake()
    {
        GetComponents();
        _gameTimeController = GameTimeController.Instance;
    }

    private void OnEnable()
    {
        if (_inputReaderSO != null)
        {
            _inputReaderSO.OnPotionInformationTabToggled += TogglePotionInformation;
        }

        if (_pausePresenter != null)
        {
            _pausePresenter.OnPauseStateChanged += HandlePauseStateChanged;
            _pausePresenter.OnQuitConfirmed += HandleQuitConfirmed;
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

        if (_pausePresenter != null)
        {
            _pausePresenter.ResetPause();
            _pausePresenter.OnPauseStateChanged -= HandlePauseStateChanged;
            _pausePresenter.OnQuitConfirmed -= HandleQuitConfirmed;
        }

        if (_gameTimeController != null)
        {
            _gameTimeController.ReleaseTimeScale(_pauseTimeOwner);
        }

        _isPauseOpen = false;
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

        if (_pausePresenter == null)
        {
            _pausePresenter = GetComponentInChildren<PausePresenter>(true);
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
            if (_pausePresenter != null)
            {
                _pausePresenter.Initialize(_inputReaderSO, _player);
            }
        }
        else
        {
            Debug.Log($"{gameObject.name} : Can't find target PlayerController!");
        }
    }
    private void TogglePotionInformation()
    {
        // Tab은 플레이어 입력 처리 경로를 거치지 않으므로 별도로 차단한다.
        if (_isPauseOpen)
        {
            return;
        }

        if (_isPotionInformationOpen)
        {
            SoundManager.PlayUi(_potionInformationOpenSound, _potionInformationOpenVolume);
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

        _isPotionInformationOpen = true;

        SoundManager.PlayUi(_potionInformationOpenSound, _potionInformationOpenVolume);
        _player?.AcquireGamePlayInputLock(this);
        _potionInformationHUD.SetActive(true);
        _gameTimeController.RequestTimeScale(_potionInformationTimeOwner, _pauseTimeScale);
    }

    private void ClosePotionInformation()
    {
        if (!_isPotionInformationOpen)
        {
            return;
        }

        _isPotionInformationOpen = false;
        _player?.ReleaseGamePlayInputLock(this);
        if (_potionInformationHUD != null)
        {
            _potionInformationHUD.SetActive(false);
        }

        if (_gameTimeController != null)
        {
            _gameTimeController.ReleaseTimeScale(_potionInformationTimeOwner);
        }
    }

    private void HandlePauseStateChanged(bool isOpen)
    {
        _isPauseOpen = isOpen;

        if (isOpen)
        {
            _gameTimeController.RequestTimeScale(_pauseTimeOwner, _pauseMenuTimeScale);
        }
        else if (_gameTimeController != null)
        {
            _gameTimeController.ReleaseTimeScale(_pauseTimeOwner);
        }
    }

    private void HandleQuitConfirmed()
    {
        // 이동할 수 없는 경우 확인창과 일시정지를 유지한다.
        if (string.IsNullOrWhiteSpace(_titleSceneName) || !Application.CanStreamedLevelBeLoaded(_titleSceneName))
        {
            Debug.LogError($"{name}: 이동할 타이틀 씬을 지정하고 Build Profiles의 Scene List에 등록해주세요. ({_titleSceneName})", this);
            return;
        }

        ClosePotionInformation();
        _pausePresenter.ResetPause();
        SceneManager.LoadScene(_titleSceneName);
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
