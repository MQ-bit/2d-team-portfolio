using Unity.Cinemachine;
using UnityEngine;

// 이 오브젝트가 생성되는 순간 화면을 흔든다. 폭발 같은 이펙트 프리팹에 붙여서 쓴다.
// 흔들림 모양/세기는 CinemachineImpulseSource에서 설정하고,
// 실제로 흔들리려면 CinemachineCamera에 CinemachineImpulseListener가 있어야 한다.
[RequireComponent(typeof(CinemachineImpulseSource))]
public class CameraShakeOnSpawn : MonoBehaviour
{
    [SerializeField] private CinemachineImpulseSource _impulseSource;
    [SerializeField, Min(0f)] private float _force = 1f;

    private void Awake()
    {
        if (_impulseSource == null)
        {
            _impulseSource = GetComponent<CinemachineImpulseSource>();
        }
    }

    private void Start()
    {
        _impulseSource.GenerateImpulseWithForce(_force);
    }
}
