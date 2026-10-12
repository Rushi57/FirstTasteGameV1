using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RecipeIngredientRowUI : MonoBehaviour
{
    [Tooltip("Shows the combined 'Name - Quantity Unit' text, e.g. 'Garlic - 12 cloves'.")]
    public TMP_Text label;

    [Tooltip("Optional icon showing the ingredient's sprite.")]
    public Image icon;

    [Tooltip("Check image shown once the ingredient has been gathered/spawned.")]
    public GameObject spawnedCheckmark;
    [Tooltip("Keeps the check image's proportions instead of stretching it to the rect.")]
    public bool preserveCheckAspect = true;

    [Tooltip("Also preserve the aspect ratio of the ingredient icon.")]
    public bool preserveIconAspect = true;

    [Tooltip("Optional: check/highlight shown once placed on the table.")]
    public GameObject collectedCheckmark;

    [Header("Optional dimming (off by default)")]
    public bool dimWhenSpawned = false;
    public CanvasGroup canvasGroup;
    [Range(0.1f, 1f)] public float spawnedAlpha = 0.5f;



    private RecipeIngredientEntry entry;

    private void Awake()
    {
        if (dimWhenSpawned && canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (icon != null)
            icon.preserveAspect = preserveIconAspect;
    }


    public void SetData(RecipeIngredientEntry ingredientEntry)
    {
        entry = ingredientEntry;

        if (label != null)
            label.text = entry.ingredient != null ? entry.ingredient.name : "";

        if (icon != null && entry.ingredient != null)
        {
            icon.sprite = entry.ingredient.icon;
            icon.enabled = entry.ingredient.icon != null;
        }

        SetCollected(false);
        SetSpawned(false);
    }

    public bool Matches(IngredientData ingredient)
    {
        return entry != null && entry.ingredient == ingredient;
    }

    public void SetCollected(bool collected)
    {
        if (collectedCheckmark != null)
            collectedCheckmark.SetActive(collected);
    }

    /// <summary>Shows the check image when the ingredient has been gathered.</summary>
    public void SetSpawned(bool spawned)
    {
        if (spawnedCheckmark != null)
            spawnedCheckmark.SetActive(spawned);

        if (dimWhenSpawned && canvasGroup != null)
            canvasGroup.alpha = spawned ? spawnedAlpha : 1f;
    }
}