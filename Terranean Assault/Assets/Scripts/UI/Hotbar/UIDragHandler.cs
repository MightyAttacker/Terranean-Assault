using UnityEngine;
using UnityEngine.EventSystems;

public class UIDragHandler : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    //Author - Lachlan Klenk
    private Vector2 offset;

    public void OnPointerDown(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            transform as RectTransform, 
            eventData.position, 
            eventData.pressEventCamera, 
            out offset);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localPointerPosition;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            transform.parent as RectTransform, 
            eventData.position, 
            eventData.pressEventCamera, 
            out localPointerPosition))
        {
            (transform as RectTransform).localPosition = localPointerPosition - offset;
        }
    }
}
