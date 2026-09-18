using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Spawns an IngredientData as a draggable UI item by instantiating the
/// matching prefab (small/large, ingredient/utensil), then wires up its
/// Image, TestDrag, and TutorialInteractable so it's immediately usable -
/// no manual Hierarchy setup, no matter which of the 4 prefabs gets picked.
///
/// Hook SpawnIngredient() up to a Button's OnClick() to test "tap this
/// button to spawn the ingredient".
/// </summary>
public class IngredientSpawner : MonoBehaviour
{
    [Header("What to spawn")]
    [Tooltip("Used by the parameterless SpawnIngredient() (e.g. for Button OnClick()).")]
    public IngredientData ingredientToSpawn;

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

    /// <summary>Wire this to a Button's OnClick() - spawns ingredientToSpawn.</summary>
    public void SpawnIngredient()
    {
        SpawnIngredient(ingredientToSpawn);
    }

    /// <summary>Spawn a specific ingredient (useful if one spawner/button handles several types).</summary>
    public GameObject SpawnIngredient(IngredientData data)
    {
        if (data == null)
        {
            Debug.LogWarning("[IngredientSpawner] No IngredientData assigned - nothing to spawn.");
            return null;
        }
        if (spawnParent == null)
        {
            Debug.LogWarning("[IngredientSpawner] No Spawn Parent assigned.");
            return null;
        }

        GameObject prefab = SelectPrefab(data);
        if (prefab == null)
        {
            Debug.LogWarning($"[IngredientSpawner] No prefab assigned for category={data.category}, size={data.size}.");
            return null;
        }

        GameObject go = Instantiate(prefab, spawnParent);
        go.name = string.IsNullOrEmpty(data.displayName) ? data.name : data.displayName;

        RectTransform rect = go.transform as RectTransform;
        if (rect != null && spawnParent.GetComponent<UnityEngine.UI.LayoutGroup>() == null)
        {
            // No Layout Group on the parent - position manually, offsetting
            // each successive spawn so items don't stack on top of each other.
            rect.anchoredPosition = spawnPosition + spawnSpacing * spawnCount;
            // Width/height are NOT overridden here - each prefab already has
            // the correct baked-in size for its category/size combination.
        }
        // If spawnParent DOES have a Layout Group, it positions children
        // automatically - leave anchoredPosition alone so it isn't fighting
        // the layout every frame.

        spawnCount++;

        // Get-or-add each component, so this works whether the prefab
        // already has them pre-attached (recommended) or you kept the
        // prefabs as plain Images and want the spawner to add behavior.
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
        interactable.sourceData = data; // pulls its tutorial id from data.TutorialId automatically

        // IMPORTANT: OnEnable() already fired during Instantiate()/AddComponent()
        // above, before sourceData was assigned - so it registered with an
        // empty id and got skipped. Re-register now that sourceData is set.
        TutorialManager.Instance?.Register(interactable);

        Debug.Log($"[IngredientSpawner] Spawned '{data.displayName}' (id='{data.id}', category={data.category}, size={data.size}) under {spawnParent.name}");

        return go;
    }

    private GameObject SelectPrefab(IngredientData data)
    {
        if (data.category == ItemCategory.Ingredient)
            return data.size == ItemSize.Small ? ingredientSmallPrefab : ingredientLargePrefab;
        else
            return data.size == ItemSize.Small ? utensilSmallPrefab : utensilLargePrefab;
    }
}