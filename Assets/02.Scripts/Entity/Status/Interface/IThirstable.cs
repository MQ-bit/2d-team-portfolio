using System;

public interface IThirstable
{
    public float CurrentThirst { get; }
    public float MaxThirst { get; }
    public void RestoreThirst(float amount);
    public void DecreaseThirst(float amount);

    public event Action<float, float, float> OnThirstChanged; //previous, current, max
}
