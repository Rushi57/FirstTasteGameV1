using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class RecipeIngredientRowUI1 : MonoBehaviour
{

    [Tooltip("Shows the combined 'Name - Quantity Unit' text, e.g. 'Garlic - 12 cloves'.")]
    public TMP_Text label;
    [Tooltip("Optional icon showing the ingredient's sprite.")]
    public Image icon;

    [Tooltip("Optional checkmark/highlight shown once this ingredient has been placed on the table. Leave unassigned if you don't want this visual.")]
    public GameObject collectedCheckmark;

    private RecipeIngredientEntry entry;

    public void SetData(RecipeIngredientEntry ingredientEntry)
    {
        entry = ingredientEntry;

        if (label != null)
            label.text = entry.DisplayText;

        if(icon != null && entry.ingredient != null)
        {
            icon.sprite = entry.ingredient.icon;
            icon.enabled = entry.ingredient != null;
        }

        SetCollected(false);
    }

    public bool Matches(IngredientData ingredient)
    {
        return entry != null && entry.ingredient == ingredient;
    }

    public void SetCollected(bool collected)
    {
        if(collectedCheckmark  != null)
            collectedCheckmark.SetActive(collected);

        if(label != null)
            label.fontStyle = collected ? FontStyles.Strikethrough : FontStyles.Normal;
    }

}
