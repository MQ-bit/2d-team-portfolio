using UnityEngine;

public class TmpPlayerFollowCamera : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField] private Transform _target;
    [SerializeField] private Vector3 _offset = new Vector3(0f, 0f, -10f);
    [SerializeField, Min(0f)] private float _smoothTime = 0.15f;
    //Smooth Time은 카메라가 목표 위치에 도달할 때까지 움직임을 얼마나 부드럽게 완화할지 정하는 값
    private Vector3 _velocity;

    private void LateUpdate()
    {
        if (_target == null)
        {
            return;
        }

        Vector3 targetPosition = _target.position + _offset;
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref _velocity,
            _smoothTime
        );
    }
}
