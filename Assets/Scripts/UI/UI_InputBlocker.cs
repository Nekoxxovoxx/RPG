using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public static class UI_InputBlocker
{
    private static readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

    public static bool ShouldBlockPlayerMouseInput()
    {
        if (UI_NpcInteractionMenu.IsOpen || UI_DialogueConversationSystem.IsOpen)
            return true;

        EventSystem eventSystem = EventSystem.current;

        if (eventSystem == null)
            return false;

        if (eventSystem.IsPointerOverGameObject())
            return true;

        PointerEventData pointerData = new PointerEventData(eventSystem)
        {
            position = Input.mousePosition
        };

        raycastResults.Clear();
        eventSystem.RaycastAll(pointerData, raycastResults);
        return raycastResults.Count > 0;
    }
}
