using UnityEngine;

public class PlayerStatus : MonoBehaviour
{
    [SerializeField] private float _speed = 0f;
    
    public float Speed => _speed;
}
