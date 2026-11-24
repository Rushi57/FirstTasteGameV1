using UnityEngine;
using UnityEngine.EventSystems;

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
        if (spoon == null || drag == null) return; 

        controller?.HandleSpoonDropped(spoon, drag, dropped);
        tutorialTag?.ReportDrop();

        SFXManager.Instance?.PlayJarScoop();
    }
}