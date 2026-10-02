using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DictionaryCardSlotView : MonoBehaviour
{
    private const float HiddenAngle = 180f;
    private const float RevealedAngle = 0f;
    private const float RevealEndAngle = 360f;

    [Header("Flip Animation Setting")]
    [SerializeField] private RectTransform _flipRoot;
    [SerializeField, Min(0f)] private float _revealDuration = 0.5f;
    [SerializeField] private AnimationCurve _revealCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Front Info")]
    [SerializeField] private Image _cardBackgroundImage;
    [SerializeField] private Image _potionImage;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _descriptionText;

    private Coroutine _revealRoutine;
    public bool HasRevealPlayed { get; private set; }
    public bool IsRevealing => _revealRoutine != null;

    public void SetHidden()
    {
        CancelReveal();

        HasRevealPlayed = false;

        SetImage(_cardBackgroundImage, null);
        SetImage(_potionImage, null);

        if (_nameText != null)
        {
            _nameText.text = string.Empty;
        }

        if (_descriptionText != null)
        {
            _descriptionText.text = string.Empty;
        }

        SetAngle(HiddenAngle);
    }

    public void SetRevealed(PotionData data, Sprite uiPotionSprite, Sprite cardBackgroundSprite)
    {
        CancelReveal();

        SetContent(data, uiPotionSprite, cardBackgroundSprite);

        HasRevealPlayed = true;
        SetAngle(RevealedAngle);
    }

    public void PlayRevealEffect(PotionData data, Sprite uiPotionSprite, Sprite cardBackgroundSprite)
    {
        if (data == null ||
            uiPotionSprite == null ||
            cardBackgroundSprite == null)
        {
            SetHidden();
            return;
        }

        CancelReveal();

        SetContent(data, uiPotionSprite, cardBackgroundSprite);

        SetAngle(HiddenAngle);

        if (_revealDuration <= 0f)
        {
            CompleteReveal();
            return;
        }

        _revealRoutine = StartCoroutine(RevealCoroutine());
    }

    public void CancelReveal()
    {
        if (_revealRoutine == null)
        {
            return;
        }

        StopCoroutine(_revealRoutine);
        _revealRoutine = null;
    }

    private IEnumerator RevealCoroutine()
    {
        float elapsed = 0f;
        while (elapsed <= _revealDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float normalizedTime = Mathf.Clamp01(elapsed / _revealDuration);
            float easedTime = _revealCurve.Evaluate(normalizedTime);
            float angle = Mathf.LerpUnclamped(HiddenAngle, RevealEndAngle, easedTime);

            SetAngle(angle);
            yield return null;
        }

        CompleteReveal();
    }

    private void CompleteReveal()
    {
        _revealRoutine = null;
        HasRevealPlayed = true;
    }

    private void SetContent(PotionData data, Sprite uiPotionSprite, Sprite cardBackgroundSprite)
    {
        SetImage(_cardBackgroundImage, cardBackgroundSprite);

        SetImage(_potionImage, uiPotionSprite);

        if (_nameText != null)
        {
            _nameText.text = data != null ? data.Name : string.Empty;
        }

        if (_descriptionText != null)
        {
            _descriptionText.text = data != null ? data.Description : string.Empty;
        }
    }

    private void SetAngle(float yAngle)
    {
        if (_flipRoot == null)
        {
            _flipRoot = transform as RectTransform;
        }

        if (_flipRoot != null)
        {
            _flipRoot.localRotation = Quaternion.Euler(0f, yAngle, 0f);
        }
    }

    private static void SetImage(Image image, Sprite sprite)
    {
        if (image == null)
        {
            return;
        }

        image.sprite = sprite;
        image.preserveAspect = true;
        image.color = sprite != null ? Color.white : Color.clear;
    }

    private void OnDisable()
    {
        if (_revealRoutine == null)
        {
            return;
        }

        CancelReveal();

        HasRevealPlayed = true;
        SetAngle(RevealedAngle);
    }
}
