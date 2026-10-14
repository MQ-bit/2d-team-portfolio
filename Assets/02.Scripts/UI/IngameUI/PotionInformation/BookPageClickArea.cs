using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// A page button only accepts clicks where no other interactive UI is hit.
public sealed class BookPageClickArea : Button
{
    private readonly List<RaycastResult> _hits = new();

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left ||
            !IsActive() || !IsInteractable() || HasOtherClickTarget(eventData))
        {
            return;
        }

        base.OnPointerClick(eventData);
    }

    private bool HasOtherClickTarget(PointerEventData eventData)
    {
        if (EventSystem.current == null) return false;

        _hits.Clear();
        EventSystem.current.RaycastAll(eventData, _hits);
        foreach (RaycastResult hit in _hits)
        {
            if (hit.module is not GraphicRaycaster) continue;

            GameObject target = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit.gameObject);
            if (target == null || target.GetComponent<BookPageClickArea>() != null) continue;

            Selectable selectable = target.GetComponent<Selectable>();
            if (selectable != null && !selectable.IsInteractable()) continue;

            return true;
        }
        return false;
    }
}
