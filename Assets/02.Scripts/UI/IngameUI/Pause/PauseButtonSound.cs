using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// PausePresenter가 일시정지 메뉴의 버튼에만 연결한다.
[RequireComponent(typeof(Button))]
[DisallowMultipleComponent]
public class PauseButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler
{
    private Button _button;
    private bool _pointerInside;

    public event Action OnFocused;

    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _pointerInside = true;
        NotifyFocused();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _pointerInside = false;
    }

    public void OnSelect(BaseEventData eventData)
    {
        // 마우스로 이미 가리키는 버튼을 클릭해서 선택할 때는 중복 재생하지 않는다.
        if (!_pointerInside) NotifyFocused();
    }

    private void OnDisable()
    {
        _pointerInside = false;
    }

    private void NotifyFocused()
    {
        if (!isActiveAndEnabled || !_button.IsActive() || !_button.IsInteractable()) return;

        OnFocused?.Invoke();
    }
}
