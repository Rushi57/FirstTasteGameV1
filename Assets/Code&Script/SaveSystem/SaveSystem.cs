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
}

// Holds the loaded data while switching from MainMenu to MapScene.
public static class GameSession
{
    public static SaveData Data;          // null = new game
    public static bool HasPendingLoad => Data != null;
    public static void Clear() => Data = null;

    /// <summary>Returns Data, creating a fresh SaveData if this is a new game (Data is currently null).</summary>
    public static SaveData GetOrCreateData()
    {
        if (Data == null) Data = new SaveData();
        return Data;
    }
}

public static class SaveSystem
{
    // persistentDataPath works on Android, iOS, and PC
    private static string FilePath => Path.Combine(Application.persistentDataPath, "savegame.json");

    public static bool HasSave() => File.Exists(FilePath);

    public static void Save(SaveData data)
    {
        try
        {
            data.lastSaved = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            string json = JsonUtility.ToJson(data, true);

            // write to a temp file first so a crash mid-save can't corrupt the real file
            string tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, json);
            if (File.Exists(FilePath)) File.Delete(FilePath);
            File.Move(tmp, FilePath);

            Debug.Log("Game saved: " + FilePath);
        }
        catch (Exception e)
        {
            Debug.LogError("Save failed: " + e.Message);
        }
    }

    public static SaveData Load()
    {
        if (!HasSave()) return null;
        try
        {
            string json = File.ReadAllText(FilePath);
            return JsonUtility.FromJson<SaveData>(json);
        }
        catch (Exception e)
        {
            Debug.LogError("Load failed: " + e.Message);
            return null;
        }
    }

    public static void DeleteSave()
    {
        if (HasSave()) File.Delete(FilePath);
    }
}