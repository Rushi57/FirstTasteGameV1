using System;
using System.Collections.Generic;
using UnityEngine;

public class CookingStepGate : MonoBehaviour
{
    public static CookingStepGate Instance { get; private set; }

    public WarningMessageUI warning;
    public string wrongStepMessage = "Wrong Step!\nMinus 1 heart";

    public event Action<int> OnStepCompleted;   // CookingPrepListUI listens: strike through this row
    public event Action<int> OnCurrentStepChanged;

    private List<string> stepActionIds = new List<string>();
    public int CurrentIndex { get; private set; }
    public bool AllDone => CurrentIndex >= stepActionIds.Count;

    private void Awake() => Instance = this;

    /// <summary>Call once when the Cooking Prep panel is filled, with one actionId per step, in order.</summary>
    public void BeginRecipe(List<string> actionIds)
    {
        stepActionIds = new List<string>(actionIds);
        CurrentIndex = 0;
        OnCurrentStepChanged?.Invoke(CurrentIndex);
    }

    public bool IsCurrent(string actionId) =>
        !AllDone && stepActionIds[CurrentIndex] == actionId;

    /// <summary>Call BEFORE the action starts (on drop / tap). If false, cancel the action.</summary>
    public bool TryBeginAction(string actionId)
    {
        if (IsCurrent(actionId)) return true;

        warning?.Show(wrongStepMessage);
        ScoreManager.Instance?.ReportWrongStep();
        return false;
    }

    /// <summary>Call when the action has truly finished (animation done, timer done).</summary>
    public void CompleteStep(string actionId)
    {
        if (!IsCurrent(actionId)) return;

        OnStepCompleted?.Invoke(CurrentIndex);
        CurrentIndex++;
        OnCurrentStepChanged?.Invoke(CurrentIndex);
    }
}