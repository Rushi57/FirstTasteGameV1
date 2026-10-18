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

    private RectTransform knob;
    private Canvas canvas;
    private float currentAngle;   // clockwise from top, 0-360

    private float lastRawAngle;


    private bool isDragging;
    private float lastMoveTime;

    [Tooltip("Seconds without movement before the tick stops (while still holding the knob).")]
    public float stopDelay = 0.12f;

    public bool locked;
    private StoveHeat lockedHeat;

    public void LockAt(StoveHeat heat) { locked = true; lockedHeat = heat; }

    public void Unlock() { locked = false; }

    private TutorialInteractable[] interactables;

    private void Awake()
    {
        knob = (RectTransform)transform;
        canvas = GetComponentInParent<Canvas>();
        interactables = GetComponents<TutorialInteractable>();
    }

    /// Called by the stove so the knob matches the current heat.
    public void SetVisual(StoveHeat heat)
    {
        if (knob == null) knob = (RectTransform)transform;
        currentAngle = (int)heat * 90f;
        ApplyRotation(currentAngle);
    }

    public void OnBeginDrag(PointerEventData e)
    {
        lastRawAngle = currentAngle;
        lastRawAngle = currentAngle;
        UpdateAngle(e);
    }
    public void OnDrag(PointerEventData e) => UpdateAngle(e);

    public void OnEndDrag(PointerEventData e)
    {
        isDragging = false;
        SFXManager.Instance?.StopTick();

        int step = Mathf.Clamp(Mathf.RoundToInt(currentAngle / 90f), 0, 3);
        StoveHeat heat = (StoveHeat)step;

        // Heat step already completed: reject any change, snap back, cost a heart.
        if (stove != null && stove.IsHeatLocked && heat != stove.CurrentHeat)
        {
            bool inTutorial = TutorialManager.Instance != null && TutorialManager.Instance.IsActive;
            if (!inTutorial)
                ScoreManager.Instance?.ReportMistake("Don't change the heat!\nMinus 1 heart");

            SetVisual(stove.CurrentHeat);   // knob back to the real heat
            lastRawAngle = currentAngle;
            return;                         // SetHeat is never called
        }

        currentAngle = step * 90f;
        lastRawAngle = currentAngle;
        ApplyRotation(currentAngle);

        StoveHeat previous = stove != null ? stove.CurrentHeat : StoveHeat.Off;
        if (stove != null) stove.SetHeat(heat);

        bool tutorialRunning = TutorialManager.Instance != null && TutorialManager.Instance.IsActive;
        if (tutorialRunning && heat != previous)
            ReportToTutorial(heat);
    }

    private void UpdateAngle(PointerEventData e)
    {
        float previousAngle = currentAngle;

        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? canvas.worldCamera : null;

        Vector2 center = RectTransformUtility.WorldToScreenPoint(cam, knob.position);
        Vector2 dir = e.position - center;
        if (dir.sqrMagnitude < 4f) return;   // finger on the exact center, ignore

        float angle = Mathf.Atan2(-dir.x, dir.y) * Mathf.Rad2Deg;   // 0 = up, clockwise positive
        if (angle < 0f) angle += 360f;


        //Hard Stop
        if (lastRawAngle > 180f && angle < 90f)       // clockwise past High
            angle = 270f;
        else if (lastRawAngle < 90f && angle > 270f)  // counter-clockwise past Off
            angle = 0f;

        lastRawAngle = angle;
        currentAngle = angle;
        ApplyRotation(currentAngle);

        UpdateTickSound(previousAngle);

    }

    private void ApplyRotation(float counterClockwiseAngle)
    {
        knob.localRotation = Quaternion.Euler(0f, 0f, counterClockwiseAngle + angleOffset);
    }
    private void ReportToTutorial(StoveHeat heat)
    {
        if (heat == StoveHeat.Off) return;

        string wantedId = "Stove" + heat;   // StoveLow, StoveHigh, ...

        foreach (var t in interactables)
        {
            if (t.ResolvedId == wantedId)
            {
                if (t.ResolvedId == wantedId) { t.ReportDrop(); return; }
            }
        }

        if (stove != null) stove.SetHeat(StoveHeat.Off);
        SetVisual(StoveHeat.Off);   // snap the knob back to Off
        TutorialManager.Instance?.ReportMistake();
    }

    private void UpdateTickSound(float previousAngle)
    {
        if (SFXManager.Instance == null) return;

        // Only play if the knob actually moved
        if (Mathf.Abs(currentAngle - previousAngle) > 0.5f)
        {
            lastMoveTime = Time.unscaledTime;
            SFXManager.Instance.StartTick();
        }
    }
    private void OnDisable()
    {
        isDragging = false;
        SFXManager.Instance?.StopTick();
    }

    private void Update()
    {
        if (isDragging && Time.unscaledDeltaTime - lastMoveTime > stopDelay)
            SFXManager.Instance?.StopTick();
    }
}