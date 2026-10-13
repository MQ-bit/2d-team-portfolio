using System;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// 캡처에는 시각 컴포넌트만 복제한다. UI의 게임 스크립트/버튼 이벤트는 실행하지 않는다.
public static class BookPageCapture
{
    private const int CaptureLayer = 31;

    public static RenderTexture Capture(RectTransform book, Transform excluded, int maxWidth)
    {
        GameObject canvasObject = null;
        GameObject cameraObject = null;
        RenderTexture texture = null;
        try
        {
            Canvas.ForceUpdateCanvases();
            Vector2 size = book.rect.size;
            if (size.x <= 0f || size.y <= 0f) throw new InvalidOperationException("BookPanel 크기가 비어 있습니다.");
            int width = Mathf.Clamp(Mathf.RoundToInt(size.x), 64, Mathf.Max(64, maxWidth));
            int height = Mathf.Max(1, Mathf.RoundToInt(width * size.y / size.x));
            texture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            canvasObject = new GameObject("Book capture canvas", typeof(RectTransform), typeof(Canvas));
            canvasObject.hideFlags = HideFlags.HideAndDontSave;
            canvasObject.layer = CaptureLayer;
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            Canvas originalCanvas = book.GetComponentInParent<Canvas>();
            if (originalCanvas != null) canvas.additionalShaderChannels = originalCanvas.additionalShaderChannels;
            RectTransform canvasRect = (RectTransform)canvas.transform;
            canvasRect.position = new Vector3(10000f, 10000f, 0f);
            canvasRect.sizeDelta = size;

            RectTransform visualRoot = new GameObject("Book visuals", typeof(RectTransform)).GetComponent<RectTransform>();
            visualRoot.gameObject.layer = CaptureLayer;
            visualRoot.SetParent(canvasRect, false);
            visualRoot.anchorMin = Vector2.zero;
            visualRoot.anchorMax = Vector2.one;
            visualRoot.sizeDelta = Vector2.zero;
            CopyVisualComponents(book.gameObject, visualRoot.gameObject);
            foreach (Transform child in book) CloneVisualTree(child, visualRoot, excluded);

            cameraObject = new GameObject("Book capture camera", typeof(Camera));
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.enabled = false;
            camera.transform.position = canvasRect.position + Vector3.back * 10f;
            camera.orthographic = true;
            camera.orthographicSize = size.y * 0.5f;
            camera.aspect = size.x / size.y;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 50f;
            camera.cullingMask = 1 << CaptureLayer;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.targetTexture = texture;
            canvas.worldCamera = camera;
            Canvas.ForceUpdateCanvases();
            foreach (TMP_Text label in canvasObject.GetComponentsInChildren<TMP_Text>()) label.ForceMeshUpdate();
            Canvas.ForceUpdateCanvases();

            if (GraphicsSettings.currentRenderPipeline != null)
            {
                UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = false;
                data.requiresColorOption = CameraOverrideOption.Off;
                data.requiresDepthOption = CameraOverrideOption.Off;
                RenderPipeline.SubmitRenderRequest(camera,
                    new UniversalRenderPipeline.SingleCameraRequest { destination = texture });
            }
            else
            {
                camera.Render();
            }

            return texture;
        }
        catch
        {
            if (texture != null) RenderTexture.ReleaseTemporary(texture);
            throw;
        }
        finally
        {
            if (canvasObject != null) { canvasObject.SetActive(false); DestroyObject(canvasObject); }
            if (cameraObject != null) DestroyObject(cameraObject);
        }
    }

    public static RenderTexture Crop(RenderTexture spread, RectTransform book, RectTransform page)
    {
        Rect area = PageArea(book, page);
        Rect bounds = book.rect;
        Vector2 scale = new Vector2(area.width / bounds.width, area.height / bounds.height);
        Vector2 offset = new Vector2((area.xMin - bounds.xMin) / bounds.width,
            (area.yMin - bounds.yMin) / bounds.height);
        RenderTexture texture = RenderTexture.GetTemporary(
            Mathf.Max(1, Mathf.RoundToInt(spread.width * scale.x)),
            Mathf.Max(1, Mathf.RoundToInt(spread.height * scale.y)), 0, RenderTextureFormat.ARGB32);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        Graphics.Blit(spread, texture, scale, offset);
        return texture;
    }

    public static Rect PageArea(RectTransform parent, RectTransform page)
    {
        Vector3[] corners = new Vector3[4];
        page.GetWorldCorners(corners);
        Vector3 min = parent.InverseTransformPoint(corners[0]);
        Vector3 max = parent.InverseTransformPoint(corners[2]);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private static void CloneVisualTree(Transform source, Transform parent, Transform excluded)
    {
        if (source == excluded || !source.gameObject.activeSelf || source.GetComponent<TMP_SubMeshUI>() != null) return;
        RectTransform original = source as RectTransform;
        if (original == null) return;
        RectTransform clone = new GameObject(source.name, typeof(RectTransform)).GetComponent<RectTransform>();
        clone.gameObject.layer = CaptureLayer;
        clone.SetParent(parent, false);
        clone.anchorMin = original.anchorMin;
        clone.anchorMax = original.anchorMax;
        clone.pivot = original.pivot;
        clone.sizeDelta = original.sizeDelta;
        clone.anchoredPosition3D = original.anchoredPosition3D;
        clone.localRotation = original.localRotation;
        clone.localScale = original.localScale;
        CopyVisualComponents(source.gameObject, clone.gameObject);
        foreach (Transform child in source) CloneVisualTree(child, clone, excluded);
    }

    private static void CopyVisualComponents(GameObject source, GameObject target)
    {
        Image originalImage = source.GetComponent<Image>();
        if (originalImage != null && originalImage.enabled)
        {
            Image image = target.AddComponent<Image>();
            image.sprite = originalImage.overrideSprite;
            image.type = originalImage.type;
            image.color = originalImage.color;
            image.material = originalImage.material;
            image.preserveAspect = originalImage.preserveAspect;
            image.pixelsPerUnitMultiplier = originalImage.pixelsPerUnitMultiplier;
            image.fillAmount = originalImage.fillAmount;
            image.fillMethod = originalImage.fillMethod;
            image.fillOrigin = originalImage.fillOrigin;
            image.fillClockwise = originalImage.fillClockwise;
            image.fillCenter = originalImage.fillCenter;
            image.raycastTarget = false;
            image.maskable = originalImage.maskable;
        }
        RawImage originalRaw = source.GetComponent<RawImage>();
        if (originalRaw != null && originalRaw.enabled)
        {
            CurvedBookPageImage curved = originalRaw as CurvedBookPageImage;
            RawImage raw = curved != null ? target.AddComponent<CurvedBookPageImage>() : target.AddComponent<RawImage>();
            if (curved != null) ((CurvedBookPageImage)raw).CopySurfaceFrom(curved);
            raw.texture = originalRaw.texture;
            raw.uvRect = originalRaw.uvRect;
            raw.color = originalRaw.color;
            raw.material = originalRaw.material;
            raw.raycastTarget = false;
        }
        TextMeshProUGUI originalText = source.GetComponent<TextMeshProUGUI>();
        if (originalText != null && originalText.enabled)
        {
            TextMeshProUGUI text = target.AddComponent<TextMeshProUGUI>();
            text.font = originalText.font;
            text.fontSharedMaterial = originalText.fontSharedMaterial;
            text.text = originalText.text;
            text.color = originalText.color;
            text.fontSize = originalText.fontSize;
            text.fontStyle = originalText.fontStyle;
            text.fontWeight = originalText.fontWeight;
            text.enableAutoSizing = originalText.enableAutoSizing;
            text.fontSizeMin = originalText.fontSizeMin;
            text.fontSizeMax = originalText.fontSizeMax;
            text.alignment = originalText.alignment;
            text.margin = originalText.margin;
            text.textWrappingMode = originalText.textWrappingMode;
            text.overflowMode = originalText.overflowMode;
            text.richText = originalText.richText;
            text.characterSpacing = originalText.characterSpacing;
            text.lineSpacing = originalText.lineSpacing;
            text.raycastTarget = false;
        }
        CanvasGroup originalGroup = source.GetComponent<CanvasGroup>();
        if (originalGroup != null && originalGroup.enabled)
        {
            CanvasGroup group = target.AddComponent<CanvasGroup>();
            group.alpha = originalGroup.alpha;
            group.ignoreParentGroups = originalGroup.ignoreParentGroups;
            group.interactable = false;
            group.blocksRaycasts = false;
        }
        RectMask2D originalRectMask = source.GetComponent<RectMask2D>();
        if (originalRectMask != null && originalRectMask.enabled)
        {
            RectMask2D mask = target.AddComponent<RectMask2D>();
            mask.padding = originalRectMask.padding;
            mask.softness = originalRectMask.softness;
        }
        Mask originalMask = source.GetComponent<Mask>();
        if (originalMask != null && originalMask.enabled)
            target.AddComponent<Mask>().showMaskGraphic = originalMask.showMaskGraphic;
        foreach (Shadow originalShadow in source.GetComponents<Shadow>())
        {
            if (!originalShadow.enabled || target.GetComponent<Graphic>() == null) continue;
            Shadow shadow = originalShadow is Outline ? target.AddComponent<Outline>() : target.AddComponent<Shadow>();
            shadow.effectColor = originalShadow.effectColor;
            shadow.effectDistance = originalShadow.effectDistance;
            shadow.useGraphicAlpha = originalShadow.useGraphicAlpha;
        }
    }

    private static void DestroyObject(UnityEngine.Object target)
    {
        if (Application.isPlaying) UnityEngine.Object.Destroy(target);
        else UnityEngine.Object.DestroyImmediate(target);
    }
}
