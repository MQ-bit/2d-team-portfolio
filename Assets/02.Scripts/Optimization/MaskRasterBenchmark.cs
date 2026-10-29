#if UNITY_EDITOR
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

// Batch-mode micro benchmark: Unity.exe -batchmode -quit -executeMethod MaskRasterBenchmark.Run
// Synthetic wall-clipped puddle: an ellipse fan of triangles, like the footprints seen in the demo.
public static class MaskRasterBenchmark
{
    public static void Run()
    {
        foreach (int triangles in new[] { 100, 400 })
        {
            Vector2[][] fan = BuildFan(triangles);
            var bounds = Rect.MinMaxRect(-5f, -2.5f, 5f, 2.5f);
            const int width = 256, height = 128;
            var reference = new Color32[width * height];
            var fast = new Color32[width * height];
            double referenceMs = Measure(() => Fill(fan, bounds, width, height, reference, false), 30);
            double fastMs = Measure(() => Fill(fan, bounds, width, height, fast, true), 30);
            int different = 0;
            for (int i = 0; i < reference.Length; i++) if (reference[i].a != fast[i].a) different++;
            Debug.Log($"MaskBench triangles={triangles} boundingBox={referenceMs:F3}ms spans={fastMs:F3}ms different={different}");
        }
    }

    private static void Fill(Vector2[][] fan, Rect bounds, int width, int height, Color32[] pixels, bool spans)
    {
        System.Array.Clear(pixels, 0, pixels.Length);
        foreach (Vector2[] triangle in fan)
        {
            if (spans) OptimizedPotionAreaMaskRenderer.FillTriangleSpans(triangle, bounds, width, height, pixels);
            else OptimizedPotionAreaMaskRenderer.FillBoundingBox(triangle, bounds, width, height, pixels);
        }
    }

    private static double Measure(System.Action action, int iterations)
    {
        action();
        var watch = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++) action();
        return watch.Elapsed.TotalMilliseconds / iterations;
    }

    private static Vector2[][] BuildFan(int count)
    {
        var result = new Vector2[count][];
        for (int i = 0; i < count; i++)
        {
            float a0 = i * Mathf.PI * 2f / count, a1 = (i + 1) * Mathf.PI * 2f / count;
            result[i] = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(Mathf.Cos(a0) * 4.5f, Mathf.Sin(a0) * 2.2f),
                new Vector2(Mathf.Cos(a1) * 4.5f, Mathf.Sin(a1) * 2.2f)
            };
        }
        return result;
    }
}
#endif
