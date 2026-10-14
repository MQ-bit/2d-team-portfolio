using System.Collections;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CombinationInfoSlot : MonoBehaviour
{
    [Header("Combination Icons")]
    [SerializeField] private Image _elementAIcon;
    [SerializeField] private Image _elementBIcon;
    [SerializeField] private Image _resultIcon;
    [Header("Combination Details")]
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private TMP_Text _statusLabel;
    [Header("Icon Captions")]
    [SerializeField] private TMP_Text _firstIngredientCaption;
    [SerializeField] private TMP_Text _secondIngredientCaption;
    [SerializeField] private TMP_Text _resultCaption;
    [Header("Empty slot marks")]
    [SerializeField] private GameObject _firstPlaceholder;
    [SerializeField] private GameObject _secondPlaceholder;
    [SerializeField] private GameObject _resultPlaceholder;
    [Header("Result Reveal Animation")]
    [SerializeField, Min(0f)] private float _resultRevealDuration = 0.45f;
    [SerializeField, Range(0.1f, 1f)] private float _resultRevealStartScale = 0.78f;
    [SerializeField, Range(1f, 1.3f)] private float _resultRevealOvershoot = 1.1f;

    private Coroutine _resultRevealRoutine;
    private Vector3 _revealBaseScale;
    private Color _revealBaseColor;
    private Outline _revealOutline;
    private Color _revealOutlineColor;
    private bool _revealOutlineEnabled;

    public void DrawPendingSelection(Sprite firstIcon, Sprite secondIcon)
    {
        DrawIcon(firstIcon, secondIcon, null);
        int selectedCount = (firstIcon != null ? 1 : 0) + (secondIcon != null ? 1 : 0);
        if (_statusLabel != null)
        {
            _statusLabel.text = selectedCount == 2 ? "아직 남겨진 기록이 없다"
                : selectedCount == 1 ? "재료 하나가 더 필요하다" : "아직 밝혀진 것은 없다";
        }

        if (_nameText != null) _nameText.text = "조합 결과";
        if (_descriptionText != null)
        {
            _descriptionText.text = firstIcon == null && secondIcon == null
                ? "왼쪽에서 포션 두 개를 고르자.\n직접 확인한 반응만 이곳에 남는다"
                : secondIcon == null
                    ? "한 가지 재료로는 부족하다.\n함께 쓸 포션을 하나 더 고르자"
                    : "이 조합의 반응은 아직 알려지지 않았다.\n장판이 겹치도록 포션을 던져 확인한다";
        }
    }

    public void DrawRecordedSelectionPending(Sprite firstIcon, Sprite secondIcon)
    {
        DrawPendingSelection(firstIcon, secondIcon);
        if (_statusLabel != null) _statusLabel.text = "기록이 남아 있다";
        if (_descriptionText != null)
            _descriptionText.text = "이미 확인한 반응이다.\n아래에서 남겨진 기록을 열람하자";
    }

    public void DrawSelection(Sprite firstIcon, Sprite secondIcon, PotionCombinationData combination)
    {
        DrawIcon(firstIcon, secondIcon, combination != null ? combination.VisualData.IconSprite : null);
        if (_statusLabel != null)
            _statusLabel.text = combination != null ? "확인된 반응 기록" : "기록이 없다";
        if (_nameText != null)
            _nameText.text = combination != null ? combination.Name : "포션 조합";
        if (_descriptionText != null)
        {
            _descriptionText.text = combination != null ? combination.Description
                : "이 두 포션에는 조합 효과가 없다\n다른 조합을 찾아보자";
        }
    }

    public void DrawIcon(Sprite elementAIcon, Sprite elementBIcon, Sprite resultIcon)
    {
        StopResultReveal();
        SetImage(_elementAIcon, elementAIcon);
        SetImage(_elementBIcon, elementBIcon);
        SetImage(_resultIcon, resultIcon);
        if (_firstPlaceholder != null) _firstPlaceholder.SetActive(elementAIcon == null);
        if (_secondPlaceholder != null) _secondPlaceholder.SetActive(elementBIcon == null);
        if (_resultPlaceholder != null) _resultPlaceholder.SetActive(resultIcon == null);
    }

    public void DrawTableRecord(PotionData first, PotionData second, PotionCombinationData combination)
    {
        DrawSelection(first != null ? first.VisualData.IconSprite : null,
            second != null ? second.VisualData.IconSprite : null, combination);
        DrawCaptions(first, second, combination);
        Color nameColor = new Color(0.23921569f, 0.18039216f, 0.10980392f);
        if (combination != null)
        {
            nameColor = _resultCaption != null ? _resultCaption.color : combination.VisualData.AreaColor;
            Color.RGBToHSV(nameColor, out float hue, out float saturation, out float value);
            nameColor = Color.HSVToRGB(hue, saturation, Mathf.Min(value, 0.68f));
        }
        SetCaption(_nameText, combination != null ? combination.Name : "조합 반응", nameColor);
        if (combination != null) return;
        if (_statusLabel != null) _statusLabel.text = "열람할 기록을 고르자";
        if (_descriptionText != null)
            _descriptionText.text = "왼쪽 목록에서 발견한 조합을 누르면\n재료와 반응 효과가 이곳에 표시된다";
    }

    public void Clear()
    {
        DrawIcon(null, null, null);
        DrawCaptions(null, null, null);
        if (_nameText != null) _nameText.text = string.Empty;
        if (_descriptionText != null) _descriptionText.text = string.Empty;
        if (_statusLabel != null) _statusLabel.text = string.Empty;
    }

    public void SetVisible(bool isVisible)
    {
        gameObject.SetActive(isVisible);
    }

    public void PlayResultReveal()
    {
        StopResultReveal();
        if (!Application.isPlaying || !isActiveAndEnabled || _resultRevealDuration <= 0f ||
            _resultIcon == null || _resultIcon.sprite == null)
        {
            return;
        }

        _revealBaseScale = _resultIcon.rectTransform.localScale;
        _revealBaseColor = _resultIcon.color;
        _revealOutline = _resultIcon.transform.parent != null
            ? _resultIcon.transform.parent.GetComponent<Outline>() : null;
        if (_revealOutline != null)
        {
            _revealOutlineColor = _revealOutline.effectColor;
            _revealOutlineEnabled = _revealOutline.enabled;
            _revealOutline.enabled = true;
        }
        _resultRevealRoutine = StartCoroutine(RevealResult());
    }

    private IEnumerator RevealResult()
    {
        float elapsed = 0f;
        while (elapsed < _resultRevealDuration)
        {
            float progress = Mathf.Clamp01(elapsed / _resultRevealDuration);
            float scale = progress < 0.65f
                ? Mathf.Lerp(_resultRevealStartScale, _resultRevealOvershoot,
                    1f - Mathf.Pow(1f - progress / 0.65f, 3f))
                : Mathf.Lerp(_resultRevealOvershoot, 1f,
                    Mathf.SmoothStep(0f, 1f, (progress - 0.65f) / 0.35f));
            _resultIcon.rectTransform.localScale = _revealBaseScale * scale;
            Color iconColor = _revealBaseColor;
            iconColor.a *= Mathf.Clamp01(progress / 0.4f);
            _resultIcon.color = iconColor;
            if (_revealOutline != null)
            {
                Color glow = _revealOutlineColor;
                glow.a = Mathf.Lerp(1f, _revealOutlineColor.a, progress);
                _revealOutline.effectColor = glow;
            }
            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }
        RestoreRevealAppearance();
        _resultRevealRoutine = null;
    }

    private void OnDisable() => StopResultReveal();

    private void StopResultReveal()
    {
        if (_resultRevealRoutine == null) return;
        StopCoroutine(_resultRevealRoutine);
        _resultRevealRoutine = null;
        RestoreRevealAppearance();
    }

    private void RestoreRevealAppearance()
    {
        if (_resultIcon != null)
        {
            _resultIcon.rectTransform.localScale = _revealBaseScale;
            _resultIcon.color = _revealBaseColor;
        }
        if (_revealOutline != null)
        {
            _revealOutline.effectColor = _revealOutlineColor;
            _revealOutline.enabled = _revealOutlineEnabled;
        }
        _revealOutline = null;
    }

    public void DrawCaptions(PotionData first, PotionData second, PotionCombinationData result)
    {
        Color ingredientDefault = new Color(0.36f, 0.25f, 0.13f);
        Color resultDefault = new Color(0.48f, 0.35f, 0.18f);
        SetCaption(_firstIngredientCaption, first != null ? first.Name : "첫 번째 포션",
            first != null ? GetPotionCaptionColor(first) : ingredientDefault);
        SetCaption(_secondIngredientCaption, second != null ? second.Name : "두 번째 포션",
            second != null ? GetPotionCaptionColor(second) : ingredientDefault);

        Color resultColor = resultDefault;
        if (result != null)
        {
            // Vaporization results use the reacting element's ink color rather than white mist.
            PotionData element = first != null && first.Element != PotionElement.Vaporization ? first : second;
            resultColor = result.ElementA == PotionElement.Vaporization || result.ElementB == PotionElement.Vaporization
                ? element != null ? GetPotionCaptionColor(element) : resultDefault
                : result.VisualData.AreaColor;
            resultColor.a = 1f;
        }
        SetCaption(_resultCaption, result != null ? result.Name : "결과", resultColor);
    }

    private static Color GetPotionCaptionColor(PotionData potion)
    {
        // Reuse the element color already used by the potion's name in the dictionary.
        Match colorTag = Regex.Match(potion.Name, @"<color=(#[0-9a-fA-F]{6,8})>");
        if (colorTag.Success && ColorUtility.TryParseHtmlString(colorTag.Groups[1].Value, out Color color))
        {
            color.a = 1f;
            return color;
        }
        Color fallback = potion.VisualData.AreaColor;
        fallback.a = 1f;
        return fallback;
    }

    private static void SetCaption(TMP_Text caption, string title, Color color)
    {
        if (caption == null) return;
        caption.text = Regex.Replace(title, "<[^>]*>", string.Empty);
        caption.color = color;
    }

    private static void SetImage(Image targetIcon, Sprite sprite)
    {
        if (targetIcon == null)
        {
            return;
        }

        targetIcon.sprite = sprite;
        targetIcon.preserveAspect = false;
        targetIcon.color = sprite == null ? Color.clear : Color.white;
    }

}
