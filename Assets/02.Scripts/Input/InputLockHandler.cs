using System;
using System.Collections.Generic;

public sealed class InputLockHandler
{
    private readonly HashSet<object> _gamePlayInputLockOwners = new HashSet<object>();

    public bool CanReadInput => _gamePlayInputLockOwners.Count == 0;
    public int LockCount => _gamePlayInputLockOwners.Count;

    public bool TryAcquireGamePlayInputLock(object owner)
    {
        if (owner == null)
        {
            throw new ArgumentNullException(nameof(owner));
        }

        return _gamePlayInputLockOwners.Add(owner);
    }

    public bool TryReleaseGamePlayInputLock(object owner)
    {
        if (owner == null)
        {
            throw new ArgumentNullException(nameof(owner));
        }

        return _gamePlayInputLockOwners.Remove(owner);
    }
}