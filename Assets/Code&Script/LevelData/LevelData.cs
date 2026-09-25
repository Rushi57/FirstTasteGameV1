using UnityEngine;

/// <summary>
/// One level's data: which recipe it teaches/tests. Create instances:
/// right-click in Project window -> Create -> Game -> Level Data.
/// </summary>
[CreateAssetMenu(fileName = "LevelData", menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Identity")]
    public int levelNumber;
    public string levelName; // e.g. "Adobo", "Dinuguan"

    [Header("Recipe")]
    [Tooltip("The recipe this level teaches/tests - drives the ingredients list and TableZone validation once the level scene loads.")]
    public RecipeData recipe;

    [Header("Optional")]
    public Sprite levelPreviewImage;

    [Header("Optional")]
    public Sprite ScoreStar;

}