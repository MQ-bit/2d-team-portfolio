using System;
using UnityEngine;
using UnityEngine.UI;

public class PauseView : MonoBehaviour
{

    [Header("References")]
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("Buttons")]
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _mainMenuButton;


    public event Action OnResumeRequested;
    public event Action OnMainMenuRequested;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        Hide();

    }

    private void OnEnable()
    {
        _resumeButton.onClick.AddListener(HandleResumeClicked);
        _mainMenuButton.onClick.AddListener(HandleMainMenuClicked);
    }

    private void OnDisable()
    {
        _resumeButton.onClick.RemoveListener(HandleResumeClicked);
        _mainMenuButton.onClick.RemoveListener(HandleMainMenuClicked);
    }


    public void Show()
    {
        IsOpen = true;
        _canvasGroup.alpha = 1f;
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;
    }

    public void Hide()
    {
        IsOpen = false;
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
    }


    private void HandleResumeClicked()
    {
        OnResumeRequested?.Invoke();
    }

    private void HandleMainMenuClicked()
    {
        OnMainMenuRequested?.Invoke();
    }


}
