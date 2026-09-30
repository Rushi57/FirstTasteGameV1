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

    public void OpenAndConfigure()
    {
        CookMode mode = GetRecipeMode();
        game.SetSpeed(mode == CookMode.Simmer ? simmerSpeed : boilSpeed);
        Debug.Log($"[BoilSimmer] Mode = {mode}");

        // Toggle so OnEnable (and its 2s headstart) always re-runs
        boilSimmerPanel.SetActive(false);
        boilSimmerPanel.SetActive(true);
    }

    // TODO: connect this to your real recipe data
    private CookMode GetRecipeMode()
    {
      RecipeData recipe = stove  != null ? stove.recipe : null;
        if(recipe == null)
        {
            Debug.LogWarning("[BoilSimmer] No recipe foind - default boild");
            return CookMode.Boil;
        }
        foreach(string line in recipe.cookingInstructions)
        {
            string lower = line.ToLower();
            if (!lower.Contains("lid")) continue;

            if (lower.Contains("simmer")) return CookMode.Simmer;
            if (lower.Contains("boil")) return CookMode.Boil;
        }

        return CookMode.Boil;
    }
}