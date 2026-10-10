using System;
using UnityEngine;

public class MinimapTerrainData : IDisposable
{
    public Texture2D Texture { get; }
    public Vector2Int MinCell { get; }

    private bool _disposed;

    public MinimapTerrainData(Texture2D texture, Vector2Int minCell)
    {
        Texture = texture;
        MinCell = minCell;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (Texture == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            UnityEngine.Object.Destroy(Texture);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(Texture);
        }
    }
}