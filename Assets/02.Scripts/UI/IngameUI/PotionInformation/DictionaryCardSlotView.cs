using System.Collections;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DictionaryCardSlotView : MonoBehaviour, IPointerClickHandler
{
    private const float HiddenAngle = 180f;
    private const float RevealedAngle = 0f;
    private const float RevealEndAngle = 360f;

    [Header("Flip Animation Setting")]
    [SerializeField] private RectTransform _flipRoot;
    [SerializeField] private Image _cardBackImage;
    [SerializeField, Min(0f)] private float _revealDuration = 0.5f;
    [SerializeField] private AnimationCurve _revealCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Front Info")]
    [SerializeField] private Image _cardBackgroundImage;
    [SerializeField] private Image _potionImage;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _descriptionText;

    private Coroutine _revealRoutine;
    private bool _revealPending;
    private Outline _selectionOutline;
    public PotionData Potion { get; private set; }
    public Sprite PotionIcon => _potionImage != null ? _potionImage.sprite : null;
    public event Action<DictionaryCardSlotView> OnCardClicked;
    public bool HasRevealPlayed { get; private set; }
    public bool IsRevealing => _revealPending || _revealRoutine != null;

    private void OnEnable()
    {
        if (!_revealPending) return;
        _revealPending = false;
        _revealRoutine = StartCoroutine(RevealCoroutine());
    }

    public void SetHidden()
    {
        Potion = null;
        SetSelected(false);
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

        if (!isActiveAndEnabled)
        {
            // 조합표 탭 등으로 숨겨진 카드의 공개 연출은 활성화될 때 시작한다.
            _revealPending = true;
            return;
        }

        _revealRoutine = StartCoroutine(RevealCoroutine());
    }

    public void CancelReveal()
    {
        _revealPending = false;
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
        _revealPending = false;
        HasRevealPlayed = true;
        SetAngle(RevealedAngle);
    }

    private void SetContent(PotionData data, Sprite uiPotionSprite, Sprite cardBackgroundSprite)
    {
        Potion = data;
        SetImage(_cardBackgroundImage, cardBackgroundSprite);
        if (_cardBackgroundImage != null)
        {
            _cardBackgroundImage.preserveAspect = false;
        }

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

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || Potion == null || IsRevealing) return;
        OnCardClicked?.Invoke(this);
    }

    public void SetSelected(bool selected)
    {
        if (_selectionOutline == null && selected && _cardBackgroundImage != null)
        {
            _selectionOutline = _cardBackgroundImage.gameObject.AddComponent<Outline>();
            _selectionOutline.effectColor = new Color(1f, 0.8f, 0.25f, 1f);
            _selectionOutline.effectDistance = new Vector2(4f, -4f);
        }
        if (_selectionOutline != null) _selectionOutline.enabled = selected;
    }

    private void SetAngle(float yAngle)
    {
        if (_flipRoot == null)
        {
            _flipRoot = transform as RectTransform;
        }

        if (_flipRoot != null)
        {
            if (_cardBackImage == null)
            {
                Transform back = _flipRoot.Find("CardBack");
                if (back != null) _cardBackImage = back.GetComponent<Image>();
            }

            if (_cardBackImage != null && _cardBackgroundImage != null)
            {
                RectTransform front = _cardBackgroundImage.rectTransform;
                RectTransform back = _cardBackImage.rectTransform;
                back.anchorMin = front.anchorMin;
                back.anchorMax = front.anchorMax;
                back.pivot = front.pivot;
                back.sizeDelta = front.sizeDelta;
                back.anchoredPosition3D = front.anchoredPosition3D;
                back.localRotation = front.localRotation;
                back.localScale = front.localScale;

                // Both faces use UI rendering; show the back only on its half of the flip.
                _cardBackImage.material = _cardBackgroundImage.material;
                _cardBackImage.preserveAspect = false;
                _cardBackImage.enabled = Mathf.Cos(yAngle * Mathf.Deg2Rad) < 0f;
            }

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
