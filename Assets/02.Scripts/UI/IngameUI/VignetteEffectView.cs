using UnityEngine;

public class VignetteEffectView : MonoBehaviour
{
    [SerializeField] private GameObject _vignetteEffect;

    public void Initialize()
    {
        Hide();
    }

    public void Show()
    {
        if (_vignetteEffect != null)
        {
            _vignetteEffect.SetActive(true);
        }
    }

    public void Hide()
    {
        if (_vignetteEffect != null)
        {
            _vignetteEffect.SetActive(false);
        }
    }

    public void SetVisible(bool isVisible)
    {
        if (isVisible)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }

    private void OnDisable()
    {
        Hide();
    }
}
