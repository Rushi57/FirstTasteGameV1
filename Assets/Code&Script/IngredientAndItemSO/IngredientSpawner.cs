using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Spawns an IngredientData as a draggable UI item by instantiating the
/// matching prefab (small/large, ingredient/utensil), then wires up its
/// Image, TestDrag, and TutorialInteractable so it's immediately usable -
/// no manual Hierarchy setup, no matter which of the 4 prefabs gets picked.
/// </summary>
public class IngredientSpawner : MonoBehaviour
{
    [Header("Prefabs - one per category/size combination")]
    public GameObject ingredientSmallPrefab; // e.g. IngTestPrefabSmall
    public GameObject ingredientLargePrefab; // e.g. IngTestPrefabLarge
    public GameObject utensilSmallPrefab;    // e.g. UtenTestPrefabSmall
    public GameObject utensilLargePrefab;    // e.g. UtenTestPrefabLarge

    [Header("Where to spawn it")]
    [Tooltip("Parent RectTransform the spawned item will be placed under (usually a Canvas or a container inside one).")]
    public RectTransform spawnParent;

    [Tooltip("Local anchored position within spawnParent for the FIRST item. Ignored if spawnParent has a Layout Group - that will position children automatically instead.")]
    public Vector2 spawnPosition = Vector2.zero;

    [Tooltip("How far to offset each successive spawned item from the last, so they don't stack on top of each other. Ignored if spawnParent has a Layout Group.")]
    public Vector2 spawnSpacing = new Vector2(140f, 0f);

    private int spawnCount = 0;

    /// <summary>
    /// Spawn a specific ingredient. Hook this up to a UI Button OnClick() 
    /// and drag your IngredientData ScriptableObject into the inspector slot!
    /// </summary>
    public void SpawnIngredient(IngredientData data)
    {
        if (data == null)
        {
            Debug.LogWarning("[IngredientSpawner] No IngredientData passed to SpawnIngredient - nothing to spawn.");
            return;
        }
        if (spawnParent == null)
        {
            Debug.LogWarning("[IngredientSpawner] No Spawn Parent assigned.");
            return;
        }

        GameObject prefab = SelectPrefab(data);
        if (prefab == null)
        {
            Debug.LogWarning($"[IngredientSpawner] No prefab assigned for category={data.category}, size={data.size}.");
            return;
        }

        GameObject go = Instantiate(prefab, spawnParent);
        go.name = string.IsNullOrEmpty(data.displayName) ? data.name : data.displayName;

        RectTransform rect = go.transform as RectTransform;
        if (rect != null && spawnParent.GetComponent<UnityEngine.UI.LayoutGroup>() == null)
        {
            rect.anchoredPosition = spawnPosition + spawnSpacing * spawnCount;
        }

        spawnCount++;

        // Get-or-add required UI and drag components
        Image image = go.GetComponent<Image>();
        if (image == null) image = go.AddComponent<Image>();
        image.sprite = data.icon;
        image.preserveAspect = true;
        image.raycastTarget = true;

        if (go.GetComponent<CanvasGroup>() == null)
            go.AddComponent<CanvasGroup>();

        TestDrag drag = go.GetComponent<TestDrag>();
        if (drag == null) drag = go.AddComponent<TestDrag>();
        drag.itemId = data.id;

        TutorialInteractable interactable = go.GetComponent<TutorialInteractable>();
        if (interactable == null) interactable = go.AddComponent<TutorialInteractable>();
        interactable.sourceData = data;

        // Re-register with TutorialManager now that sourceData is assigned
        TutorialManager.Instance?.Register(interactable);

        Debug.Log($"[IngredientSpawner] Spawned '{data.displayName}' (id='{data.id}', category={data.category}, size={data.size}) under {spawnParent.name}");
    }

    private GameObject SelectPrefab(IngredientData data)
    {
        if (data.category == ItemCategory.Ingredient)
            return data.size == ItemSize.Small ? ingredientSmallPrefab : ingredientLargePrefab;
        else
            return data.size == ItemSize.Small ? utensilSmallPrefab : utensilLargePrefab;
    }
}