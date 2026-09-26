/// <summary>
/// Remembers which LevelData the player tapped on the map, so LevelMapScene
/// can read it on load and display the right recipe. Same pattern as
/// LoadingManager.targetScene - a plain static field surviving the scene
/// change (loading a new scene doesn't reset static fields).
/// </summary>
public static class LevelSelectionManager
{
    public static LevelData SelectedLevel;

    public static void SelectLevel(LevelData level)
    {
        SelectedLevel = level;
    }
}