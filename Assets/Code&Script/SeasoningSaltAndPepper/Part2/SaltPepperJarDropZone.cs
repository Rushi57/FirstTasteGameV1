using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Put this on the JarImageSalt/Pepper object. Needs Raycast Target ON.
/// Forwards any dropped spoon to SaltPepperController for scoring.
/// </summary>
public class SaltPepperJarDropZone : MonoBehaviour, IDropHandler
{
    public SaltPepperController controller;
    private TutorialInteractable tutorialTag;

    private void Awake()
    {
        tutorialTag = GetComponent<TutorialInteractable>();
    }
    public void OnDrop(PointerEventData eventData)
    {
        GameObject dropped = eventData.pointerDrag;
        if (dropped == null) return;

        var spoon = dropped.GetComponent<SaltPepperSpoonItem>();
        var drag = dropped.GetComponent<TestDrag>();
        if (spoon == null || drag == null) return; // not a spoon - bounces back on its own

        controller?.HandleSpoonDropped(spoon, drag, dropped);
        tutorialTag?.ReportDrop();
    }
}