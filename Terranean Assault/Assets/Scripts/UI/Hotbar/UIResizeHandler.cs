using UnityEngine;
using UnityEngine.EventSystems;
using TMPro; // Make sure you’re using TextMeshPro

public class UIResizeHandler : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    public RectTransform targetToResize; // The Scoreboard panel
    public Vector2 minSize = new Vector2(200, 200);
    public Vector2 maxSize = new Vector2(2000, 2000);

    private Vector2 originalSize;
    private Vector2 pointerStartLocalPosition;
    private float originalWidth;
    private TMP_Text[] allTexts;

    void Awake()
    {
        if (targetToResize == null)
            targetToResize = transform.parent.GetComponent<RectTransform>();

        // Cache all TextMeshProUGUI components inside the scoreboard
        allTexts = targetToResize.GetComponentsInChildren<TMP_Text>(true);
        originalWidth = targetToResize.sizeDelta.x;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        originalSize = targetToResize.sizeDelta;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(targetToResize, eventData.position, eventData.pressEventCamera, out pointerStartLocalPosition);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localPointerPosition;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(targetToResize, eventData.position, eventData.pressEventCamera, out localPointerPosition))
        {
            Vector2 sizeDelta = originalSize + new Vector2(
                pointerStartLocalPosition.x - localPointerPosition.x,
                localPointerPosition.y - pointerStartLocalPosition.y
            );

            sizeDelta = new Vector2(Mathf.Clamp(sizeDelta.x, minSize.x, maxSize.x),
                                    Mathf.Clamp(sizeDelta.y, minSize.y, maxSize.y));

            targetToResize.sizeDelta = sizeDelta;

            // Scale text size based on width ratio
            float scaleRatio = sizeDelta.x / originalWidth;
            foreach (TMP_Text text in allTexts)
            {
                text.fontSize = Mathf.Clamp(text.fontSize * scaleRatio, 8f, 200f);
            }

            // Update reference for continuous resizing
            originalWidth = sizeDelta.x;
        }
    }
}
