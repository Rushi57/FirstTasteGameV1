using UnityEngine;

public class PanelOpenButton : MonoBehaviour
{
    [SerializeField] private GameObject targetPanel;
    [ Tooltip("Optional. Assign only on buttons that should be locked by the tutorial (e.g. the fridge X). " +
             "Give it an id matching the tutorial step that expects this close.")]
    [SerializeField] private TutorialInteractable tutorialInteractable;
    public void Open() => PanelExclusivityManager.Instance.TryOpen(targetPanel);
    public void Close()
    {
        {
            if (tutorialInteractable != null)
            {
               
                if (!tutorialInteractable.TryUse(TutorialActionType.Tap, false)) return;
                tutorialInteractable.ReportTap();
            }

            PanelExclusivityManager.Instance.Close(targetPanel);
        }
    }
}