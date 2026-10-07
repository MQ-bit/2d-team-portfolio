using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CooltimeCounterView : MonoBehaviour
{
    [SerializeField] private Image _shadowPanel;
    [SerializeField] private TMP_Text _cooltimeText;

    private Coroutine _cooltimeCoroutine;

    private void Awake()
    {
        ResetStatus();
    }

    public void StartCount(float duration)
    {
        if (_shadowPanel == null || _cooltimeText == null)
        {
            Debug.LogWarning($"{gameObject.name}: elements are not fully assigned.");
            return;
        }

        StopCoolTimeEffect();

        if (duration <= 0f)
        {
            return;
        }

        _cooltimeCoroutine = StartCoroutine(CoolTimeCoroutine(duration));
    }

    public void StopCoolTimeEffect()
    {
        if (_cooltimeCoroutine != null)
        {
            StopCoroutine(_cooltimeCoroutine);
            _cooltimeCoroutine = null;
        }

        ResetStatus();
    }

    private IEnumerator CoolTimeCoroutine(float duration)
    {
        float remainingTime = duration;

        _shadowPanel.fillAmount = 1f;
        _cooltimeText.gameObject.SetActive(true);

        while (remainingTime > 0f)
        {
            _shadowPanel.fillAmount = Mathf.Clamp01(remainingTime / duration);

            _cooltimeText.text = remainingTime.ToString("F2");

            yield return null;

            remainingTime -= Time.deltaTime;
        }

        _cooltimeCoroutine = null;
        ResetStatus();
    }

    private void ResetStatus()
    {
        if (_shadowPanel != null)
        {
            _shadowPanel.fillAmount = 0f;
        }

        if (_cooltimeText != null)
        {
            _cooltimeText.text = string.Empty;
            _cooltimeText.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        StopCoolTimeEffect();
    }
}