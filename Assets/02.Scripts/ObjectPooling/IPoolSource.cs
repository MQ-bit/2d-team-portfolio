using UnityEngine;

public interface IPoolSource<T>
{
    int InactiveCount { get; }
    void Prewarm(int count);
    T Rent(Vector3 position, Quaternion rotation);
    void Return(T member);
}