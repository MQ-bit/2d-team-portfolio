using UnityEngine;

public class PoolMember : MonoBehaviour 
{
    private IPoolSource<GameObject> _owner;
    private IPoolMember _callback;

    public bool IsRented { get; private set; }

    public void SetPoolSource(IPoolSource<GameObject> poolSource) 
    {
        _owner = poolSource;
        _callback = GetComponent<IPoolMember>();

        if (_callback == null) 
        {
            Debug.LogWarning($"{name}에 IPoolMember 구현체가 없습니다.", this);
        }
    }

    public void ReturnToPool() 
    {
        if (IsRented && _owner != null) 
        {
            _owner.Return(gameObject);
        }
    }


    public bool IsOwnedBy(IPoolSource<GameObject> pool) 
    {
        return ReferenceEquals(_owner, pool);
    }

    public void SetRented() 
    {
        if (IsRented) return;
    
        IsRented = true;
        _callback?.OnSpawned();
    }

    public void SetReturned() 
    {
        if (!IsRented) return;

        IsRented = false;
        _callback?.OnDespawned();
    }
}