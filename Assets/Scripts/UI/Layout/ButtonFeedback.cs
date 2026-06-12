using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    private Vector3 originalScale;
    private RectTransform rectTransform;
    private const float HoverScale = 1.01f;
    private const float PressedScale = 0.98f;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData) => rectTransform.localScale = originalScale * HoverScale;
    public void OnPointerExit(PointerEventData eventData) => rectTransform.localScale = originalScale;
    public void OnPointerDown(PointerEventData eventData) => rectTransform.localScale = originalScale * PressedScale;
    public void OnPointerUp(PointerEventData eventData) => rectTransform.localScale = originalScale * (eventData.pointerEnter == gameObject ? HoverScale : 1f);

    private void OnDisable()
    {
        if (rectTransform != null) rectTransform.localScale = originalScale;
    }
}
