using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class QuitConfirmView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("Buttons")]
    [SerializeField] private Button _quitButton;
    [SerializeField] private Button _cancelButton;
    [SerializeField] private Button _closeButton;

    public event Action OnQuitRequested;
    public event Action OnCancelRequested;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        Hide();
    }

    private void OnEnable()
    {
        _quitButton.onClick.AddListener(HandleQuitClicked);
        _cancelButton.onClick.AddListener(HandleCancelClicked);
        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(HandleCancelClicked);
        }
    }

    private void OnDisable()
    {
        _quitButton.onClick.RemoveListener(HandleQuitClicked);
        _cancelButton.onClick.RemoveListener(HandleCancelClicked);
        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveListener(HandleCancelClicked);
        }
    }

    public void Show()
    {
        IsOpen = true;
        _canvasGroup.alpha = 1f;
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;
        _cancelButton.Select();
    }

    public void Hide()
    {
        IsOpen = false;
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
        ClearSelection();
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

    private void HandleQuitClicked()
    {
        // 실행 중인 전투를 포기하는 요청이다. 실제 씬 이동은 Presenter가 처리한다.
        OnQuitRequested?.Invoke();
    }

    private void HandleCancelClicked()
    {
        OnCancelRequested?.Invoke();
    }
}
