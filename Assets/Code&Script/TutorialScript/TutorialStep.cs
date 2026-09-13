using UnityEngine;

public enum GameMechanicType
{
    DialogOnly,
    MixingSpatula,
    SimmerAndBoil
}
[System.Serializable]
public class TutorialStep
{
    [TextArea(2, 4)]
    public string dialogText;
    public GameMechanicType requiredMechanic;


    [Header("Validation Rules")]
    public int targetStirsCount = 3;
    public bool requiredGreenHit = true;
    public string errorMEssageOnFailure = "Oops! Try again and follow the prompt carefully.";
}

