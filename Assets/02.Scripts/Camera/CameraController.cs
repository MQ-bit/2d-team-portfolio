using Unity.Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    private static CameraController _instance;
    public static CameraController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<CameraController>();
            }

            return _instance;
        }
    }

    [Header("Cinemachine Camera Setting")]
    [SerializeField] private CinemachineCamera _camera;
    [SerializeField] private CinemachineConfiner2D _confiner;

    [Header("Boundary Settings")]
    [SerializeField] private Collider2D _cameraBoundary;

    //나중에 카메라 이펙트 처리 코드 들고올수도?

    private void Awake()
    {
        SingletonInitiate();
        InitiateCameraSettings();
        InitiateCameraBoundary();
    }

    private void SingletonInitiate()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    private void InitiateCameraSettings()
    {
        if (_camera == null)
        {
            _camera = GetComponent<CinemachineCamera>();
        }
        if (_confiner == null)
        {
            _confiner = GetComponent<CinemachineConfiner2D>();
        }
    }

    private void InitiateCameraBoundary()
    {
        if (_cameraBoundary == null)
        {
            GameObject obj = GameObject.FindGameObjectWithTag("CameraBoundary");

            if (!obj.TryGetComponent(out _cameraBoundary))
            {
                Debug.LogWarning($"{gameObject.name} : Fail to Find Camera Boundary! Need to make Boundary Object.");
            }
        }
    }

    private void Start()
    {
        SetFollowTarget(FindAnyObjectByType<PlayerController>().transform); //TODO : 나중에 오브젝트 관리 매니저 생기면 플레이어 참조를 넘기는 걸로.
    }

    public void SetFollowTarget(Transform target)
    {
        if (_camera == null || target == null)
        {
            Debug.LogWarning($"{gameObject.name} : Can't Setup New Follow Target");
            return;
        }
        _camera.Target.TrackingTarget = target;
    }
}
