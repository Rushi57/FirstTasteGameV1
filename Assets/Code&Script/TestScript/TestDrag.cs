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

    /// <summary>
    /// Optional gate. If set and it returns false, the drag is refused
    /// (the item simply stays where it is).
    /// </summary>
    public System.Func<bool> CanBeginDrag;

    /// <summary>Set by a drop zone for items that sit in it: releasing them anywhere else
    /// returns them without the tutorial "bad drop" mistake.</summary>
    public bool suppressBadDropMistake;

    /// <summary>Called when a drag ends without a successful drop (item already returned).</summary>
    public System.Action OnBadDrop;

    /// <summary>The parent this item had when the current/last drag started.</summary>
    public Transform OriginalParent => originalParent;


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
        if (CanBeginDrag != null && !CanBeginDrag()) return;   // wrong order: stays put
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

            OnBadDrop?.Invoke();

            if (reportMistakeOnBadDrop && !suppressBadDropMistake && !isLocked)
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