using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    private Vector3 originalScale;
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData) => rectTransform.localScale = originalScale * 1.02f;
    public void OnPointerExit(PointerEventData eventData) => rectTransform.localScale = originalScale;
    public void OnPointerDown(PointerEventData eventData) => rectTransform.localScale = originalScale * 0.94f;
    public void OnPointerUp(PointerEventData eventData) => rectTransform.localScale = originalScale * (eventData.pointerEnter == gameObject ? 1.02f : 1f);

    private void OnDisable()
    {
        if (rectTransform != null) rectTransform.localScale = originalScale;
    }
}
