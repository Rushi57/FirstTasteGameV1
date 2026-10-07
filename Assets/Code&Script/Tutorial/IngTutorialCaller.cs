using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class IngInteractable : MonoBehaviour
{
    public List<Button> buttons;   // drag all your buttons here

    [Header("Hold buttons (reports on click)")]
    public List<Button> holdButtons;

    [Tooltip("The player must hold at least this long before releasing for it to count. 0 = any press and release counts.")]
    public float minHoldSeconds = 0.5f;

    void Awake()
    {
        //--Tap Button--
        foreach (var btn in buttons)
        {
            if (btn == null) continue;

            var interactable = btn.GetComponent<TutorialInteractable>();
            if (interactable == null) continue;   // no interactable = not reported

            btn.onClick.AddListener(interactable.ReportTap);
        }

        //--Hold Button
        foreach (var btn in holdButtons)
        {
            if (btn == null) continue;

            var interactable = btn.GetComponent<TutorialInteractable>();
            if (interactable == null) continue;

            SetupHold(btn.gameObject, interactable);
        }

    }

    private void SetupHold(GameObject target, TutorialInteractable interactable)
    {
        var trigger = target.GetComponent<EventTrigger>();
        if (trigger == null) trigger = target.AddComponent<EventTrigger>();

        bool isHeld = false;
        float pressStart = 0f;

        //Finger/Mouse goes down 
        var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        down.callback.AddListener(_ =>
        {
            isHeld = true;
            pressStart = Time.unscaledTime;
        });
        trigger.triggers.Add(down);

        //Finger/mouse released
        var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        up.callback.AddListener(_ =>
        {
            if (!isHeld) return;
            isHeld = false;

            if (Time.unscaledTime - pressStart >= minHoldSeconds)
                interactable.ReportHold();
        });
        trigger.triggers.Add(up);
    }
}