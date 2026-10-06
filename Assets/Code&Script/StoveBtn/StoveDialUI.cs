using UnityEngine;
using UnityEngine.EventSystems;

/// Put this on DialImage (the rotating knob). Drag to rotate, snaps to Off/Low/Medium/High.
[RequireComponent(typeof(RectTransform))]
public class StoveDialUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Tooltip("The StoveDropZone's StoveHeatController.")]
    public StoveHeatController stove;

    [Tooltip("Degrees to add if your knob art doesn't point straight up at rotation 0.")]
    public float angleOffset = 0f;

    [SerializeField] private TutorialInteractable tutorialInteractable;

    private RectTransform knob;
    private Canvas canvas;
    private float currentAngle;   // clockwise from top, 0-360

    private void Awake()
    {
        knob = (RectTransform)transform;
        canvas = GetComponentInParent<Canvas>();
    }

    /// Called by the stove so the knob matches the current heat.
    public void SetVisual(StoveHeat heat)
    {
        if (knob == null) knob = (RectTransform)transform;
        currentAngle = (int)heat * 90f;
        ApplyRotation(currentAngle);
    }

    public void OnBeginDrag(PointerEventData e) => UpdateAngle(e);
    public void OnDrag(PointerEventData e) => UpdateAngle(e);

    public void OnEndDrag(PointerEventData e)
    {
        int step = Mathf.RoundToInt(currentAngle / 90f) % 4;   // 0..3 = Off, Low, Medium, High
        currentAngle = step * 90f;
        ApplyRotation(currentAngle);

        if (stove != null)
            stove.SetHeat((StoveHeat)step);
    }

    private void UpdateAngle(PointerEventData e)
    {
        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? canvas.worldCamera : null;

        Vector2 center = RectTransformUtility.WorldToScreenPoint(cam, knob.position);
        Vector2 dir = e.position - center;
        if (dir.sqrMagnitude < 4f) return;   // finger on the exact center, ignore

        float angle = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;   // 0 = up, clockwise positive
        if (angle < 0f) angle += 360f;

        currentAngle = angle;
        ApplyRotation(currentAngle);
    }

    private void ApplyRotation(float clockwiseAngle)
    {
        knob.localRotation = Quaternion.Euler(0f, 0f, -(clockwiseAngle + angleOffset));
    }

}