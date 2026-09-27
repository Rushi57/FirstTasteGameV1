using UnityEngine;
using UnityEngine.UI;
public class TutorialTag : MonoBehaviour
{
    [Tooltip("Optional ")]
    public TutorialInteractable tutorialTagTapToCut;
    public TutorialInteractable tutorialTagTapToCutAgain;
    public TutorialInteractable tutorialTagTapToPour;
    public TutorialInteractable tutorialTagTapToStop;
    public TutorialInteractable tutorialTagTapToCloseMiniGame;
    public TutorialInteractable tutorialTagTapToSpawnIngredient;

    public void TapCutBtn()
    {
        tutorialTagTapToCut.ReportTap();
        Debug.Log("Cut Button is Tap!!!!");
    }
    public void TapCutBtnAgain()
    {
        tutorialTagTapToCut.ReportTap();
        Debug.Log("Cut Again Button is Tap!!!!");
    }

    public void TapToPour()
    {
        tutorialTagTapToPour.ReportTap();
        Debug.Log("Tap to Pour!!!!");
    }

    public void TapStopSimmerBtn()
    {
        tutorialTagTapToPour?.ReportTap();
        Debug.Log("Tap Simmer Button !!!!");
    }

    public void TapToCloseMinigameBtn()
    {
        tutorialTagTapToCloseMiniGame?.ReportTap();
        Debug.Log("CloseButton is Tap");
    }

    public void TapToSpawnIngedient()
    {
        tutorialTagTapToSpawnIngredient?.ReportTap();
        Debug.Log("Ingedinet is Tap");
    }
}
