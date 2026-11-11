using UnityEngine;
using UnityEngine.UI;


public class IngredientSpawner : MonoBehaviour
{
    [Header("Prefabs - one per category/size combination")]
    public GameObject ingredientSmallPrefab; 
    public GameObject ingredientLargePrefab; 
    public GameObject utensilSmallPrefab;   
    public GameObject utensilLargePrefab;  

    [Header("Plate (for ingredients with Needs Plate ticked)")]
    [Tooltip("Plate sprite used as the container. Leave empty to disable plates entirely.")]
    public Sprite plateSprite;

    [Tooltip("How big the ingredient icon is relative to the plate.")]
    [Range(0.3f, 1f)] public float iconSizeOnPlate = 0.75f;

    [Tooltip("Nudge the icon on the plate (x, y) so it sits nicely in the dish.")]
    public Vector2 iconOffsetOnPlate = new Vector2(0f, 6f);

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

    private readonly System.Collections.Generic.Dictionary<IngredientData, GameObject> spawnedItems
        = new System.Collections.Generic.Dictionary<IngredientData, GameObject>();


    [Tooltip("Exact plate width in pixels. Height stays at the prefab's height.")]
    public float plateWidth = 130f;

    [Tooltip("Move the plate relative to the food (x, y).")]
    public Vector2 plateOffset = Vector2.zero;

    [Tooltip("Off = the plate sprite stretches to fill the new width/height. On = it keeps its original proportions.")]
    public bool plateKeepAspect = false;


    public void SpawnIngredient(IngredientData data)
    {
        if (data == null)
        {
            Debug.LogWarning("[IngredientSpawner] No IngredientData passed to SpawnIngredient - nothing to spawn.");
            return;
        }

        //Not part of the current Recipe
        if (ingredientList != null && !ingredientList.IsInRecipe(data))
        {
            string wrongName = string.IsNullOrEmpty(data.displayName) ? data.name : data.displayName;
            WarningMessageUI.Instance?.Show($"{wrongName} is not needed for this recipe");
            return;
        }


        // Already on the table? Warn, but do NOT deduct a heart and do NOT spawn a duplicate.
        if (spawnedItems.TryGetValue(data, out GameObject existing))
        {
            if (existing != null)  
            {
                string itemName = string.IsNullOrEmpty(data.displayName) ? data.name : data.displayName;
                WarningMessageUI.Instance?.Show($"{itemName} is already on the table");
                return;
            }
            spawnedItems.Remove(data);   
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

      
        bool usePlate = plateSprite != null
                        && data.category == ItemCategory.Ingredient
                        && data.needsPlate;

        RectTransform rect = go.transform as RectTransform;
        if (rect != null)
        {
            
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
               
                rect.anchoredPosition = Vector2.zero;
            }
            else if (spawnParent.GetComponent<LayoutGroup>() == null)
            {
             
                rect.anchoredPosition = spawnPosition + spawnSpacing * spawnCount;
            }
          
        }

        spawnCount++;

        if (targetSlot != null)
        {
            slotManager.OccupySlot(targetSlot, go);
            TableSlotOccupant occupant = go.AddComponent<TableSlotOccupant>();
            occupant.slotManager = slotManager;
            occupant.slot = targetSlot;

         
            bool wasNeeded = slotManager.TryCollect(data);
            if (!wasNeeded)
                Debug.Log($"[IngredientSpawner] '{data.displayName}' spawned onto the table but isn't needed by the current recipe.");
        }
        Image image = go.GetComponent<Image>();
        if (image == null) image = go.AddComponent<Image>();
        image.sprite = data.icon;        
        image.preserveAspect = true;
        image.raycastTarget = true;

        if (usePlate)
        {
            
            GameObject plateGO = new GameObject(go.name + " Plate",
                typeof(RectTransform), typeof(Image), typeof(LayoutElement));

            RectTransform plateRect = (RectTransform)plateGO.transform;
            plateRect.SetParent(parent, false);
            Vector2 plateBase = ((RectTransform)prefab.transform).rect.size;
            plateRect.sizeDelta = new Vector2(plateWidth, plateBase.y);
            plateRect.anchoredPosition = rect.anchoredPosition;
            plateRect.SetSiblingIndex(go.transform.GetSiblingIndex());   

            plateGO.GetComponent<LayoutElement>().ignoreLayout = true;

            Image plateImg = plateGO.GetComponent<Image>();
            plateImg.sprite = plateSprite;
            plateImg.preserveAspect = true;
            plateImg.raycastTarget = false;

       
            rect.anchoredPosition += iconOffsetOnPlate;

            go.AddComponent<PlatedItem>().plate = plateGO;
        }
        else
        {
            image.sprite = data.icon;
        }

        if (go.GetComponent<CanvasGroup>() == null)
            go.AddComponent<CanvasGroup>();

        TestDrag drag = go.GetComponent<TestDrag>();
        if (drag == null) drag = go.AddComponent<TestDrag>();
        drag.itemId = data.id;

        TutorialInteractable interactable = go.GetComponent<TutorialInteractable>();
        if (interactable == null) interactable = go.AddComponent<TutorialInteractable>();
        interactable.sourceData = data;

 
        TutorialManager.Instance?.Register(interactable);

        ingredientList?.MarkSpawned(data);

        Debug.Log($"[IngredientSpawner] Spawned '{data.displayName}' (id='{data.id}', category={data.category}, size={data.size}, iconScale={data.iconScale}, plate={usePlate}) into {parent.name}");
    }

    private GameObject SelectPrefab(IngredientData data)
    {
        if (data.category == ItemCategory.Ingredient)
            return data.size == ItemSize.Small ? ingredientSmallPrefab : ingredientLargePrefab;
        else
            return data.size == ItemSize.Small ? utensilSmallPrefab : utensilLargePrefab;
    }
}