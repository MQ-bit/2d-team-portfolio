using Interactable;
using UnityEngine;
using System.Collections.Generic;
public class InteractablePotionBox : MonoBehaviour, IInteractable
{
    [Header("Visual Settings")]
    [SerializeField] private Animator _animator;

    [Header("Potion Reward")]
    [SerializeField] private List<PotionFieldItem> _dropPrefabs = new List<PotionFieldItem>();
    [SerializeField] private Transform _dropPoint;
    [SerializeField, Min(1)] private int _minDropCount = 1;
    [SerializeField, Min(1)] private int _maxDropCount = 10;
    [SerializeField, Min(0f)] private float _dropRadius = 1f;
    [Header("Drop Animation")]
    [SerializeField, Min(0.01f)] private float _dropFlightDuration = 0.45f;
    [SerializeField, Min(0f)] private float _dropArcHeight = 0.5f;

    public Transform DropPoint => _dropPoint;
    private bool _isOPened;
    private static readonly int OpenTrigger = Animator.StringToHash("Open");

    private void Awake()
    {
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }

        if (_dropPrefabs == null || _dropPrefabs.Count == 0)
        {
            Debug.LogError($"{name}: 포션 드롭 목록이 비어 있습니다.", this);
            enabled = false;
            return;
        }

        foreach (PotionFieldItem prefab in _dropPrefabs)
        {
            if (prefab == null)
            {
                Debug.LogError($"{name}: 드롭 목록에 누락된 프리팹이 있습니다.", this);
                enabled = false;
                return;
            }
        }
    }

    public bool CanInteract(GameObject interactor)
    {
        if (_isOPened || interactor == null || _dropPrefabs == null || _dropPrefabs.Count == 0)
        {
            return false;
        }

        foreach (PotionFieldItem prefab in _dropPrefabs)
        {
            if (prefab == null) return false;
        }
        return true;
    }

    public void Interact(GameObject interactor)
    {
        if (!CanInteract(interactor)) return;

        //중복 개봉 방지
        _isOPened = true;

        if (_animator != null)
        {
            _animator.SetTrigger(OpenTrigger);
        }

        Vector3 origin = _dropPoint != null ? _dropPoint.position : transform.position;

        int dropCount = Random.Range(_minDropCount, _maxDropCount + 1);
        float startAngle = Random.Range(0f, 360f);
        int sectorCount = (dropCount + 1) / 2;
        float angleStep = 360f / sectorCount;
        float angleSpread = Mathf.Min(angleStep * 0.2f, 30f);
        Physics2D.SyncTransforms();

        for (int i = 0; i < dropCount; i++)
        {

            PotionFieldItem prefab = _dropPrefabs[Random.Range(0, _dropPrefabs.Count)];
            int sectorIndex = i / 2;
            float angleOffset = Random.Range(-angleSpread, angleSpread);
            float angle = (startAngle + angleStep * sectorIndex + angleOffset) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            float radius = _dropRadius * Random.Range(0.65f, 1f);
            float duration = _dropFlightDuration * Random.Range(0.8f, 1.2f);
            float arcHeight = _dropArcHeight * Random.Range(0.6f, 1.4f);
            if (!prefab.TryGetSpawnPosition(origin, out Vector3 spawnPosition))
            {
                Debug.LogWarning($"{name}: 포션을 생성할 안전한 위치를 찾지 못했습니다. Drop Point를 벽 밖으로 이동해주세요.", this);
                continue;
            }
            Vector3 position = spawnPosition + new Vector3(direction.x * radius, direction.y * radius, 0f);
            PotionFieldItem item = Instantiate(prefab, spawnPosition, Quaternion.identity);
            item.PlayDropFlight(position, duration, arcHeight);

        }

    }

    private void OnValidate()
    {
        _minDropCount = Mathf.Max(1, _minDropCount);
        _maxDropCount = Mathf.Max(_minDropCount, _maxDropCount);
        _dropFlightDuration = Mathf.Max(0.01f, _dropFlightDuration);
        _dropArcHeight = Mathf.Max(0f, _dropArcHeight);
    }
}
