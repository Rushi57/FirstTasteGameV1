using UnityEngine;

public enum CookMode { Boil, Simmer }

public class BoilSimmerController : MonoBehaviour
{
    [Header("Panel")]
    public GameObject boilSimmerPanel;
    public ColorPrecisionGame game;

    [Header("Speeds (degrees/second)")]
    public float boilSpeed = 200f;
    public float simmerSpeed = 260f;
    public StoveHeatController stove;

    [Header("Boil Evaporation (starts after the mini-game succeeds)")]
    public PanLiquidFill panLiquid;
    public string evaporateLiquidId = "Water";
    [Tooltip("Total ml lost. Use >= the water you poured to dry the pan completely.")]
    public float boilEvaporateMl = 250f;
    public float boilEvaporateSeconds = 8f;

    public const string BoilStepId = "Pan:PanLid";
    public const string SimmerStepId = "Pan:PanLidSimmer";

    private CookMode currentMode = CookMode.Boil;

    private void OnEnable()
    {
        if (game != null) game.OnSucceeded += HandleGameSucceeded;
    }

    private void OnDisable()
    {
        if (game != null) game.OnSucceeded -= HandleGameSucceeded;
    }

    public void OpenAndConfigure(CookMode mode)
    {
        currentMode = mode;
        game.SetSpeed(mode == CookMode.Simmer ? simmerSpeed : boilSpeed);
        Debug.Log($"[BoilSimmer] Mode = {mode}");

        boilSimmerPanel.SetActive(false);
        boilSimmerPanel.SetActive(true);
    }

    private void HandleGameSucceeded()
    {
        if (currentMode != CookMode.Boil) return;   // simmer keeps the water

        Debug.Log("[BoilSimmer] Boil finished - starting evaporation");
        panLiquid?.StartEvaporate(evaporateLiquidId, boilEvaporateMl, boilEvaporateSeconds);
    }
}