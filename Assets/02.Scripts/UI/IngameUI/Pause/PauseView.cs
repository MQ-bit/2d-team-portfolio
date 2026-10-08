using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PauseView : MonoBehaviour
{

    [Header("References")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private PauseVolumeView _volumeView;

    [Header("Buttons")]
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _mainMenuButton;
    [SerializeField] private Button _closeButton;


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
        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(HandleResumeClicked);
        }
    }

    private void OnDisable()
    {
        _resumeButton.onClick.RemoveListener(HandleResumeClicked);
        _mainMenuButton.onClick.RemoveListener(HandleMainMenuClicked);
        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveListener(HandleResumeClicked);
        }
    }


    public void Show()
    {
        IsOpen = true;
        _canvasGroup.alpha = 1f;
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;
        _resumeButton.Select();
    }

    public void Hide()
    {
        IsOpen = false;
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
        ClearSelection();
    }

    public void SetButtonsInteractable(bool interactable)
    {
        if (_volumeView != null)
        {
            _volumeView.SetInteractable(interactable);
        }
        _resumeButton.interactable = interactable;
        _mainMenuButton.interactable = interactable;
        if (_closeButton != null)
        {
            _closeButton.interactable = interactable;
        }
    }

    public void SelectMainMenuButton()
    {
        _mainMenuButton.Select();
    }

    private void ClearSelection()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null) return;

        GameObject selected = eventSystem.currentSelectedGameObject;
        if (selected != null && selected.transform.IsChildOf(transform))
        {
            eventSystem.SetSelectedGameObject(null);
        }
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
