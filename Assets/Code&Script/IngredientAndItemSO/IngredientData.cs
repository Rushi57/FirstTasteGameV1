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

    public string TutorialId => id;
}