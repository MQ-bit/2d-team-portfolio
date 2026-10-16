using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class PerformanceDemoPotionDriver : MonoBehaviour
{
    public PotionAreaController AreaPrefab;
    public PotionProjectile ProjectilePrefab;
    private readonly List<PotionAreaController> _areas = new();
    public int ActualCount
    {
        get
        {
            int count = 0;
            foreach (PotionAreaController area in _areas)
                if (area != null && area.gameObject.activeInHierarchy) count++;
            return count;
        }
    }
    public void CreateAreas(PerformanceDemoScenario scenario, IDamageable owner, PotionRunCatalog catalog)
    {
        _areas.Clear();
        float spacing = scenario.Potion.EffectRadius * 2.2f;
        int columns = Mathf.Min(5, scenario.Count);
        int rows = Mathf.CeilToInt(scenario.Count / (float)columns);
        for (int i = 0; i < scenario.Count; i++)
        {
            Vector3 position = new((i % columns - (columns - 1) * 0.5f) * spacing,
                (i / columns - (rows - 1) * 0.5f) * spacing * 0.5f, 0);
            PotionAreaController area = Instantiate(AreaPrefab, position, Quaternion.identity);
            _areas.Add(area);
            area.Initialize(scenario.Potion, LayerMask.GetMask("Wall"));
            area.SetOwner(owner);
            area.SetCatalog(catalog);
            area.Activate();
            if (!area.gameObject.activeSelf || area.AreaCollider == null || !area.AreaCollider.enabled)
                throw new InvalidOperationException("Area initialization/footprint failed.");
        }
    }
    public PotionThrowPrediction Throw(PerformanceDemoScenario scenario, IDamageable owner, PotionRunCatalog catalog)
    {
        _areas.Clear();
        Vector3 start = new(-6, 0, 0);
        Vector3 target = scenario.Arena == PerformanceDemoArena.OpenFloor ? Vector3.zero : new Vector3(4, 0, 0);
        PotionProjectile projectile = Instantiate(ProjectilePrefab, start, Quaternion.identity);
        projectile.Initiate(catalog.GetPotionItemByData(scenario.Potion), catalog, owner);
        PotionThrowPrediction prediction = projectile.PredictThrow(start, target);
        projectile.Launch(target, 0.8f, 2.5f, 480);
        return prediction;
    }
    public int ObserveLanding(Scene scene)
    {
        _areas.Clear();
        foreach (GameObject root in scene.GetRootGameObjects())
            _areas.AddRange(root.GetComponentsInChildren<PotionAreaController>());
        return ActualCount;
    }
    public bool HasLandingAt(Vector3 position)
    {
        foreach (PotionAreaController area in _areas)
        {
            if (area != null && area.AreaCollider != null && area.AreaCollider.enabled &&
                Vector2.Distance(area.transform.position, position) < 0.1f)
            {
                return true;
            }
        }

        return false;
    }
    public void Clear() => _areas.Clear();
}
