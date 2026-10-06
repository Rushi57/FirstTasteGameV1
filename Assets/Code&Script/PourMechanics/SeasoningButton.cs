using UnityEngine;
using UnityEngine.UI;

/// Put this on SoyBtn, VinegarBtn and CookingOilBtn.
/// Set a different profile (id, glass sprite, particle color) on each one.
[RequireComponent(typeof(Button))]
public class SeasoningButton : MonoBehaviour
{
    public SeasoningPourController controller;   // drag PourSeasoningPanel here
    public SeasoningPourController.SeasoningProfile profile;

    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() => controller.Open(profile));
    }
}