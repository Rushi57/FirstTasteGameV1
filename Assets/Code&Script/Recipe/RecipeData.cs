using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

[System.Serializable]
public class RecipeIngredientEntry
{
    public IngredientData ingredient;

    public float quantity = 1f;

    [Tooltip("e.g. \"cloves\", \"tbsp\", \"cup\", \"pc\" - purely for display text.")]
    public string unit;

    public string DisplayText
    {
        get
        {
            string name = ingredient != null ? ingredient.displayName : "(missing ingredient)";
            string qty = quantity == Mathf.Floor(quantity) ? quantity.ToString("0") : quantity.ToString("0.##");
            return string.IsNullOrEmpty(unit) ? $"{name} - {qty}" : $"{name} - {qty} {unit}";
        }
    }
}

[CreateAssetMenu(fileName = "RecipeData", menuName = "Game/Recipe Data")]
public class RecipeData : ScriptableObject
{
    [Header("Identity")]
    public string recipeName;
    public Sprite recipeIcon;

    [Header("Ingredients")]
    public List<RecipeIngredientEntry> ingredients = new List<RecipeIngredientEntry>();

    [Header("Prep & Cooking Instructions")]
    [Tooltip("Each entry is one bullet-point step, shown in order in the Cooking Prep panel. Plain text.")]
    public List<string> cookingInstructions = new List<string>();

    [Tooltip("Optional. Same order as Cooking Instructions. Only needed for steps that aren't auto-detected (e.g. the pan). Leave an entry empty for auto-detect, or type '-' to force 'no auto-completion' for that line.")]
    public List<string> cookingStepIds = new List<string>();

    // ------------------------------------------------------------------
    // Prep state (Whole / Sliced / Minced)
    // ------------------------------------------------------------------

    /// <summary>
    /// Scans cookingInstructions for a line that mentions this ingredient's
    /// display name, and returns the prep state implied by keywords in that line.
    /// Defaults to Whole if nothing matches.
    /// </summary>
    public IngredientPrepState GetRequiredPrepState(IngredientData ingredient)
    {
        if (ingredient == null || string.IsNullOrEmpty(ingredient.displayName))
            return IngredientPrepState.Whole;

        string name = ingredient.displayName.ToLowerInvariant();

        foreach (string instruction in cookingInstructions)
        {
            if (string.IsNullOrEmpty(instruction)) continue;

            string line = instruction.ToLowerInvariant();
            if (!line.Contains(name)) continue;

            if (line.Contains("minced") || line.Contains("mince"))
                return IngredientPrepState.Minced;

            if (line.Contains("sliced") || line.Contains("slice"))
                return IngredientPrepState.Sliced;
        }

        return IngredientPrepState.Whole;
    }

    public IngredientPrepState GetRequiredPrepState(string ingredientId)
    {
        foreach (var entry in ingredients)
        {
            if (entry.ingredient != null && entry.ingredient.id == ingredientId)
                return GetRequiredPrepState(entry.ingredient);
        }
        return IngredientPrepState.Whole;
    }

    // ------------------------------------------------------------------
    // Step ids (used by CookingPrepListUI to match rows to completed steps)
    // ------------------------------------------------------------------

    /// <summary>
    /// Returns the id for the instruction at this index, or null if the step
    /// has no auto-completion. Priority:
    ///  1. Manual id in cookingStepIds ("-" means none)
    ///  2. Stove heat line   -> "Stove:{order}:{Heat}"
    ///  3. Chop/mince line   -> "{IngredientId}:{State}"
    /// </summary>
    public string GetStepId(int index)
    {
        if (index < 0 || index >= cookingInstructions.Count) return null;

        // 1. Manual override
        if (index < cookingStepIds.Count && !string.IsNullOrEmpty(cookingStepIds[index]))
        {
            string manual = cookingStepIds[index].Trim();
            return manual == "-" ? null : manual;
        }

        string raw = cookingInstructions[index];
        if (string.IsNullOrEmpty(raw)) return null;

        // 2. Stove heat step
        if (TryGetStoveStep(index, out int order, out StoveHeat heat))
            return StoveStepId(order, heat);

        // 3. Chop / mince step (line must START with a prep verb, so
        //    "Add the slice Garlick" is not mistaken for a chopping step)
        string line = raw.ToLowerInvariant();
        string trimmed = line.TrimStart();
        bool isPrepLine = trimmed.StartsWith("chop") || trimmed.StartsWith("slice")
                       || trimmed.StartsWith("mince") || trimmed.StartsWith("cut");
        if (!isPrepLine) return null;

        foreach (var entry in ingredients)
        {
            var ing = entry.ingredient;
            if (ing == null || string.IsNullOrEmpty(ing.displayName)) continue;
            if (!line.Contains(ing.displayName.ToLowerInvariant())) continue;

            if (line.Contains("mince")) return $"{ing.id}:{IngredientPrepState.Minced}";
            if (line.Contains("slice")) return $"{ing.id}:{IngredientPrepState.Sliced}";
        }

        return null;
    }

    // ------------------------------------------------------------------
    // Stove steps
    // ------------------------------------------------------------------

    /// <summary>Single source of truth for stove step ids. Used by RecipeData AND StoveHeatController.</summary>
    public static string StoveStepId(int order, StoveHeat heat) => $"Stove:{order}:{heat}";

    private static bool IsStoveHeatLine(string text)
    {
        return GetHeatFromLine(text) != StoveHeat.Off;
    }

    /// <summary>Returns High/Medium/Low if this is a stove-heat instruction, otherwise Off.</summary>
    private static StoveHeat GetHeatFromLine(string text)
    {
        if (string.IsNullOrEmpty(text)) return StoveHeat.Off;

        string l = text.ToLowerInvariant();
        if (!l.Contains("stove")) return StoveHeat.Off;
        if (l.Contains("pan in the stove") || l.Contains("pan on the stove")) return StoveHeat.Off;

        // Whole-word matches so "allow" doesn't count as "low"
        if (Regex.IsMatch(l, @"\bhigh\b")) return StoveHeat.High;
        if (Regex.IsMatch(l, @"\bmedium\b")) return StoveHeat.Medium;
        if (Regex.IsMatch(l, @"\blow\b")) return StoveHeat.Low;

        return StoveHeat.Off;
    }

    /// <summary>
    /// If the instruction at this index is a stove heat step, returns its
    /// order among stove steps (0 = first stove step, 1 = second, ...) and heat.
    /// </summary>
    public bool TryGetStoveStep(int index, out int order, out StoveHeat heat)
    {
        order = 0;
        heat = StoveHeat.Off;

        if (index < 0 || index >= cookingInstructions.Count) return false;

        heat = GetHeatFromLine(cookingInstructions[index]);
        if (heat == StoveHeat.Off) return false;

        for (int i = 0; i < index; i++)
            if (IsStoveHeatLine(cookingInstructions[i])) order++;

        return true;
    }

    public int StoveStepCount()
    {
        int n = 0;
        foreach (var s in cookingInstructions)
            if (IsStoveHeatLine(s)) n++;
        return n;
    }

    /// Returns the quantity + unit for an ingredient id (case-insensitive).
    public bool TryGetAmount(string ingredientId, out RecipeAmount amount)
    {
        foreach (var entry in ingredients)
        {
            var ing = entry.ingredient;
            if (ing == null) continue;

            if (string.Equals(ing.id, ingredientId, System.StringComparison.OrdinalIgnoreCase))
            {
                amount = new RecipeAmount(entry.quantity, entry.unit);
                return true;
            }
        }
        amount = default;
        return false;
    }
}