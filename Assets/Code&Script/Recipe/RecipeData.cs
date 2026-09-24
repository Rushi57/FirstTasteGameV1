using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One line in a recipe's ingredient list: which IngredientData, how much,
/// and in what unit. Reuses your existing IngredientData assets (icon, id,
/// category, size) rather than duplicating that info here.
/// </summary>
[System.Serializable]
public class RecipeIngredientEntry
{
    public IngredientData ingredient;

    public float quantity = 1f;

    [Tooltip("e.g. \"cloves\", \"tbsp\", \"cup\", \"pc\" - purely for display text.")]
    public string unit;

    /// <summary>Formatted for display, e.g. "Garlic - 12 cloves" or "Water - 1 cup".</summary>
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

/// <summary>
/// A full recipe: its name/icon plus every ingredient needed, in order.
/// Create instances: right-click in Project window -> Create -> Game -> Recipe Data.
/// </summary>
[CreateAssetMenu(fileName = "RecipeData", menuName = "Game/Recipe Data")]
public class RecipeData : ScriptableObject
{
    [Header("Identity")]
    public string recipeName;
    public Sprite recipeIcon;

    [Header("Ingredients")]
    public List<RecipeIngredientEntry> ingredients = new List<RecipeIngredientEntry>();
}