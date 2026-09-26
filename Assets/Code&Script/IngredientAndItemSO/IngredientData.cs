using System.Collections.Generic;
using UnityEngine;

/// <summary>Whether this is a cooking ingredient or a utensil/tool.</summary>
public enum ItemCategory
{
    Ingredient,
    Utensil
}

/// <summary>Which sized prefab this item should spawn as.</summary>
public enum ItemSize
{
    Small,
    Large
}

public enum IngredientPrepState
{
    Whole,
    Sliced,
    Minced
}

public class PrepStateSprite
{
    public IngredientPrepState state;
    public Sprite sprite;
}

/// <summary>
/// Data-only definition of an ingredient (or utensil - reuse/rename as needed).
/// Create instances: right-click in Project window -> Create -> Game -> Ingredient Data.
///
/// Implements ITutorialIdentifiable so TutorialInteractable.sourceData can
/// pull its tutorial id straight from here - one source of truth, no more
/// retyping the same string in multiple places.
/// </summary>
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

    [Header("Prep States (optional)")]
    [Tooltip("Sprite to show for each prep stage, e.g. a different look for Whole vs Sliced vs Minced garlic. Leave empty if this ingredient never changes appearance (e.g. water, a utensil).")]
    public List<PrepStateSprite> stateSprites = new List<PrepStateSprite>();

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
}