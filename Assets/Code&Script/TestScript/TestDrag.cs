using UnityEngine;
using UnityEngine.EventSystems;

public class TestDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Tooltip("Identifier for this item, checked by TestDrop to see if it's the correct item for that zone.")]
    public string itemId;

    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private Vector2 originalAnchoredPosition;
    private Transform originalParent;
    private bool wasDroppedSuccessfully;
    private bool isLocked;
    private bool isDragging;
    private bool dragEnabled = true;
    public void SetDragEnabled(bool enabled) => dragEnabled = enabled;


    [Header("Tutorial")]
    [Tooltip("If true, releasing this item anywhere except its correct drop zone counts as a tutorial mistake.")]
    public bool reportMistakeOnBadDrop = true;
    public bool IsLocked => isLocked;

    public void MarkDropHandled() => wasDroppedSuccessfully = true;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        canvas = GetComponentInParent<Canvas>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isLocked || isDragging || !dragEnabled) return;
        if (isLocked || isDragging) return;  
        isDragging = true;

        wasDroppedSuccessfully = false;
        originalAnchoredPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;

        transform.SetParent(canvas.transform, true);
        transform.SetAsLastSibling();

       
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isLocked || !isDragging) return;
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;  
        isDragging = false;

        canvasGroup.blocksRaycasts = !isLocked;

        if (!wasDroppedSuccessfully)
        {
            ReturnToOrigin();   

            if (reportMistakeOnBadDrop && !isLocked)
                TutorialManager.Instance?.ReportMistake();
        }
    }

    public void ReturnToOrigin()
    {
        transform.SetParent(originalParent, true);
        rectTransform.anchoredPosition = originalAnchoredPosition;
    }

    public void SnapTo(RectTransform target)
    {
        wasDroppedSuccessfully = true;
        transform.SetParent(target, true);
        rectTransform.anchoredPosition = Vector2.zero;
    }

    public void LockAndReturn()
    {
        ReturnToOrigin();
        wasDroppedSuccessfully = true;
        SetLocked(true);
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;
        canvasGroup.alpha = locked ? 0f : 1f;
        canvasGroup.blocksRaycasts = !locked;
        canvasGroup.interactable = !locked;
    }
    public void OnPointerClick(PointerEventData eventData) { }

    private void OnDisable()
    {
        isDragging = false;
        if (canvasGroup != null) canvasGroup.blocksRaycasts = !isLocked;
    }
}