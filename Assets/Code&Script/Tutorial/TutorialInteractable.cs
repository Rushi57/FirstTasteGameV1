using UnityEngine;
using UnityEngine.EventSystems;

public class TutorialInteractable : MonoBehaviour
{
    [Tooltip("Used if Source Data is not assigned. Must match TutorialStep.")]
    public string interactableId;

    [Tooltip("IngredientData/UtensilData.")]
    public ScriptableObject sourceData;

   
    public string ResolvedId => ((sourceData is ITutorialIdentifiable identifiable) ? identifiable.TutorialId : interactableId)?.Trim();

    public RectTransform RectTransform => transform as RectTransform;

    private void OnEnable()
    {
        TutorialManager.Instance?.Register(this);
    }

    private void OnDisable()
    {
        TutorialManager.Instance?.Unregister(this);
    }

    public void ReportTap()
    {
        TutorialManager.Instance?.NotifyAction(ResolvedId, TutorialActionType.Tap);
    }
    public void ReportDrop()
    {
        TutorialManager.Instance?.NotifyAction(ResolvedId, TutorialActionType.Drag);
    }
    public void ReportScroll()
    {
        TutorialManager.Instance?.NotifyAction(ResolvedId, TutorialActionType.Scroll);
    }
    public void ReportTimerComplete()
    {
        TutorialManager.Instance?.NotifyAction(ResolvedId, TutorialActionType.TimerComplete);
    }
    public void ReportHold()
    {
        TutorialManager.Instance?.NotifyAction(ResolvedId, TutorialActionType.Hold);
    }
    public void ReportTripleTap()
    {
        TutorialManager.Instance?.NotifyAction(ResolvedId, TutorialActionType.TripleTap);
    }
    public void ReportMistake()
    {
        TutorialManager.Instance?.ReportMistake();
    }
    public bool TryUse(TutorialActionType type, bool reportMistakeIfBlocked = true)
    {
        var m = TutorialManager.Instance;
        if (m == null || !m.IsActive) return true;
        if (m.IsExpected(ResolvedId, type)) return true;
        if (reportMistakeIfBlocked) m.ReportMistake();
        return false;
    }
}

public interface ITutorialIdentifiable
{
    string TutorialId { get; }
}
