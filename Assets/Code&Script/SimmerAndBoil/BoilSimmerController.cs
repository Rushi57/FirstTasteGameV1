using UnityEngine;

public enum CookMode { Boil, Simmer }

public class BoilSimmerController : MonoBehaviour
{
    [Header("Panel")]
    public GameObject boilSimmerPanel;      // Boil/SimmerPanel
    public ColorPrecisionGame game;         // the ColorPrecisionGame on that panel

    [Header("Speeds (degrees/second)")]
    public float boilSpeed = 200f;          // default speed
    public float simmerSpeed = 260f;        // "a little fast"
    public StoveHeatController stove;

    public const string BoilStepId = "Pan:PanLid";
    public const string SimmerStepId = "Pan:PanLidSimmer";

    public void OpenAndConfigure(CookMode mode)
    {
        game.SetSpeed(mode == CookMode.Simmer ? simmerSpeed : boilSpeed);
        Debug.Log($"[BoilSimmer] Mode = {mode}");

        boilSimmerPanel.SetActive(false);
        boilSimmerPanel.SetActive(true);
    }

    // TODO: connect this to your real recipe data
    private CookMode GetModeFromStep()
    {
        string current = CookingPrepListUI.Instance != null
            ? CookingPrepListUI.Instance.CurrentStepId : null;

        return current == SimmerStepId ? CookMode.Simmer : CookMode.Boil;
    }

}