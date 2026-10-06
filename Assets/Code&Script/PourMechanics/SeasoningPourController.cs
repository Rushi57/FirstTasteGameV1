using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// One ingredient amount taken from the RecipeSO (Quantity + Unit fields).
public readonly struct RecipeAmount
{
    public readonly float quantity;
    public readonly string unit;
    public RecipeAmount(float quantity, string unit) { this.quantity = quantity; this.unit = unit; }
}

/// Converts recipe quantity/unit to ml and formats labels like "1/2 cup".
public static class MeasureConverter
{
    public const float TspMl = 5f, TbspMl = 15f, CupMl = 250f;

    /// Accepts: tsp, teaspoon(s), tbsp, tbps (typo-safe), tablespoon(s), cup(s). Returns null if unknown.
    public static string NormalizeUnit(string unit)
    {
        if (string.IsNullOrWhiteSpace(unit)) return null;
        switch (unit.Trim().ToLowerInvariant())
        {
            case "tsp": case "teaspoon": case "teaspoons": return "tsp";
            case "tbsp": case "tbps": case "tablespoon": case "tablespoons": return "tbsp";
            case "cup": case "cups": return "cup";
            default: return null;
        }
    }

    public static float ToMl(float quantity, string unit)
    {
        switch (NormalizeUnit(unit))
        {
            case "tsp": return quantity * TspMl;
            case "tbsp": return quantity * TbspMl;
            case "cup": return quantity * CupMl;
            default: return -1f;
        }
    }

    static readonly (float value, string text)[] Fractions =
    {
        (0.125f, "1/8"), (0.25f, "1/4"), (0.333f, "1/3"), (0.5f, "1/2"), (0.667f, "2/3"), (0.75f, "3/4")
    };

    public static string Format(float quantity, string unit)
    {
        float whole = Mathf.Floor(quantity + 0.01f);
        float frac = quantity - whole;
        string fracText = "";
        foreach (var f in Fractions)
            if (Mathf.Abs(frac - f.value) < 0.02f) { fracText = f.text; break; }

        string number;
        if (whole > 0 && fracText != "") number = $"{whole:0} {fracText}";
        else if (whole > 0) number = $"{whole:0}";
        else number = fracText != "" ? fracText : quantity.ToString("0.##");

        return $"{number} {NormalizeUnit(unit) ?? unit}";
    }
}

/// Put this on PourSeasoningPanel.
/// Flow: Soy/Vinegar/Oil button -> PickPanel -> (Tbsp or Cup) -> PourImageBoarder + measurement list -> pour.
/// The pour target comes from the recipe; the measurement the player picks is checked against it.
public class SeasoningPourController : MonoBehaviour
{
    [Serializable]
    public class SeasoningProfile
    {
        [Tooltip("Drag the IngredientData asset (Soy, Vinegar, Cooking Oil)")]
        public IngredientData ingredient;
        [Tooltip("Only used if Ingredient is empty")]
        public string id = "Soy";
        public Sprite glassSprite;
        public Color particleColor = Color.black;

        public string Id => ingredient != null ? ingredient.id : id;
    }

    [Header("Panels (children of PourSeasoningPanel)")]
    public GameObject pickPanel;
    public GameObject pourImageBoarder;
    public GameObject tbspMeasurementScrolling;
    public GameObject cupMeasurementPick;

    [Header("Pick Panel Buttons")]
    public Button pickTablespoonButton;
    public Button pickCupButton;

    [Header("Pour Visuals")]
    public Image glassImage;
    public ParticleSystem pourParticles;
    public PourMechanic pourMechanic;
    public Button closeButton;

    [Header("Recipe Feedback (optional)")]
    [Tooltip("Shows e.g. 'Recipe needs: 1/2 cup (125 ml)'")]
    public TMP_Text requirementLabel;
    [Tooltip("Shows 'Correct measure!' / 'Wrong measure!'")]
    public TMP_Text messageLabel;

    [Header("Recipe Rules")]
    [Range(0f, 0.2f)] public float measureTolerance = 0.05f;
    [Tooltip("Allows e.g. 1/4 cup x2 for a 1/2 cup recipe")]
    public int maxScoops = 4;
    [Tooltip("If true, wrong measures block pouring. If false, the picked measure becomes the target.")]
    public bool requireCorrectMeasure = true;

    /// Set this from your recipe manager. Given an ingredient id, return its amount in the current recipe (or null).
    public Func<string, RecipeAmount?> RecipeLookup;

    public enum Tool { None, Tablespoon, Cup }
    public Tool CurrentTool { get; private set; } = Tool.None;
    public SeasoningProfile CurrentSeasoning { get; private set; }

    /// seasoning id, result, poured ml, target ml
    public event Action<string, PourMechanic.PourResult, float, float> OnSeasoningPoured;

    private bool hasRecipe;
    private float recipeMl;
    private string recipeLabel = "";

    void Awake()
    {
        if (pickTablespoonButton) pickTablespoonButton.onClick.AddListener(() => ChooseTool(Tool.Tablespoon));
        if (pickCupButton) pickCupButton.onClick.AddListener(() => ChooseTool(Tool.Cup));
        if (closeButton) closeButton.onClick.AddListener(Close);
        if (pourMechanic) pourMechanic.OnPourFinished += HandlePourFinished;
    }

    void OnDestroy()
    {
        if (pourMechanic) pourMechanic.OnPourFinished -= HandlePourFinished;
    }

    public void Open(SeasoningProfile profile)
    {
        CurrentSeasoning = profile;
        CurrentTool = Tool.None;
        gameObject.SetActive(true);

        if (glassImage && profile.glassSprite) glassImage.sprite = profile.glassSprite;
        if (pourParticles)
        {
            var main = pourParticles.main;
            main.startColor = profile.particleColor;
        }

        LoadRecipeAmount(profile.id);

        if (pickPanel) pickPanel.SetActive(true);
        if (pourImageBoarder) pourImageBoarder.SetActive(false);
        if (tbspMeasurementScrolling) tbspMeasurementScrolling.SetActive(false);
        if (cupMeasurementPick) cupMeasurementPick.SetActive(false);

        SetMessage("");
        if (pourMechanic) pourMechanic.SetTarget(0f, "");
    }

    void LoadRecipeAmount(string id)
    {
        hasRecipe = false;
        recipeMl = 0f;
        recipeLabel = "";

        RecipeAmount? amount = RecipeLookup?.Invoke(id);
        if (amount.HasValue)
        {
            float ml = MeasureConverter.ToMl(amount.Value.quantity, amount.Value.unit);
            if (ml > 0f)
            {
                hasRecipe = true;
                recipeMl = ml;
                recipeLabel = MeasureConverter.Format(amount.Value.quantity, amount.Value.unit);
            }
            else Debug.LogWarning($"Recipe amount for '{id}' has quantity 0 or an unknown unit '{amount.Value.unit}'.");
        }
        else Debug.LogWarning($"Recipe has no entry for '{id}'. Falling back to the picked measure.");

        if (requirementLabel)
            requirementLabel.text = hasRecipe ? $"Recipe needs: {recipeLabel} ({recipeMl:0.#} ml)" : "";
    }

    void ChooseTool(Tool tool)
    {
        CurrentTool = tool;
        if (pickPanel) pickPanel.SetActive(false);
        if (pourImageBoarder) pourImageBoarder.SetActive(true);
        if (tbspMeasurementScrolling) tbspMeasurementScrolling.SetActive(tool == Tool.Tablespoon);
        if (cupMeasurementPick) cupMeasurementPick.SetActive(tool == Tool.Cup);

        SetMessage("");
        if (pourMechanic) pourMechanic.SetTarget(0f, "");
    }

    /// Called by MeasureOptionButton.
    public void SelectMeasure(float ml, string label)
    {
        if (!pourMechanic) return;

        if (!hasRecipe)
        {
            pourMechanic.SetTarget(ml, label);
            return;
        }

        if (MatchesRecipe(ml, out int scoops))
        {
            pourMechanic.SetTarget(recipeMl, recipeLabel);
            SetMessage(scoops > 1 ? $"Correct! {label} x{scoops}" : "Correct measure!");
        }
        else if (requireCorrectMeasure)
        {
            pourMechanic.SetTarget(0f, "");
            SetMessage($"Wrong measure! Recipe needs {recipeLabel}");
        }
        else
        {
            pourMechanic.SetTarget(ml, label);
            SetMessage($"Recipe needs {recipeLabel}");
        }
    }

    bool MatchesRecipe(float measureMl, out int scoops)
    {
        scoops = 0;
        if (measureMl <= 0f) return false;

        scoops = Mathf.RoundToInt(recipeMl / measureMl);
        if (scoops < 1 || scoops > maxScoops) return false;

        float diff = Mathf.Abs(measureMl * scoops - recipeMl) / recipeMl;
        return diff <= measureTolerance;
    }

    void SetMessage(string text)
    {
        if (messageLabel) messageLabel.text = text;
    }

    void HandlePourFinished(PourMechanic.PourResult result, float poured, float target)
    {
        OnSeasoningPoured?.Invoke(CurrentSeasoning != null ? CurrentSeasoning.id : "", result, poured, target);
    }

    public void Close()
    {
        if (pourMechanic) pourMechanic.ResetPour();
        gameObject.SetActive(false);
    }
}