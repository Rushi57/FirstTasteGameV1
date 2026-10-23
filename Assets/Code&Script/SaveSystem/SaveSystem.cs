using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>One level's best result so far - score and star rating actually achieved.</summary>
[Serializable]
public class LevelScoreEntry
{
    public int levelNumber;
    public int score;
    public int stars;
}

// Everything you want to remember goes in here. Add or remove fields as needed.
[Serializable]
public class SaveData
{
    public int coins;
    public int currentLevel;
    public List<int> unlockedLevels = new List<int>();
    public bool tutorialDone;
    public string lastSaved;

    [Tooltip("Per-level score/stars actually achieved by the player - NOT ScoreManager's startingScore, that's just the fresh-attempt default before any deductions.")]
    public List<LevelScoreEntry> levelScores = new List<LevelScoreEntry>();
    public List<string> completedTutorials = new List<string>();
    public bool IsTutorialCompleted(string id) => completedTutorials.Contains(id);
    /// <summary>
    /// Records a level's result. By default only overwrites if the new score
    /// is BETTER than any previous attempt (so retrying a level worse than
    /// before doesn't erase your best run) - pass keepBest: false if you'd
    /// rather always save the most recent attempt instead.
    /// </summary>
    public void SetLevelResult(int levelNumber, int score, int stars, bool keepBest = true)
    {
        LevelScoreEntry entry = levelScores.Find(e => e.levelNumber == levelNumber);

        if (entry == null)
        {
            levelScores.Add(new LevelScoreEntry { levelNumber = levelNumber, score = score, stars = stars });
            return;
        }

        if (!keepBest || score > entry.score)
        {
            entry.score = score;
            entry.stars = stars;
        }
    }

    /// <summary>Returns the saved result for a level, or null if it's never been completed.</summary>
    public LevelScoreEntry GetLevelResult(int levelNumber)
    {
        return levelScores.Find(e => e.levelNumber == levelNumber);
    }
    public int GetLevelStars(int levelNumber)
    {
        LevelScoreEntry entry = GetLevelResult(levelNumber);
        return entry != null ? entry.stars : 0;
    }
    public void MarkTutorialCompleted(string id)
    {
        if (!string.IsNullOrEmpty(id) && !completedTutorials.Contains(id))
            completedTutorials.Add(id);
    }
}



// Holds the loaded data while switching from MainMenu to MapScene.
public static class GameSession
{
    public static SaveData Data;          // null = new game
    public static int CurrentSlot = -1;   // NEW: which slot was loaded / last saved
    public static bool HasPendingLoad => Data != null;
    public static void Clear() { Data = null; CurrentSlot = -1; }

    public static SaveData GetOrCreateData()
    {
        if (Data == null) Data = new SaveData();
        return Data;
    }
}

public static class SaveSystem
{
    public const int SlotCount = 5; // must match the number of LoadButtons in your list

    private static string PathFor(int slot) =>
        Path.Combine(Application.persistentDataPath, $"savegame_slot{slot}.json");

    // Old single-file save -> moves into slot 0 so players don't lose it
    private static string LegacyPath => Path.Combine(Application.persistentDataPath, "savegame.json");
    private static void MigrateLegacy()
    {
        if (File.Exists(LegacyPath) && !File.Exists(PathFor(0)))
            File.Move(LegacyPath, PathFor(0));
    }

    public static bool HasSave(int slot)
    {
        MigrateLegacy();
        return File.Exists(PathFor(slot));
    }

    public static bool AnySaveExists()
    {
        for (int i = 0; i < SlotCount; i++)
            if (HasSave(i)) return true;
        return false;
    }

    public static void Save(SaveData data, int slot)
    {
        try
        {
            data.lastSaved = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            string json = JsonUtility.ToJson(data, true);

            string path = PathFor(slot);
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, json);
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);

            Debug.Log($"Game saved to slot {slot}: {path}");
        }
        catch (Exception e)
        {
            Debug.LogError("Save failed: " + e.Message);
        }
    }

    public static SaveData Load(int slot)
    {
        if (!HasSave(slot)) return null;
        try
        {
            return JsonUtility.FromJson<SaveData>(File.ReadAllText(PathFor(slot)));
        }
        catch (Exception e)
        {
            Debug.LogError("Load failed: " + e.Message);
            return null;
        }
    }

    public static void DeleteSave(int slot)
    {
        if (HasSave(slot)) File.Delete(PathFor(slot));
    }

    /// <summary>
    /// Autosave helper: writes to the slot the player loaded or last saved to.
    /// If the player has never picked a slot (new game), it does nothing, and the
    /// data stays in memory (GameSession.Data) until they save manually.
    /// </summary>
    public static void SaveToCurrentSlot(SaveData data)
    {
        if (GameSession.CurrentSlot < 0)
        {
            Debug.Log("Autosave skipped: no save slot chosen yet.");
            return;
        }
        Save(data, GameSession.CurrentSlot);
    }
}