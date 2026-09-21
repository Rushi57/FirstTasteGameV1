using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Everything you want to remember goes in here. Add or remove fields as needed.
[Serializable]
public class SaveData
{
    public int coins;
    public int currentLevel;
    public List<int> unlockedLevels = new List<int>();
    public bool tutorialDone;
    public string lastSaved;
}

// Holds the loaded data while switching from MainMenu to MapScene.
public static class GameSession
{
    public static SaveData Data;          // null = new game
    public static bool HasPendingLoad => Data != null;
    public static void Clear() => Data = null;
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