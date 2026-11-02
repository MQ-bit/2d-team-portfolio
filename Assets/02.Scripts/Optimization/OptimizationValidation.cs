#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Differential checks execute the real original and improved implementations.
public static class OptimizationValidation
{
    private sealed class Profile : IPotionEffectData
    {
        public PotionTargetType TargetType => PotionTargetType.Both;
        public float EffectRadius => 2;
        public bool CreatesFloor => true;
        public float FloorDuration => 10;
        public float TickInterval => 1;
        public bool ApplyOnEnter => true;
        public float LifeStealRatio => 0;
        public IReadOnlyList<PotionEffectDefinition> Effects { get; set; }
        public PotionFrostSettings Frost => null;
    }
    [Serializable]
    private sealed class Result
    {
        public string unity, utc;
        public int assertions;
        public int gameMaskComparisons;
        public long originalStatusBytes, improvedStatusBytes;
        public bool allocationCounterSupported;
        public string status = "Passed";
    }
    private static int _checks;
    private static void Check(bool condition, string message)
    { _checks++; if (!condition) throw new Exception(message); }

    public static string Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop the demo before differential validation.");
        _checks = 0;
        var result = new Result { unity = Application.unityVersion, utc = DateTime.UtcNow.ToString("O") };
        ValidateQueries(result);
        ValidateTimeline();
        ValidateMasks();
        ValidateGameDefaultMasks(result);
        ValidateSearch();
        result.assertions = _checks;
        string json = JsonUtility.ToJson(result, true);
        Directory.CreateDirectory("Logs/Optimization");
        File.WriteAllText("Logs/Optimization/functional-validation.json", json);
        return json;
    }

    private static void ValidateQueries(Result result)
    {
        var original = new PotionAreaEffectTracker();
        var improved = new OptimizedPotionAreaEffectTracker();
        foreach (var nullableProfile in new IPotionEffectData[] { null, new Profile { Effects = null } })
        {
            original.Enter(nullableProfile, new PotionEffectSource(1));
            improved.Enter(nullableProfile, new PotionEffectSource(1));
            foreach (PotionStatusEffectTracker query in Enum.GetValues(typeof(PotionStatusEffectTracker)))
                Check(original.HasStatus(query) == improved.HasStatus(query), "Null profile/list status differs.");
            foreach (PotionElement element in Enum.GetValues(typeof(PotionElement)))
                Check(original.HasDamageElement(element) == improved.HasDamageElement(element), "Null profile/list damage differs.");
            original.Clear(); improved.Clear();
        }
        foreach (PotionEffectPhase phase in Enum.GetValues(typeof(PotionEffectPhase)))
        {
            foreach (PotionEffectOperation operation in Enum.GetValues(typeof(PotionEffectOperation)))
            {
                foreach (PotionStatusEffectTracker status in Enum.GetValues(typeof(PotionStatusEffectTracker)))
                {
                    var effect = JsonUtility.FromJson<PotionEffectDefinition>("{\"_phase\":" + (int)phase +
                        ",\"_operation\":" + (int)operation + ",\"_status\":" + (int)status +
                        ",\"_element\":" + (int)PotionElement.Fire + "}");
                    var profile = new Profile { Effects = new PotionEffectDefinition[] { null, effect } };
                    original.Enter(profile, new PotionEffectSource(1));
                    improved.Enter(profile, new PotionEffectSource(1));
                    foreach (PotionStatusEffectTracker query in Enum.GetValues(typeof(PotionStatusEffectTracker)))
                        Check(original.HasStatus(query) == improved.HasStatus(query), "Status query differs.");
                    foreach (PotionElement element in Enum.GetValues(typeof(PotionElement)))
                        Check(original.HasDamageElement(element) == improved.HasDamageElement(element), "Damage element differs.");
                    original.Clear(); improved.Clear();
                }
            }
        }

        var persistent = new Profile
        {
            Effects = new[] {
            JsonUtility.FromJson<PotionEffectDefinition>("{\"_phase\":2,\"_operation\":2,\"_status\":" +
                (int)PotionStatusEffectTracker.Burning + "}") }
        };
        original.Enter(persistent, new PotionEffectSource(10));
        improved.Enter(persistent, new PotionEffectSource(10));
        original.Enter(persistent, new PotionEffectSource(11));
        improved.Enter(persistent, new PotionEffectSource(11));
        original.Exit(10, out _, out _); improved.Exit(10, out _, out _);
        Check(original.HasStatus(PotionStatusEffectTracker.Burning) && improved.HasStatus(PotionStatusEffectTracker.Burning),
            "Removing one source removed another source's status.");
        for (int i = 0; i < 100; i++) { original.HasStatus(PotionStatusEffectTracker.Frozen); improved.HasStatus(PotionStatusEffectTracker.Frozen); }
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 10000; i++) original.HasStatus(PotionStatusEffectTracker.Frozen);
        result.originalStatusBytes = GC.GetAllocatedBytesForCurrentThread() - before;
        before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 10000; i++) improved.HasStatus(PotionStatusEffectTracker.Frozen);
        result.improvedStatusBytes = GC.GetAllocatedBytesForCurrentThread() - before;
        result.allocationCounterSupported = result.originalStatusBytes > 0;
        // Unity's Mono can return zero even for an explicit allocation. In that
        // environment these counters are unavailable; use binary Profiler captures.
        if (result.allocationCounterSupported)
            Check(result.improvedStatusBytes < result.originalStatusBytes, "Status allocation did not decrease.");
    }

    private static List<string> TimelineTrace(bool improved, int mode)
    {
        var trace = new List<string>();
        Action<IEnumerable<int>, double> sync = null;
        Action<bool> end = null;
        bool nested = false;
        Action<int> enter = id =>
        {
            trace.Add("enter:" + id);
            if (mode == 1) end(false);
            if (mode == 2 && !nested) { nested = true; sync(new[] { 1, 2 }, 0); }
        };
        Action<int> tick = id => { trace.Add("tick:" + id); if (mode == 3) end(true); };
        Action<int, bool> exit = (id, residual) => { trace.Add("exit:" + id + ":" + residual); if (mode == 4) end(false); };
        if (improved)
        {
            var timeline = new OptimizedPotionAreaTimeline<int>(10, 1, true, _ => true, enter, tick, exit);
            sync = timeline.Synchronize; end = timeline.End;
        }
        else
        {
            var timeline = new PotionAreaTimeline<int>(10, 1, true, _ => true, enter, tick, exit);
            sync = timeline.Synchronize; end = timeline.End;
        }
        sync(new[] { 1, 1, 2 }, 0);
        sync(new[] { 1, 2 }, .5);
        sync(new[] { 2, 3 }, 1);
        sync(new[] { 2, 3 }, 5);
        sync(new[] { 3 }, 10);
        end(true);
        sync(new[] { 4 }, 11);
        return trace;
    }
    private static void ValidateTimeline()
    {
        for (int mode = 0; mode < 5; mode++)
        {
            Check(string.Join("|", TimelineTrace(false, mode)) == string.Join("|", TimelineTrace(true, mode)),
                "Timeline event trace differs, callback mode=" + mode);
        }
    }

    private static Color32[] Read(Texture texture)
    {
        RenderTexture previous = RenderTexture.active;
        RenderTexture target = RenderTexture.GetTemporary(texture.width, texture.height, 0);
        var copy = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
        try
        {
            Graphics.Blit(texture, target);
            RenderTexture.active = target;
            copy.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            copy.Apply();
            return copy.GetPixels32();
        }
        finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); Object.DestroyImmediate(copy); }
    }
    private static void ValidateMasks()
    {
        Check(OptimizedPotionAreaMaskRenderer.CachedFootprints == 0, "Mask cache not empty before validation.");
        Vector2[] outline = { new(-2, -1), new(2, -1), new(2, 1), new(-2, 1) };
        Vector2[] wall = { new(.5f, -2), new(3, -2), new(3, 2), new(.5f, 2) };
        PotionAreaFootprint footprint = PotionAreaClipper.Subtract(outline, Vector2.zero, new List<Vector2[]> { wall });
        Check(footprint != null && footprint.IsClipped, "Clipped footprint missing.");
        var original = new PotionAreaMaskRenderer();
        var first = new OptimizedPotionAreaMaskRenderer();
        var second = new OptimizedPotionAreaMaskRenderer();
        var a = new MaterialPropertyBlock(); var b = new MaterialPropertyBlock(); var c = new MaterialPropertyBlock();
        try
        {
            original.SetFootprint(footprint); first.SetFootprint(footprint); second.SetFootprint(footprint);
            original.Apply(a); first.Apply(b); second.Apply(c);
            Texture expected = a.GetTexture("_AreaClipMask"); Texture actual = b.GetTexture("_AreaClipMask");
            Check(actual == c.GetTexture("_AreaClipMask"), "Same footprint did not share its texture.");
            Check(expected.width == actual.width && expected.height == actual.height, "Mask dimensions changed.");
            Color32[] x = Read(expected), y = Read(actual);
            Check(x.Length == y.Length, "Mask pixel count changed.");
            for (int i = 0; i < x.Length; i++) Check(x[i].Equals(y[i]), "Mask pixels differ at " + i);
            first.SetFootprint(footprint); first.Apply(b);
            Check(actual == b.GetTexture("_AreaClipMask"), "Repeated binding recreated the texture.");
            first.Release(); second.Apply(c);
            Check(actual != null && actual == c.GetTexture("_AreaClipMask"), "First release invalidated the remaining lease.");
            second.Release();
            Check(OptimizedPotionAreaMaskRenderer.CachedFootprints == 0, "Last release leaked a cache entry.");
            second.SetFootprint(null); second.Apply(c);
            Check(c.GetFloat("_AreaClipEnabled") == 0, "Null footprint did not disable clipping.");
        }
        finally { original.Release(); first.Release(); second.Release(); }
    }

    // The renderer the game really uses (ComparisonAreaMaskRenderer with the default variant) must produce the same
    // pixels as the original renderer for many footprints: varied outlines clipped by one to three walls.
    private static void ValidateGameDefaultMasks(Result result)
    {
        Check(OptimizationRuntime.Variant == OptimizationVariant.GameDefault, "Game default variant is not applied.");
        var random = new System.Random(20261102);
        int compared = 0;
        for (int n = 0; n < 60; n++)
        {
            int sides = 4 + random.Next(0, 13);
            float radiusX = 1.5f + (float)random.NextDouble() * 3f, radiusY = 1.5f + (float)random.NextDouble() * 3f;
            var outline = new Vector2[sides];
            for (int i = 0; i < sides; i++)
            {
                float angle = i * Mathf.PI * 2f / sides;
                outline[i] = new Vector2(Mathf.Cos(angle) * radiusX, Mathf.Sin(angle) * radiusY);
            }

            var walls = new List<Vector2[]>();
            int wallCount = 1 + random.Next(0, 3);
            for (int w = 0; w < wallCount; w++)
            {
                float x = -3f + (float)random.NextDouble() * 6f, y = -3f + (float)random.NextDouble() * 6f;
                float width = .3f + (float)random.NextDouble() * 2f, height = .3f + (float)random.NextDouble() * 2f;
                walls.Add(new Vector2[] { new(x, y), new(x + width, y), new(x + width, y + height), new(x, y + height) });
            }

            PotionAreaFootprint footprint = PotionAreaClipper.Subtract(outline, Vector2.zero, walls);
            if (footprint == null || !footprint.IsClipped) continue;
            var original = new PotionAreaMaskRenderer();
            var game = new ComparisonAreaMaskRenderer();
            var a = new MaterialPropertyBlock(); var b = new MaterialPropertyBlock();
            try
            {
                original.SetFootprint(footprint); game.SetFootprint(footprint);
                original.Apply(a); game.Apply(b);
                Texture expected = a.GetTexture("_AreaClipMask"), actual = b.GetTexture("_AreaClipMask");
                Check(expected.width == actual.width && expected.height == actual.height, "Game mask dimensions changed, case " + n);
                Color32[] x = Read(expected), y = Read(actual);
                for (int i = 0; i < x.Length; i++) Check(x[i].Equals(y[i]), "Game mask pixels differ, case " + n + " pixel " + i);
                compared++;
            }
            finally { original.Release(); game.Release(); }
        }

        Check(compared >= 20, "Too few clipped footprints were compared: " + compared);
        Check(OptimizedPotionAreaMaskRenderer.CachedFootprints == 0, "Game mask validation leaked a cache entry.");
        result.gameMaskComparisons = compared;
    }

    private static void ValidateSearch()
    {
        var root = new GameObject("OptimizationSearchValidation");
        try
        {
            root.transform.position = new Vector3(10000, 10000, 0);
            var player = new GameObject("Player"); player.transform.SetParent(root.transform, false);
            var receiver = player.AddComponent<PotionEffectReceiver>();
            var serialized = new SerializedObject(receiver);
            serialized.FindProperty("_targetKind").intValue = (int)PotionTargetKind.Player;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            player.layer = LayerMask.NameToLayer("Player"); player.AddComponent<BoxCollider2D>();
            var sensor = new GameObject("Sensor"); sensor.transform.SetParent(player.transform, false);
            sensor.AddComponent<CircleCollider2D>().isTrigger = true;
            for (int i = 0; i < 30; i++)
            {
                var enemy = new GameObject("Enemy"); enemy.transform.SetParent(root.transform, false);
                enemy.transform.localPosition = new Vector3(i * .05f, 1, 0);
                enemy.layer = LayerMask.NameToLayer("Enemy"); enemy.AddComponent<BoxCollider2D>();
            }
            OptimizedEnemySearch.ConfigureIsolatedPlayer(receiver);
            Physics2D.SyncTransforms();
            var hits = new List<Collider2D>(); var optimizedHits = new List<Collider2D>();
            foreach (float offset in new[] { 0f, 2f, 8f, 20f })
            {
                Vector2 origin = (Vector2)root.transform.position + Vector2.right * offset;
                Physics2D.OverlapCircle(origin, 5, ContactFilter2D.noFilter, hits);
                Transform expected = null;
                foreach (var hit in hits)
                {
                    var candidate = hit.GetComponentInParent<PotionEffectReceiver>();
                    if (candidate != null && candidate.TargetKind == PotionTargetKind.Player) { expected = candidate.transform; break; }
                }
                Check(expected == OptimizedEnemySearch.Find(origin, 5, optimizedHits), "Layer-filtered target differs.");
            }
            player.SetActive(false); Physics2D.SyncTransforms();
            Check(OptimizedEnemySearch.Find(root.transform.position, 5, optimizedHits) == null, "Disabled target still detected.");
        }
        finally { OptimizedEnemySearch.Clear(); Object.DestroyImmediate(root); }
    }
}
#endif
