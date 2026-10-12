using UnityEngine;


public enum CookingPhase { GatherIngredients, IngredientPrep, CookingPrep}
public class CookingPhaseGate : MonoBehaviour
{
    public static CookingPhaseGate Instance { get; private set; }

    [Header("Message")]
    public string gatherFirstMessage = "Gather Ingredient first";
    public string finishPrepFirstMessage = "Finish the Gather Ingredient or Ingredient Prep First";

    [Tooltip("The tutorial controls the steps itself, so skip the gate while it runs.")]
    public bool bypassDuringTutorial = true;

    public CookingPhase Current { get; private set; } = CookingPhase.GatherIngredients;

    private void Awake()
    {
        if(Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void SetPhase(CookingPhase phase)
    {
        Current = phase;
        Debug.Log($"[PhaseGate] Phase -> {phase}");
    }

    private bool TutorialBypass =>
        bypassDuringTutorial && TutorialManager.Instance != null && TutorialManager.Instance.IsActive;


    public bool CanCut()
    {
        if (TutorialBypass || Current >= CookingPhase.IngredientPrep) return true;
        WarningMessageUI.Instance?.Show(gatherFirstMessage);
        return false;
    }

    public bool CanUseStove()
    {
        if(TutorialBypass || Current >= CookingPhase.CookingPrep) return true;
        WarningMessageUI.Instance?.Show(finishPrepFirstMessage);
        return false;
    }

}
