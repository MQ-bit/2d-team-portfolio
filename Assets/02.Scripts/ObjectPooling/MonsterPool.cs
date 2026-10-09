using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class MonsterPool : MonoBehaviour, IPoolSource<GameObject> 
{
    private readonly Stack<GameObject> _inactive = new Stack<GameObject>();

    private GameObject _prefab;
    private Transform _activeRoot;
    private Transform _inactiveRoot;
    private bool _initialized;

    public int InactiveCount => _inactive.Count;

    public void Initialize(GameObject prefab, int initialSize, Transform activeRoot) 
    {
        if (_initialized) 
        {

            throw new InvalidOperationException($"{name} 풀이 이미 초기화되었습니다.");
        }
        if (prefab == null)
        {
            throw new ArgumentNullException(nameof(prefab));
        } 
        

        _initialized = true;
        _prefab = prefab;
        _activeRoot = activeRoot != null ? activeRoot : transform;

        GameObject storage = new GameObject("Inactive");

        _inactiveRoot = storage.transform;
        _inactiveRoot.SetParent(transform, false);

        storage.SetActive(false);

        Prewarm(Mathf.Max(0, initialSize));
    }

    public void Prewarm(int count) 
    {
        EnsureInitialized();
        
        while (_inactive.Count < count)
        {
            _inactive.Push(CreateInstance());
        }     
    }

    public GameObject Rent(Vector3 position, Quaternion rotation) 
    {
        EnsureInitialized();

        GameObject instance = null;
        while (_inactive.Count > 0 && instance == null)
        {
            instance = _inactive.Pop();
        }
        
        if (instance == null) 
        {
            instance = CreateInstance();
        }

        instance.transform.SetParent(_activeRoot, false);
        instance.transform.SetPositionAndRotation(position, rotation);

        PoolMember member = instance.GetComponent<PoolMember>();
        member.SetRented();
        instance.SetActive(true);
        return instance;
    }

    public void Return(GameObject instance) 
    {
        if (instance == null) return;

        PoolMember member = instance.GetComponent<PoolMember>();
        if (member == null || !member.IsRented || !member.IsOwnedBy(this)) 
        {
            Debug.LogWarning($"{instance.name} : {name}으로 돌려보낼 수 없습니다.");
            return;
        }

        member.SetReturned();
        instance.SetActive(false);
        instance.transform.SetParent(_inactiveRoot, false);

        _inactive.Push(instance);
    }

    private GameObject CreateInstance() 
    {
        GameObject instance = Instantiate(_prefab, _inactiveRoot);
        instance.name = _prefab.name;

        PoolMember member = instance.GetComponent<PoolMember>();
        
        if (member == null)
        {
            member = instance.AddComponent<PoolMember>();
        }

        member.SetPoolSource(this);
        instance.SetActive(false);

        return instance;
    }

    private void EnsureInitialized() 
    {
        if (!_initialized)
        {
            throw new InvalidOperationException($"{name} 풀이 초기화되지 않았습니다. Initialize()를 먼저 호출해야 합니다.");  
        } 
    }
}