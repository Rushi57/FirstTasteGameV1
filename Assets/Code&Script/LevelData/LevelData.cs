using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Identity")]
    public int levelNumber;
    public string levelName;

    [Header("Recipe")]
    [Tooltip("The recipe this level teaches/tests")]
    public RecipeData recipe;

    [Header("Optional")]
    public Sprite levelPreviewImage;

    public DishStorySO story;

    [Header("Level Completion")]
    [Tooltip("Reflection pages shown after finishing this level.")]
    public DishReflectionData reflection;

    [Header("FAQ")]
    [Tooltip("Dish image for the FAQ. If empty, levelPreviewImage is used.")]
    public Sprite faqDishImage;
    public FAQEntry[] faqEntries;
}