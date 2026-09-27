using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Put this on your ingredient row prefab (the "Ingredient Name and Quantity"
/// rows in the scrolling Ingredients list). RecipeIngredientListUI spawns
/// one of these per ingredient in the recipe.
/// </summary>
public class RecipeIngredientRowUI : MonoBehaviour
{
    [Tooltip("Shows the combined 'Name - Quantity Unit' text, e.g. 'Garlic - 12 cloves'.")]
    public TMP_Text label;

    [Tooltip("Optional icon showing the ingredient's sprite.")]
    public Image icon;

    [Tooltip("Optional checkmark/highlight shown once this ingredient has been placed on the table. Leave unassigned if you don't want this visual.")]
    public GameObject collectedCheckmark;

    [Tooltip("Controls the row's dim/grayed-out look once its ingredient has been spawned. Auto-added if left unassigned.")]
    public CanvasGroup canvasGroup;

    [Range(0.1f, 1f)]
    [Tooltip("Alpha applied to the whole row once its ingredient has been spawned (grayed out).")]
    public float spawnedAlpha = 0.5f;

    private RecipeIngredientEntry entry;

    private void Awake()
    {
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void SetData(RecipeIngredientEntry ingredientEntry)
    {
        entry = ingredientEntry;

        if (label != null)
            label.text = entry.DisplayText;

        if (icon != null && entry.ingredient != null)
        {
            icon.sprite = entry.ingredient.icon;
            icon.enabled = entry.ingredient.icon != null;
        }

        SetCollected(false);
        SetSpawned(false);
    }

    /// <summary>Does this row represent the given ingredient?</summary>
    public bool Matches(IngredientData ingredient)
    {
        return entry != null && entry.ingredient == ingredient;
    }

    public void SetCollected(bool collected)
    {
        if (collectedCheckmark != null)
            collectedCheckmark.SetActive(collected);

        if (label != null)
            label.fontStyle = collected ? FontStyles.Strikethrough : FontStyles.Normal;
    }

    /// <summary>
    /// Dims the row to show its ingredient has already been spawned/pulled
    /// out (e.g. from the Fridge/Basket) - distinct from SetCollected, which
    /// is for "correctly placed on the table". A row can be spawned but not
    /// yet collected, or spawned AND collected at the same time.
    /// </summary>
    public void SetSpawned(bool spawned)
    {
        if (canvasGroup != null)
            canvasGroup.alpha = spawned ? spawnedAlpha : 1f;
    }
}