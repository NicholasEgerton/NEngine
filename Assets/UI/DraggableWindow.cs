using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableWindow : MonoBehaviour, IDragHandler
{
    public Canvas canvas;

    public RectTransform renderTransform;

    void Start()
    {
        renderTransform = GetComponent<RectTransform>();
    }

    void IDragHandler.OnDrag(PointerEventData eventData)
    {
        Vector2 offset = eventData.delta / canvas.scaleFactor;

        renderTransform.anchoredPosition += offset;
    }
}
