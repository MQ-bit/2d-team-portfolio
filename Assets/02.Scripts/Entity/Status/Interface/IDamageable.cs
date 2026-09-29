using System;


public interface IDamageable
{
    bool IsDead { get; }

    //이 두 이벤트 필요없으면 제거
    event Action<float, float> OnHealthChanged; //current, max
    event Action OnDeath;

    public void RestoreHealth(float amount);
    public void DecreaseHealth(float amount);

}
