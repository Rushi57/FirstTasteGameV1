using UnityEngine;
using System.Collections;
public class IngInteractable : MonoBehaviour
{
    public TutorialInteractable ingGameTag;
    
    public void InGameBtn()
    {
        ingGameTag?.ReportTap();
        Debug.Log("Ingredient Tap");
    }

}
