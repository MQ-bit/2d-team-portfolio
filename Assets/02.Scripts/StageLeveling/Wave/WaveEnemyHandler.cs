using System;
using UnityEngine;

public class WaveEnemyHandle : MonoBehaviour
{
    private bool _released;
    public event Action<WaveEnemyHandle> Released;

    private void OnEnable()
    {
        _released = false;
    }

    private void OnDisable()
    {
        NotifyReleased();
    }

    public void NotifyReleased()
    {
        if (_released)
        {
            return;
        }

        _released = true;
        Released?.Invoke(this);
    }
}