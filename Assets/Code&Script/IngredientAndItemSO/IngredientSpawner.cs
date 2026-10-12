using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Spawns an IngredientData as a draggable UI item by instantiating the
/// matching prefab (small/large, ingredient/utensil), then wires up its
/// Image, TestDrag, and TutorialInteractable so it's immediately usable -
/// no manual Hierarchy setup, no matter which of the 4 prefabs gets picked.
///
/// Each spawned item is sized as: (prefab's own size) x (IngredientData.iconScale),
/// so small things like garlic can look smaller than pork or a pitcher while
/// still using the same prefab.
///
/// If Slot Manager is assigned, spawns into the first EMPTY table slot
/// instead of a fixed position - see TableItemSlotManager. Because that
/// counts as the ingredient effectively "arriving" on the table already,
/// this also runs the recipe check right away (TableItemSlotManager.TryCollect)
/// rather than waiting for a drag-drop that will never happen for it.
/// </summary>
public class IngredientSpawner : MonoBehaviour
{
    [Header("Prefabs - one per category/size combination")]
    public GameObject ingredientSmallPrefab; // e.g. IngTestPrefabSmall
    public GameObject ingredientLargePrefab; // e.g. IngTestPrefabLarge
    public GameObject utensilSmallPrefab;    // e.g. UtenTestPrefabSmall
    public GameObject utensilLargePrefab;    // e.g. UtenTestPrefabLarge

    [Header("Table Slots (preferred)")]
    [Tooltip("If assigned, ingredients spawn into the first empty ItemContainerDropZone slot instead of a fixed spawnParent/spawnPosition.")]
    public TableItemSlotManager slotManager;

    [Tooltip("Optional: if assigned, the matching row in the recipe checklist grays out the moment this ingredient is spawned.")]
    public RecipeIngredientListUI ingredientList;

    [Header("Fallback: fixed position (used only if Slot Manager is NOT assigned)")]
    [Tooltip("Parent RectTransform the spawned item will be placed under (usually a Canvas or a container inside one).")]
    public RectTransform spawnParent;

    [Tooltip("Local anchored position within spawnParent for the FIRST item. Ignored if spawnParent has a Layout Group - that will position children automatically instead.")]
    public Vector2 spawnPosition = Vector2.zero;

    [Tooltip("How far to offset each successive spawned item from the last, so they don't stack on top of each other. Ignored if spawnParent has a Layout Group.")]
    public Vector2 spawnSpacing = new Vector2(140f, 0f);

    private int spawnCount = 0;

    // Tracks the live spawned object for each IngredientData currently on the table.
    private readonly System.Collections.Generic.Dictionary<IngredientData, GameObject> spawnedItems
        = new System.Collections.Generic.Dictionary<IngredientData, GameObject>();

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


        // Already on the table? Warn, but do NOT deduct a heart and do NOT spawn a duplicate.
        if (spawnedItems.TryGetValue(data, out GameObject existing))
        {
            if (existing != null)   // Unity-null check: false if it was destroyed
            {
                string itemName = string.IsNullOrEmpty(data.displayName) ? data.name : data.displayName;
                WarningMessageUI.Instance?.Show($"{itemName} is already on the table");
                return;
            }
            spawnedItems.Remove(data);   // stale entry, item was removed
        }


        RectTransform targetSlot = null;

        if (slotManager != null)
        {
            targetSlot = slotManager.GetEmptySlot();
            if (targetSlot == null)
            {
                Debug.LogWarning("[IngredientSpawner] All table slots are full - can't spawn right now.");
                return;
            }
        }
        else if (spawnParent == null)
        {
            Debug.LogWarning("[IngredientSpawner] Neither Slot Manager nor Spawn Parent assigned.");
            return;
        }

        RectTransform parent = targetSlot != null ? targetSlot : spawnParent;

        GameObject prefab = SelectPrefab(data);
        if (prefab == null)
        {
            Debug.LogWarning($"[IngredientSpawner] No prefab assigned for category={data.category}, size={data.size}.");
            return;
        }

        GameObject go = Instantiate(prefab, parent);
        go.name = string.IsNullOrEmpty(data.displayName) ? data.name : data.displayName;
        spawnedItems[data] = go;
        RectTransform rect = go.transform as RectTransform;
        if (rect != null)
        {
            // Size = the prefab's own size x this ingredient's iconScale.
            // SetSizeWithCurrentAnchors works whether the anchors are a point or stretched.
            RectTransform prefabRect = prefab.transform as RectTransform;
            if (prefabRect != null)
            {
                Vector2 baseSize = prefabRect.rect.size;
                float scale = data.iconScale > 0f ? data.iconScale : 1f;
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, baseSize.x * scale);
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, baseSize.y * scale);
            }
            rect.localScale = Vector3.one;

            if (targetSlot != null)
            {
                // Slot-based spawning: center inside the slot.
                rect.anchoredPosition = Vector2.zero;
            }
            else if (spawnParent.GetComponent<LayoutGroup>() == null)
            {
                // Fallback fixed-position spawning with manual offset.
                rect.anchoredPosition = spawnPosition + spawnSpacing * spawnCount;
            }
            // If spawnParent has a Layout Group, leave positioning to it.
        }

        spawnCount++;

        if (targetSlot != null)
        {
            slotManager.OccupySlot(targetSlot, go);
            TableSlotOccupant occupant = go.AddComponent<TableSlotOccupant>();
            occupant.slotManager = slotManager;
            occupant.slot = targetSlot;

            // Spawning straight into a table slot IS the ingredient arriving
            // on the table - run the same recipe check a manual drag-drop
            // would have triggered, so the checklist/panel switch still works.
            bool wasNeeded = slotManager.TryCollect(data);
            if (!wasNeeded)
                Debug.Log($"[IngredientSpawner] '{data.displayName}' spawned onto the table but isn't needed by the current recipe.");
        }

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

        ingredientList?.MarkSpawned(data);

        Debug.Log($"[IngredientSpawner] Spawned '{data.displayName}' (id='{data.id}', category={data.category}, size={data.size}, iconScale={data.iconScale}) into {parent.name}");
    }

    private GameObject SelectPrefab(IngredientData data)
    {
        if (data.category == ItemCategory.Ingredient)
            return data.size == ItemSize.Small ? ingredientSmallPrefab : ingredientLargePrefab;
        else
            return data.size == ItemSize.Small ? utensilSmallPrefab : utensilLargePrefab;
    }
}