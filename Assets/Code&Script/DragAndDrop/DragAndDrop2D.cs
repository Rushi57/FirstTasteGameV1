using UnityEngine;
using UnityEngine.EventSystems;

public class Drag2DObject : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Settings")]
    [Tooltip("If true, snaps to UI Canvas space. If false, handles 2D World space.")]
    [SerializeField] private bool isUIObject = true;
    [SerializeField] private CanvasGroup canvasGroup;

    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private Vector3 offset;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        // Find parent Canvas for UI elements
        if (isUIObject)
        {
            parentCanvas = GetComponentInParent<Canvas>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Calculate offset so the object doesn't snap its origin to the cursor
        if (isUIObject)
        {
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                rectTransform, eventData.position, eventData.pressEventCamera, out Vector3 worldPoint);
            offset = rectTransform.position - worldPoint;
        }
        else
        {
            Vector3 mouseWorldPos = GetMouseWorldPosition(eventData);
            offset = transform.position - mouseWorldPos;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Make the image slightly transparent and let raycasts pass through during drag (optional)
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.6f;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isUIObject)
        {
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                parentCanvas.transform as RectTransform, eventData.position, eventData.pressEventCamera, out Vector3 worldPoint))
            {
                rectTransform.position = worldPoint + offset;
            }
        }
        else
        {
            transform.position = GetMouseWorldPosition(eventData) + offset;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Reset appearance and raycasts
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1.0f;
            canvasGroup.blocksRaycasts = true;
        }
    }

    private Vector3 GetMouseWorldPosition(PointerEventData eventData)
    {
        Camera mainCamera = eventData.pressEventCamera != null ? eventData.pressEventCamera : Camera.main;
        Vector3 screenPos = new Vector3(eventData.position.x, eventData.position.y, -mainCamera.transform.position.z);
        return mainCamera.ScreenToWorldPoint(screenPos);
    }
}