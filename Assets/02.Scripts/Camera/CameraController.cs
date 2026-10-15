using System.Collections;
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

    [Header("Camera Shake Settings")]
    [SerializeField] private CinemachineImpulseSource _impulseSource; // 비워두면 아래 설정으로 런타임에 생성
    [SerializeField, Min(0.01f)] private float _shakeDuration = 0.15f;
    [SerializeField] private bool _shakeIgnoreTimeScale = true; // 히트스톱으로 시간이 멈춰도 흔들림은 재생되게
    private Coroutine _shakeBurstRoutine;

    private void Awake()
    {
        SingletonInitiate();
        InitiateCameraSettings();
        InitiateCameraBoundary();
        InitiateCameraShake();
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

    // 런타임에 AddComponent하면 Reset()이 불리지 않아 Gain, ChannelMask 등이 0이므로 직접 채워준다.
    private void InitiateCameraShake()
    {
        if (_camera != null && !_camera.TryGetComponent(out CinemachineImpulseListener _))
        {
            CinemachineImpulseListener listener = _camera.gameObject.AddComponent<CinemachineImpulseListener>();
            listener.ApplyAfter = CinemachineCore.Stage.Noise;
            listener.ChannelMask = 1;
            listener.Gain = 1f;
            listener.UseCameraSpace = true;
            listener.ReactionSettings = new CinemachineImpulseListener.ImpulseReaction
            {
                AmplitudeGain = 1f,
                FrequencyGain = 1f,
                Duration = 1f
            };
        }

        if (_impulseSource == null)
        {
            _impulseSource = gameObject.AddComponent<CinemachineImpulseSource>();
            _impulseSource.ImpulseDefinition = new CinemachineImpulseDefinition
            {
                ImpulseChannel = 1,
                ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Recoil,
                ImpulseDuration = _shakeDuration,
                ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform
            };
        }

        CinemachineImpulseManager.Instance.IgnoreTimeScale = _shakeIgnoreTimeScale;
    }

    // direction 방향으로 화면을 튕기듯 흔든다. 방향이 없으면 무작위 방향.
    public void ShakeCamera(Vector2 direction, float force)
    {
        if (_impulseSource == null || force <= 0f)
        {
            return;
        }

        if (direction.sqrMagnitude < Mathf.Epsilon)
        {
            direction = Random.insideUnitCircle;

            if (direction.sqrMagnitude < Mathf.Epsilon)
            {
                direction = Vector2.right;
            }
        }

        _impulseSource.GenerateImpulseWithVelocity(direction.normalized * force);
    }

    public void ShakeCamera(float force)
    {
        ShakeCamera(Vector2.zero, force);
    }

    // 첫 충격 뒤 반대 방향의 약한 충격을 이어 화면이 짧게 떨리며 가라앉게 한다.
    public void ShakeCameraBurst(float force, int pulseCount, float interval, float decay)
    {
        if (!isActiveAndEnabled || _impulseSource == null || force <= 0f) return;
        if (_shakeBurstRoutine != null)
        {
            StopCoroutine(_shakeBurstRoutine);
            _shakeBurstRoutine = null;
        }
        pulseCount = Mathf.Clamp(pulseCount, 1, 6);
        decay = Mathf.Clamp01(decay);
        if (pulseCount == 1 || decay <= 0f)
        {
            ShakeCamera(force);
            return;
        }
        _shakeBurstRoutine = StartCoroutine(PlayShakeBurst(force, pulseCount,
            Mathf.Max(0.01f, interval), decay));
    }

    private IEnumerator PlayShakeBurst(float force, int pulseCount, float interval, float decay)
    {
        Vector2 direction = Random.insideUnitCircle;
        if (direction.sqrMagnitude < Mathf.Epsilon) direction = Vector2.right;
        object delay = _shakeIgnoreTimeScale
            ? (object)new WaitForSecondsRealtime(interval) : new WaitForSeconds(interval);
        for (int i = 0; i < pulseCount; i++)
        {
            ShakeCamera(direction, force);
            if (i == pulseCount - 1 || force * decay <= 0f) break;
            yield return delay;
            direction = -direction;
            force *= decay;
        }
        _shakeBurstRoutine = null;
    }

    private void OnDisable()
    {
        if (_shakeBurstRoutine == null) return;
        StopCoroutine(_shakeBurstRoutine);
        _shakeBurstRoutine = null;
    }

    private void Start()
    {
        SetFollowTarget(FindAnyObjectByType<PlayerController>().transform); //TODO : 나중에 오브젝트 관리 매니저 생기면 플레이어 참조를 넘기는 걸로.
    }

    /// <summary>층 이동 후 실제 출력 카메라의 추적이 안정될 때까지 검은 화면에서 기다립니다.</summary>
    public IEnumerator WaitForTrackingSettled()
    {
        CinemachineBrain brain = CinemachineCore.FindPotentialTargetBrain(_camera);
        if (_camera == null || !_camera.isActiveAndEnabled || brain == null || brain.OutputCamera == null)
        {
            Debug.LogWarning($"{nameof(CameraController)}: 추적 카메라가 없어 대기를 생략합니다.", this);
            yield return null;
            yield break;
        }

        Transform output = brain.OutputCamera.transform;
        Vector3 previousPosition = output.position;
        Quaternion previousRotation = output.rotation;
        float stableTime = 0f;
        float elapsedTime = 0f;
        const float requiredStableTime = 0.15f;
        const float maxWaitTime = 5f;
        const float positionSpeedTolerance = 0.05f;
        const float rotationSpeedTolerance = 0.1f;

        while (elapsedTime < maxWaitTime)
        {
            // 플레이어의 물리 위치와 Cinemachine LateUpdate가 새 층을 반영한 뒤 비교합니다.
            yield return null;

            if (_camera == null || !_camera.isActiveAndEnabled || brain == null ||
                !brain.isActiveAndEnabled || output == null)
            {
                Debug.LogWarning($"{nameof(CameraController)}: 대기 중 카메라가 비활성화되었습니다.", this);
                yield break;
            }

            float deltaTime = Time.unscaledDeltaTime;
            elapsedTime += deltaTime;
            // 감쇠 추적은 정확히 0이 되지 않으므로 화면에서 구별하기 어려운 속도가 지속되는지 확인합니다.
            bool settled = (brain.IgnoreTimeScale || Time.deltaTime > 0f) && brain.IsLiveChild(_camera) &&
                !brain.IsBlending &&
                Vector3.Distance(previousPosition, output.position) <= positionSpeedTolerance * deltaTime &&
                Quaternion.Angle(previousRotation, output.rotation) <= rotationSpeedTolerance * deltaTime;
            stableTime = settled ? stableTime + Mathf.Min(deltaTime, 0.05f) : 0f;
            previousPosition = output.position;
            previousRotation = output.rotation;

            if (stableTime >= requiredStableTime)
            {
                yield break;
            }
        }

        // 일시정지나 계속되는 흔들림으로 무한 대기하지 않도록, 여전히 검은 상태에서 추적을 확정합니다.
        Debug.LogWarning($"{nameof(CameraController)}: 카메라 대기 시간이 초과되어 추적 위치를 즉시 갱신합니다.", this);
        CinemachineBrain.UpdateMethods previousUpdateMethod = brain.UpdateMethod;
        try
        {
            // SmartUpdate가 FixedUpdate를 기다리는 경우에도 일시정지 중 위치를 확정할 수 있게 합니다.
            brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
            _camera.PreviousStateIsValid = false;
            brain.ActiveBlend = null;
            yield return null;
            yield return null;
        }
        finally
        {
            if (brain != null)
            {
                brain.UpdateMethod = previousUpdateMethod;
            }
        }
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
