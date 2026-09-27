using UnityEngine;
using TMPro;
public class CookingPrepStepRowUI : MonoBehaviour
{
    public TMP_Text label;

    [Tooltip("Prefix shown before each step's text, e.g. a bullet character.")]
    public string bulletPrefix = "• ";

    public void SetText(string instructuion )
    {
        if (label != null)
            label.text = bulletPrefix + instructuion;
    }
}
