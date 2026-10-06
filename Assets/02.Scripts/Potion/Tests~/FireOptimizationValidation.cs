#if UNITY_EDITOR
namespace PotionValidation
{
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;


public static class FireOptimizationValidation
{

    public static void Run()
    {
        var scene = EditorSceneManager.NewPreviewScene();
        Mesh mesh = null;
        Sprite sprite = null;
        RenderTexture target = null;
        Texture2D output = null;
        try
        {
            Directory.CreateDirectory("Temp");
            var root = new GameObject("FireProbeRoot");
            SceneManager.MoveGameObjectToScene(root, scene);
            var groundObject = new GameObject("FireProbeGround");
            groundObject.transform.SetParent(root.transform, false);
            var ground = groundObject.AddComponent<SpriteRenderer>();
            sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f, 2);
            ground.sprite = sprite;
            ground.transform.localScale = new Vector3(6, 3, 1);
            ground.color = Color.clear;
            var type = typeof(PotionAreaVisual).Assembly.GetType("PotionFireVortexRenderer");
            var helper = Activator.CreateInstance(type, true);
            MethodInfo bind = type.GetMethod("Bind", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo apply = type.GetMethod("Apply", BindingFlags.Instance | BindingFlags.NonPublic);
            var watch = System.Diagnostics.Stopwatch.StartNew();
            bind.Invoke(helper, new object[] { ground, ground });
            watch.Stop();
            double bindMs = watch.Elapsed.TotalMilliseconds;
            var field = (MeshRenderer)type.GetField("_renderer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(helper);
            mesh = field.GetComponent<MeshFilter>().sharedMesh;
            if (mesh.vertexCount != 720) throw new Exception("Unexpected flame mesh size.");
            Vector3 position = field.transform.position;
            Vector3 scale = field.transform.localScale;
            ground.transform.localScale = new Vector3(20, 12, 1);
            ground.transform.localPosition = new Vector3(8, 4, 0);
            watch.Restart();
            for (int i = 0; i < 120; i++)
                apply.Invoke(helper, new object[] { ground, new Color(1, 0.42f, 0.075f), i / 30f, 1f });
            watch.Stop();
            if (field.transform.position != position || field.transform.localScale != scale)
                throw new Exception("Flame coordinates followed animated ground bounds.");
            var cameraObject = new GameObject("FireProbeCamera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<UniversalAdditionalCameraData>();
            camera.scene = scene;
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = 4;
            camera.transform.position = new Vector3(0, 1, -10);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.06f, 0.06f, 0.07f, 1);
            target = new RenderTexture(512, 512, 24);
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            output = new Texture2D(512, 512, TextureFormat.RGB24, false);
            output.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
            output.Apply();
            RenderTexture.active = previous;
            int lit = 0;
            foreach (Color pixel in output.GetPixels()) if (pixel.r > 0.18f && pixel.r > pixel.g * 1.4f) lit++;
            if (lit < 100) throw new Exception("Flames were not rendered by the active URP pipeline.");
            if (ShaderUtil.ShaderHasError(field.sharedMaterial.shader)) throw new Exception("Shader compilation failed.");
            File.WriteAllBytes("Temp/FireOptimizationPreview.png", output.EncodeToPNG());
            File.WriteAllText("Temp/FireOptimizationValidation.txt", "PASS: 40 flame strips / 720 vertices; fixed position and scale across 120 updates; URP rendered pixels=" + lit
                + "; bindMs=" + bindMs + "; reflection-inclusive 120 updateMs=" + watch.Elapsed.TotalMilliseconds);
        }
        catch (Exception error)
        {
            File.WriteAllText("Temp/FireOptimizationValidation.txt", "FAIL: " + error);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
            if (mesh != null) UnityEngine.Object.DestroyImmediate(mesh);
            if (sprite != null) UnityEngine.Object.DestroyImmediate(sprite);
            if (target != null) UnityEngine.Object.DestroyImmediate(target);
            if (output != null) UnityEngine.Object.DestroyImmediate(output);
        }
    }
}
}
#endif
