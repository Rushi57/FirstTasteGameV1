using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class SpoonDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Spoon Data")]
    public float spoonValue = 1f;
    public TextMeshProUGUI valueText;

    private Transform slotContainerParent;
    private Vector3 originalLocalPosition;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Canvas mainCanvas;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        mainCanvas = GetComponentInParent<Canvas>();
    }

    public void SetupSpoon(float value, string label)
    {
        spoonValue = value;
        if (valueText != null)
        {
            valueText.text = label;
            valueText.raycastTarget = false; // Prevents text from blocking mouse inputs
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Store slot container parent and local placement
        slotContainerParent = transform.parent;
        originalLocalPosition = rectTransform.localPosition;

        // Move to root Canvas so it renders above everything while dragging
        transform.SetParent(mainCanvas.transform, true);

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.6f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / mainCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        // Return back inside its original slot container
        transform.SetParent(slotContainerParent, true);
        rectTransform.localPosition = originalLocalPosition;
    }
}