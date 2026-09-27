using System.Collections.Generic;
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
    [Tooltip("Each entry is one bullet-point step, shown in order in the Cooking Prep panel once all ingredients are on the table. Plain text - no ScriptableObject needed per line.")]
    public List<string> cookingInstructions = new List<string>();

    /// <summary>
    /// Scans cookingInstructions for a line that mentions this ingredient's
    /// display name, and returns the prep state implied by keywords in that
    /// line (e.g. "slice" -> Sliced, "minced"/"mince" -> Minced).
    /// Defaults to Whole if no matching instruction or keyword is found.
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

    /// <summary>
    /// Convenience overload: look up the required prep state by ingredient id
    /// instead of holding an IngredientData reference.
    /// </summary>
    public IngredientPrepState GetRequiredPrepState(string ingredientId)
    {
        foreach (var entry in ingredients)
        {
            if (entry.ingredient != null && entry.ingredient.id == ingredientId)
                return GetRequiredPrepState(entry.ingredient);
        }
        return IngredientPrepState.Whole;
    }
}