using UnityEngine;

public class IngameUIController : MonoBehaviour
{
    [Header("Presenter References")]
    [SerializeField] private PlayerStatusPresenter _statPresneter;
    [SerializeField] private Sprite _testSprite; //TODO : PlayerData가 담긴 ScriptableObject를 구현하면 이거 제거


    private void Awake()
    {
        if (_statPresneter == null)
        {
            _statPresneter = GetComponentInChildren<PlayerStatusPresenter>();
        }
    }

    private void Start()
    {
        Initalize();
    }

    private void Initalize()
    {
        PlayerStatus targetStatus = FindAnyObjectByType<PlayerStatus>();
        if (targetStatus != null)
        {
            _statPresneter.Initialize(targetStatus, _testSprite);
        }
        else
        {
            Debug.Log($"{gameObject.name} : Can't find target PlayerStatus!");
        }
    }
}
