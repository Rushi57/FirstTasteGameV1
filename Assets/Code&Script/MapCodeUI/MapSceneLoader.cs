using UnityEngine;

// Put on any object in MapScene (e.g. CodeForUIGameObject or a new empty "SaveLoader").
public class MapSceneLoader : MonoBehaviour
{
    private void Start()
    {
        if (!GameSession.HasPendingLoad) return; // new game, nothing to apply

        SaveData d = GameSession.Data;

        // TODO: push the saved values back into YOUR game systems
        // PlayerStats.Instance.coins = d.coins;
        // LevelManager.Instance.SetCurrentLevel(d.currentLevel);
        // LevelManager.Instance.SetUnlocked(d.unlockedLevels);
        // TutorialManager.IsFinished = d.tutorialDone;

        Debug.Log($"Loaded save from {d.lastSaved} | coins: {d.coins}, level: {d.currentLevel}");

        GameSession.Clear();
    }
}