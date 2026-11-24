using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[Serializable]
public class LevelScoreEntry
{
    public int levelNumber;
    public int score;
    public int stars;
}


[Serializable]
public class SaveData
{
    public int coins;
    public int currentLevel;
    public List<int> unlockedLevels = new List<int>();
    public bool tutorialDone;
    public string lastSaved;
    public bool congratsShown;

    [Tooltip("Per-level score/stars actually achieved by the player - NOT ScoreManager's startingScore, that's just the fresh-attempt default before any deductions.")]
    public List<LevelScoreEntry> levelScores = new List<LevelScoreEntry>();
    public List<string> completedTutorials = new List<string>();
    public bool IsTutorialCompleted(string id) => completedTutorials.Contains(id);

    //Premade
    [Tooltip("Ingredient lines the player has already been shown.")]
    public List<string> seenPrepMessages = new List<string>();
    [Tooltip("Level whose message was shown but not yet resolved (left with no star?).")]
    public int pendingPrepLevel;
    public List<string> pendingPrepKeys = new List<string>();

    public bool HasSeenPrep(string key) => seenPrepMessages.Contains(key);
    public void MarkPrepSeen(string key)
    {
        if (!string.IsNullOrEmpty(key) && !seenPrepMessages.Contains(key))
            seenPrepMessages.Add(key);
    }
    public void ForgetPrep(string key) => seenPrepMessages.Remove(key);
   
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

public static class GameSession
{
    public static SaveData Data;          
    public static int CurrentSlot = -1;  
    public static bool HasPendingLoad => Data != null;
    public static void Clear() { Data = null; CurrentSlot = -1; PendingSavedSlotMessage = -1; }

    public static int PendingSavedSlotMessage = -1;   
   

    public static SaveData GetOrCreateData()
    {
        if (Data == null) Data = new SaveData();
        return Data;
    }
}

public static class SaveSystem
{
    public const int SlotCount = 5; 

    private static string PathFor(int slot) =>
        Path.Combine(Application.persistentDataPath, $"savegame_slot{slot}.json");

    
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

    public static void SaveToCurrentSlot(SaveData data)
    {
        if (GameSession.CurrentSlot < 0)
        {
            Debug.Log("Autosave skipped: no save slot chosen yet.");
            return;
        }
        Save(data, GameSession.CurrentSlot);
    }

    public static int PickAutoSaveSlot()
    {
        int lowestSlot = 0;
        int lowestCount = int.MaxValue;


        for(int i = 0; i < SlotCount; i++)
        {
            SaveData d = Load(i);
            if (d == null) return i;

            int count = d.levelScores.Select(e => e.levelNumber).Distinct().Count();
            if (count < lowestCount) { lowestCount = count; lowestSlot = i; }
        }
        return lowestSlot;
    }

}