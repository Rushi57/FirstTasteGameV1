using System.Collections.Generic;
using UnityEngine;

public enum ItemCategory
{
    Ingredient,
    Utensil
}
public enum ItemSize
{
    Small,
    Large
}

public enum IngredientPrepState
{
    Whole,
    Sliced,
    Minced,
    Cooked
}

[System.Serializable]
public class PrepStateSprite
{
    public IngredientPrepState state;
    public Sprite sprite;
}

[CreateAssetMenu(fileName = "IngredientData", menuName = "Game/Ingredient Data")]
public class IngredientData : ScriptableObject, ITutorialIdentifiable
{
    [Header("Identity")]
    [Tooltip("Unique id for this ingredient. Used by TestDrop matching AND by the tutorial system.")]
    public string id;

    public string displayName;


    [Header("Spawning")]
    [Tooltip("Whether this is an ingredient or a cooking utensil/tool - determines which prefab IngredientSpawner uses.")]
    public ItemCategory category = ItemCategory.Ingredient;

    [Tooltip("Small or large sized prefab.")]
    public ItemSize size = ItemSize.Small;

    [Header("Visual")]
    public Sprite icon;

    [Tooltip("If true, this ingredient is served on a plate while on the table (meat, garlic, etc). Leave off for pitchers, bottles and utensils.")]
    public bool needsPlate = false;

    [Tooltip("Multiplies the prefab's size for this item. 1 = full prefab size, 0.5 = half. Use it to make small things like garlic look smaller than pork or a pitcher.")]
    [Range(0.2f, 1.5f)]
    public float iconScale = 1f;

    [Header("Prep States (optional)")]
    [Tooltip("Sprite to show different look")]
    public List<PrepStateSprite> stateSprites = new List<PrepStateSprite>();

    [Header("Pour Animation (optional)")]
    [Tooltip("If true, dropping this item on a target plays")]
    public bool playsPourAnimation = false;

    [Tooltip("Only used when Plays Pour Animation")]
    public List<PourPositionEntry> pourPositions = new List<PourPositionEntry>();

    [Header("State Change On Drop (optional)")]
    [Tooltip("When this item is dropped on a matching target")]
    public List<DropStateChange> dropStateChanges = new List<DropStateChange>();
    public string TutorialId => id;

    public Sprite GetSpriteForState(IngredientPrepState state)
    {
        foreach (var entry in stateSprites)
        {
            if (entry.state == state)
                return entry.sprite != null ? entry.sprite : icon;
        }
        return icon;
    }

    public bool TryGetPourPosition(string targetName, out Vector2 position)
    {
        position = default;
        if (!playsPourAnimation || pourPositions == null || pourPositions.Count == 0)
            return false;

        foreach (var entry in pourPositions)
        {
            if (entry.targetName == targetName)
            {
                position = entry.position;
                return true;
            }
        }
        Debug.LogWarning($"[Pour] '{id}' has no pour position for '{targetName}'. Using '{pourPositions[0].targetName}'.");
        position = pourPositions[0].position;
        return true;
    }
    public bool TryGetStateOnDrop(string targetName, out IngredientPrepState newState)
    {
        newState = default;
        if (dropStateChanges == null) return false;

        foreach (var entry in dropStateChanges)
        {
            if (entry.targetName == targetName)
            {
                newState = entry.newState;
                return true;
            }
        }
        return false;
    }
}
[System.Serializable]
public class PourPositionEntry
{
    [Tooltip("Where the pour happens, e.g. Pan, Bowl, Pitcher. Must match the name the drop target passes in.")]
    public string targetName = "Pan";

    [Tooltip("PosX / PosY of 'Pitcher with water' (anchoredPosition) for this target.")]
    public Vector2 position;
}

[System.Serializable]
public class DropStateChange
{
    [Tooltip("Name the drop target passes in, e.g. Wok. Must match exactly.")]
    public string targetName = "Wok";

    [Tooltip("The state the ingredient becomes when dropped on this target.")]
    public IngredientPrepState newState = IngredientPrepState.Cooked;
}