using UnityEngine;

/// <summary>
/// Put this on each LevelIcon instance on the map (Level 1, Level 2, ... Level 10).
/// Assign a different LevelData per icon. Wire its SelectLevel() into the
/// icon's OnClick() ALONGSIDE your existing SceneChanger.OnClickPlay entry -
/// same pattern as spawning multiple ingredients from one shared spawner.
/// </summary>
public class LevelIconButton : MonoBehaviour
{
    [Tooltip("Which level this specific icon represents. Level 1 icon gets the Adobo LevelData, Level 2 icon gets Dinuguan, etc.")]
    public LevelData levelData;

    /// <summary>Wire this into the icon's OnClick(), before/alongside SceneChanger.OnClickPlay.</summary>
    public void SelectLevel()
    {
        if (levelData == null)
        {
            Debug.LogWarning($"[LevelIconButton] {gameObject.name} has no LevelData assigned.");
            return;
        }

        LevelSelectionManager.SelectLevel(levelData);
        Debug.Log($"[LevelIconButton] Selected Level {levelData.levelNumber}: {levelData.levelName}");
    }
}