#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Play-mode regression for native Unity resources created during AddComponent.
[InitializeOnLoad]
public static class VfxInitializationValidation
{
    private const string Pending = "PotionVfxInitializationValidation.Pending";
    static VfxInitializationValidation()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
                Validate();
        };
    }

    public static void Run()
    {
        SessionState.SetBool(Pending, true);
        EditorApplication.EnterPlaymode();
    }

    private static void Validate()
    {
        SessionState.SetBool(Pending, false);
        var errors = new List<string>();
        Application.LogCallback capture = (message, stack, type) =>
        {
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
                errors.Add(message);
        };
        Application.logMessageReceived += capture;
        try
        {
            var source = new GameObject("ValidationVfxSource", typeof(SpriteRenderer));
            var player = source.AddComponent<SpriteSheetPlayer>();
            player.frames = new[] { Texture2D.whiteTexture };
            source.AddComponent<PotionAreaVfxSettings>().burstCount = 1;

            var go = new GameObject("PotionVfxBurstField");
            // This follows the reported runtime path: AddComponent -> Awake -> SetVfx.
            var field = go.AddComponent<VFXBurstField>();
            field.ConfigureArea(2f, 1f, null);
            field.SetVfx(source);
            field.ConfigureFootprint(PotionAreaClipper.Subtract(
                Box(-1f, -0.5f, 1f, 0.5f), new Vector2(-0.5f, 0f),
                new[] { Box(0.1f, -1f, 0.2f, 1f) }));
            SpriteRenderer[] bursts = go.GetComponentsInChildren<SpriteRenderer>();
            if (bursts.Length != 1) throw new Exception("VFX pool was not initialized.");
            var properties = new MaterialPropertyBlock();
            bursts[0].GetPropertyBlock(properties);
            if (properties.GetFloat("_AreaClipEnabled") != 1f)
                throw new Exception("Clipped VFX properties were not applied.");
            if (errors.Count != 0) throw new Exception(string.Join("\n", errors));
            Debug.Log("VFX_INITIALIZATION_VALIDATION_PASS: AddComponent, Awake, SetVfx, clip mask");
            Application.logMessageReceived -= capture;
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Application.logMessageReceived -= capture;
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static Vector2[] Box(float left, float bottom, float right, float top) => new[]
    {
        new Vector2(left, bottom), new Vector2(right, bottom),
        new Vector2(right, top), new Vector2(left, top)
    };
}
#endif
