using UnityEngine;

// 맞은 방향을 함께 받는 피해. 피격 연출(화면 흔들림 방향 등)에 쓴다.
// hitDirection은 공격이 날아온 진행 방향이며, 모르면 Vector2.zero.
public interface IDirectionalDamageable
{
    void DecreaseHealth(float amount, Vector2 hitDirection);
}
