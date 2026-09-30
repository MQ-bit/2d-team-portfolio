using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class PotionValidationBatch
{
    private const string Key = "PotionValidationBatch.Active";
    private static IEnumerator _suite;
    private static double _resumeAt;
    private static readonly List<string> Results = new();
    private static readonly List<GameObject> Objects = new();
    private static int _failures;
    private static double _deadline;

    static PotionValidationBatch() { EditorApplication.playModeStateChanged += OnPlayMode; }

    // Run in a dedicated batch editor; no project scene or prefab is saved.
    public static void Run()
    {
        SessionState.SetBool(Key, true);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }

    private static void OnPlayMode(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
        SessionState.SetBool(Key, false);
        Time.timeScale = 10;
        _deadline = EditorApplication.timeSinceStartup + 180;
        _suite = Suite();
        EditorApplication.update += Pump;
    }

    private static void Pump()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > _deadline) throw new Exception("Suite timeout");
            if (Time.timeAsDouble < _resumeAt) return;
            if (_suite.MoveNext()) { _resumeAt = Time.timeAsDouble + Convert.ToDouble(_suite.Current); return; }
            Finish();
        }
        catch (Exception e) { Record(false, "Harness exception: " + e); Finish(); }
    }

    private static void Finish()
    {
        EditorApplication.update -= Pump;
        Results.Add("Failures: " + _failures);
        File.WriteAllLines("Assets/02.Scripts/Potion/Tests~/unity-integration-results.txt", Results);
        EditorApplication.Exit(_failures == 0 ? 0 : 1);
    }

    private static void Record(bool passed, string message)
    {
        string line = (passed ? "PASS " : "FAIL ") + message;
        Results.Add(line);
        Debug.Log(line);
        if (!passed) _failures++;
    }
    private static void Equal(float expected, float actual, string label) =>
        Record(Mathf.Abs(expected - actual) < .01f, $"{label}: expected={expected}, actual={actual}");

    private static PotionData Data(PotionElement element)
    {
        // Use inventory's legacy assets, exercising Resources fallback and serialization.
        foreach (string guid in AssetDatabase.FindAssets("t:PotionData", new[] { "Assets/07.Datas/PotionData" }))
        {
            var data = AssetDatabase.LoadAssetAtPath<PotionData>(AssetDatabase.GUIDToAssetPath(guid));
            if (data.Element == element) return data;
        }
        throw new Exception("Missing potion " + element);
    }

    private static PotionValidationTarget Target(bool player = false, bool colliders = true)
    {
        var go = new GameObject("Validation target");
        Objects.Add(go);
        var target = go.AddComponent<PotionValidationTarget>();
        var receiver = go.AddComponent<PotionEffectReceiver>();
        var serialized = new SerializedObject(receiver);
        serialized.FindProperty("_targetKind").intValue = (int)(player ? PotionTargetKind.Player : PotionTargetKind.Enemy);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        if (colliders)
        {
            go.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            go.AddComponent<CircleCollider2D>().radius = .1f;
            go.AddComponent<BoxCollider2D>().size = Vector2.one * .1f;
        }
        else
        {
            go.transform.position = Vector3.one * 50;
        }

        return target;
    }

    private static PotionGroundArea Area(PotionElement element, IDamageable owner, Vector2 position)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/PotionGroundArea.prefab");
        var go = Object.Instantiate(prefab, position, Quaternion.identity);
        Objects.Add(go);
        var area = go.GetComponent<PotionGroundArea>();
        area.SetOwner(owner);
        area.Initialize(Data(element));
        area.Activate();
        return area;
    }

    private static void Cleanup()
    {
        foreach (var go in Objects) if (go != null) Object.DestroyImmediate(go);
        Objects.Clear();
    }

    private static IEnumerator Suite()
    {
        var eventTarget = Target();
        var eventOwner = Target(true, false);
        var eventReceiver = eventTarget.GetComponent<PotionEffectReceiver>();
        var notifications = new List<string>();
        eventReceiver.StatusDamageApplied += result =>
        {
            notifications.Add("status");
            Equal(5.8f, eventOwner.Healing, "status event observes completed life steal");
            eventReceiver.ClearStatuses();
        };
        eventReceiver.DamageApplied += result => notifications.Add("damage");
        var eventSource = new PotionEffectSource(9001, owner: eventOwner, lifeStealRatio: .2f);
        PotionEffectApplier.ApplyProfilePhase(Data(PotionElement.Vaporization),
            PotionEffectPhase.Impact, eventReceiver, eventSource);
        Record(string.Join(",", notifications) == "damage", "direct damage emits only shared event");
        Equal(5, eventOwner.Healing, "direct damage restores life steal once");
        notifications.Clear();
        eventReceiver.ApplyStatus(new PotionStatusApplication(PotionStatusEffect.Burning, 2,
            eventSource, PotionElement.Fire, 4, 1, applyImmediately: true));
        Record(string.Join(",", notifications) == "status,damage", "immediate status preserves event order despite clear callback");
        Equal(29, eventTarget.Damage, "direct and immediate status damage applied once");
        yield return 2.2d;
        Equal(29, eventTarget.Damage, "cleared status does not damage again");
        Cleanup();

        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Player/Player.prefab");
        Equal(8, playerPrefab.GetComponent<PlayerStatus>().MaxThrowDistance, "player prefab planned throw distance");
        Equal(10, new SerializedObject(playerPrefab.GetComponent<PlayerThrowingModule>()).FindProperty("_throwSpeed").floatValue,
            "player prefab planned flight speed");

        var playerObject = new GameObject("Player status integration");
        Objects.Add(playerObject);
        playerObject.SetActive(false);
        var playerStatus = playerObject.AddComponent<PlayerStatus>();
        var playerReceiver = playerObject.AddComponent<PotionEffectReceiver>();
        playerObject.SetActive(true);
        PotionEffectApplier.ApplyDrinkEffect(Data(PotionElement.Ice), playerReceiver);
        Equal(85, playerStatus.CurrentHealth, "real PlayerStatus frost drink HP");
        Equal(0, playerStatus.Speed, "real PlayerStatus frozen walk");
        Equal(0, playerStatus.DashSpeed, "real PlayerStatus frozen dash");
        yield return 2.2d;
        Equal(5, playerStatus.Speed, "real PlayerStatus thawed walk");
        Equal(15, playerStatus.DashSpeed, "real PlayerStatus thawed dash");
        Cleanup();

        var projectileTarget = Target();
        var projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Potion.prefab");
        var projectile = Object.Instantiate(projectilePrefab, Vector3.left, Quaternion.identity).GetComponent<PotionProjectile>();
        Objects.Add(projectile.gameObject);
        projectile.Launch(Data(PotionElement.Fire), null, Vector3.zero, .3f, 1.5f, 360);
        yield return 9d;
        Equal(40, projectileTarget.Damage, "actual projectile landing through prefab to floor and residual");
        Record(projectile == null, "projectile removed after landing");
        Cleanup();

        var frostTarget = Target();
        var frostReceiver = frostTarget.GetComponent<PotionEffectReceiver>();
        Area(PotionElement.Ice, null, Vector2.zero);
        yield return .3d;
        Equal(1, frostReceiver.FrostStackCount, "frost first physical entry stack");
        Equal(.85f, frostReceiver.MoveSpeedMultiplier, "frost first slow");
        yield return 1d;
        Equal(2, frostReceiver.FrostStackCount, "frost second stack");
        Equal(.7f, frostReceiver.MoveSpeedMultiplier, "frost second slow");
        yield return 1d;
        Equal(0, frostReceiver.FrostStackCount, "frost third tick resets stacks");
        Record(!frostReceiver.CanMove, "frost third tick freezes");
        frostTarget.transform.position = Vector3.right * 20;
        yield return .2d;
        Record(!frostReceiver.CanMove, "existing freeze survives physical exit (current provisional policy)");
        yield return 2.2d;
        Record(frostReceiver.CanMove, "freeze expires after physical exit");
        Cleanup();

        var reentryTarget = Target();
        Area(PotionElement.Fire, null, Vector2.zero);
        yield return .2d;
        Equal(8, reentryTarget.Damage, "fire immediate entry");
        reentryTarget.transform.position = Vector3.right * 20;
        yield return .2d;
        reentryTarget.transform.position = Vector3.zero;
        yield return .2d;
        Equal(16, reentryTarget.Damage, "fire immediate physical reentry before next interval");
        Cleanup();

        var elements = new[] { PotionElement.Heal, PotionElement.Fire, PotionElement.Poison, PotionElement.Ice, PotionElement.Lightning, PotionElement.Vaporization };
        var damage = new float[] { 0, 40, 40, 20, 24, 25 };
        for (int i = 0; i < elements.Length; i++)
        {
            var target = Target(elements[i] == PotionElement.Heal);
            var excluded = Target(elements[i] != PotionElement.Heal);
            var area = Area(elements[i], null, Vector2.zero);
            Equal(elements[i] == PotionElement.Vaporization ? 2.5f : 2, area.AreaCollider.radius, elements[i] + " radius");
            if (i != 5)
            {
                Equal(area.AreaCollider.bounds.size.x, area.GetComponent<SpriteRenderer>().bounds.size.x,
                    elements[i] + " visible floor diameter matches collider");
            }

            yield return 9d;
            Equal(damage[i], target.Damage, elements[i] + " floor + residual, duplicate colliders");
            Equal(i == 0 ? 24 : 0, target.Healing, elements[i] + " healing");
            Equal(0, excluded.Damage + excluded.Healing, elements[i] + " target filter");
            Record(area == null, elements[i] + " floor destroyed");
            Cleanup();
            yield return .1d;
        }

        var drinkDamage = new float[] { 0, 24, 25, 15, 20, 15 };
        for (int i = 0; i < elements.Length; i++)
        {
            var target = Target(true);
            PotionEffectApplier.ApplyDrinkEffect(Data(elements[i]), target.GetComponent<PotionEffectReceiver>());
            yield return 5.5d;
            Equal(drinkDamage[i], target.Damage, elements[i] + " drink damage");
            Equal(i == 0 ? 30 : 0, target.Healing, elements[i] + " drink healing");
            Equal(i == 0 ? 10 : i == 5 ? 80 : 20, target.Thirst, elements[i] + " drink thirst");
            Cleanup();
        }

        var a = new[] { PotionElement.Fire, PotionElement.Fire, PotionElement.Fire, PotionElement.Poison, PotionElement.Poison, PotionElement.Ice };
        var b = new[] { PotionElement.Poison, PotionElement.Ice, PotionElement.Lightning, PotionElement.Ice, PotionElement.Lightning, PotionElement.Lightning };
        var totals = new float[] { 73, 45, 80, 40, 40, 48 };
        for (int i = 0; i < a.Length; i++)
        {
            var target = Target();
            var first = Area(a[i], null, new Vector2(-1, 0));
            var second = Area(b[i], null, new Vector2(1, 0));
            Record(first.IsCombined && second.IsCombined, a[i] + "+" + b[i] + " claimed both originals");
            var polygon = second.GetComponent<PolygonCollider2D>();
            Record(polygon.enabled && polygon.points.Length == 32, "32 point ellipse");
            Physics2D.SyncTransforms();
            Equal(6, polygon.bounds.size.x, "ellipse major diameter");
            Equal(2 * Mathf.Sqrt(8), polygon.bounds.size.y, "ellipse minor diameter");
            var visual = second.transform.Find("CombinationEllipseVisual");
            Record(visual != null, "ellipse visual exists");
            if (visual != null)
            {
                Equal(polygon.bounds.size.x, visual.GetComponent<SpriteRenderer>().bounds.size.x, "ellipse visible major diameter");
                Equal(polygon.bounds.size.y, visual.GetComponent<SpriteRenderer>().bounds.size.y, "ellipse visible minor diameter");
            }
            yield return 9d;
            Equal(totals[i], target.Damage, a[i] + "+" + b[i] + " impact + floor + residual");
            Cleanup();
            yield return .1d;
        }

        var attacks = new[] { PotionElement.Fire, PotionElement.Poison, PotionElement.Ice, PotionElement.Lightning };
        var vampDamage = new float[] { 40, 40, 20, 24 };
        for (int i = 0; i < attacks.Length; i++)
        {
            var target = Target();
            var owner = Target(true, false);
            Area(attacks[i], owner, Vector2.zero);
            Area(PotionElement.Heal, owner, Vector2.zero);
            yield return 9d;
            Equal(vampDamage[i], target.Damage, attacks[i] + " vampiric damage");
            Equal(vampDamage[i] * .2f, owner.Healing, attacks[i] + " vampiric owner including residual");
            Cleanup();
            yield return .1d;

            target = Target();
            Area(attacks[i], null, Vector2.zero);
            Area(PotionElement.Vaporization, null, Vector2.zero);
            yield return 4d;
            Equal(30, target.Damage, attacks[i] + " vapor impact once, no residual");
            Record(Object.FindObjectsByType<PotionGroundArea>(FindObjectsSortMode.None).Length == 0, "vapor removes both floors");
            Cleanup();
            yield return .1d;
        }
    }
}
