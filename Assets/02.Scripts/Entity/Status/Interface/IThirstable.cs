using System;

public interface IThirstable
{
    public void RestoreThirst(float amount);
    public void DecreaseThirst(float amount);

    public event Action<float, float> OnThirstChanged; //current, max / 이 event가 Monster에게도 필요 없으면 제거
}
