using UnityEngine;
using UnityEngine.UI;

public class TestBtn : MonoBehaviour
{

    [Tooltip("Optional ")]
    public TutorialInteractable tutorialTag1;
    public TutorialInteractable tutorialTag2;

    
    public void TapBnt1()
    {
        tutorialTag1?.ReportTap();
        Debug.Log("Button Test1 is tap!!!!");
    }

    public void TapBtn2()
    {
        tutorialTag2?.ReportTap();
        Debug.Log("Button Test2 is tap!!!!");
    }

}
