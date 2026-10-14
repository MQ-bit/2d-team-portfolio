using System;
using System.Collections;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// 실제 UI 캡처/양방향 넘김/중도 취소를 에디터에서 확인하는 메뉴.
internal static class BookPageTurnValidation
{
    private static GameObject _hud;
    private static IEnumerator _animation;
    private static BookPageTurnController _controller;
    private static PotionCombinationTableView _table;
    private static int _phase;
    private static bool _savedFrame;
    private static float _previousTimeScale;
    private static double _deadline;

    [MenuItem("Tools/Book UI/Validate Page Capture and Turn")]
    private static void Run()
    {
        if (EditorApplication.isPlaying || _hud != null) return;
        _previousTimeScale = Time.timeScale;
        try
        {
            _hud = PrefabUtility.LoadPrefabContents("Assets/03.Prefabs/UI/IngameUI/PotionInformationHUD.prefab");
            Canvas canvas = _hud.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rect = (RectTransform)_hud.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(1920f, 1080f);
            _hud.SetActive(true);
            _controller = _hud.GetComponentInChildren<BookPageTurnController>(true);
            _table = _hud.GetComponent<PotionCombinationTableView>();
            if (_controller == null || _table == null) throw new Exception("Book prefab references missing.");
            var controllerFields = new SerializedObject(_controller);
            controllerFields.FindProperty("_pageTurnSound").objectReferenceValue = null;
            controllerFields.ApplyModifiedPropertiesWithoutUndo();
            Invoke(_table, "Awake");
            Invoke(_controller, "Awake");
            Invoke(_table, "SetTab", false);
            Canvas.ForceUpdateCanvases();
            Time.timeScale = 0f;
            Directory.CreateDirectory("Temp/BookTurnValidation");
            _phase = 0;
            StartTurn(true);
            EditorApplication.update += Step;
        }
        catch (Exception exception) { Finish("FAIL: " + exception); }
    }

    private static void StartTurn(bool forward)
    {
        _savedFrame = false;
        _deadline = EditorApplication.timeSinceStartup + 10f;
        _animation = (IEnumerator)Invoke(_controller, "Turn", forward,
            (Action)(() => Invoke(_table, "SetTab", forward)));
    }

    private static void Step()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > _deadline) throw new Exception("Turn timed out at timeScale zero.");
            bool running = _animation.MoveNext();
            if (running)
            {
                if (!_controller.IsTurning) throw new Exception("Animation ended before its coroutine.");
                var fields = new SerializedObject(_controller);
                CanvasGroup input = (CanvasGroup)fields.FindProperty("_inputGroup").objectReferenceValue;
                if (input.interactable || input.blocksRaycasts) throw new Exception("Page input is not locked.");
                CurvedBookPageImage page = (CurvedBookPageImage)fields.FindProperty("_turningPage").objectReferenceValue;
                bool nearFold = Mathf.Abs(page.Project(1f, 0f).x) < page.rectTransform.rect.width * 0.35f;
                if (!_savedFrame && (nearFold || _phase == 2))
                {
                    RectTransform book = (RectTransform)_controller.transform;
                    RenderTexture texture = BookPageCapture.Capture(book, null, 1024);
                    try { Save(texture, "Temp/BookTurnValidation/turn_" + _phase + ".png"); }
                    finally { RenderTexture.ReleaseTemporary(texture); }
                    _savedFrame = true;
                }
                if (_phase == 2)
                {
                    ((IDisposable)_animation).Dispose();
                    Invoke(_controller, "OnDisable");
                    CheckRestored();
                    Finish("PASS: captured UI, forward/backward turns at timeScale zero, and cancellation restored UI/input.");
                }
                return;
            }
            if (!_savedFrame) throw new Exception("Capture did not produce an animated frame.");
            CheckRestored();
            _phase++;
            if (_phase == 1) StartTurn(false);
            else if (_phase == 2) StartTurn(true);
        }
        catch (Exception exception) { Finish("FAIL: " + exception); }
    }

    private static void CheckRestored()
    {
        if (_controller.IsTurning) throw new Exception("Turn state did not reset.");
        var fields = new SerializedObject(_controller);
        CanvasGroup input = (CanvasGroup)fields.FindProperty("_inputGroup").objectReferenceValue;
        if (!input.interactable || !input.blocksRaycasts) throw new Exception("Page input was not restored.");
        SerializedProperty groups = fields.FindProperty("_contentGroups");
        for (int i = 0; i < groups.arraySize; i++)
        {
            if (((CanvasGroup)groups.GetArrayElementAtIndex(i).objectReferenceValue).alpha != 1f)
                throw new Exception("Page alpha was not restored.");
        }
    }

    private static object Invoke(object target, string method, params object[] args) =>
        target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);

    private static void Save(RenderTexture texture, string path)
    {
        RenderTexture previous = RenderTexture.active;
        Texture2D image = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
        try
        {
            RenderTexture.active = texture;
            image.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            image.Apply();
            if (Array.TrueForAll(image.GetPixels32(), pixel => pixel.a == 0))
                throw new Exception("Rendered book capture is empty.");
            File.WriteAllBytes(path, image.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(image);
        }
    }

    private static void Finish(string report)
    {
        EditorApplication.update -= Step;
        if (_animation is IDisposable disposable) disposable.Dispose();
        _animation = null;
        if (_controller != null) Invoke(_controller, "OnDisable");
        if (_hud != null) PrefabUtility.UnloadPrefabContents(_hud);
        _hud = null;
        _controller = null;
        Time.timeScale = _previousTimeScale;
        Directory.CreateDirectory("Temp/BookTurnValidation");
        File.WriteAllText("Temp/BookTurnValidation/result.txt", report);
        if (report.StartsWith("FAIL")) Debug.LogError(report);
        else Debug.Log(report);
    }
}
