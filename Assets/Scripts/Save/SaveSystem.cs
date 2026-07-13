using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// =====================================================
// ECHOFORM — SaveSystem
// Lightweight foundation for save slots. Right now it just
// lists and loads slots stored as JSON in persistentDataPath;
// there are no saves yet, so LoadGameMenu will correctly show
// "No games saved" until you call SaveSystem.Save(...).
//
// Later, fill SaveData with real run state (deck, HP, fight
// index, etc.) and call SaveSystem.Save() from your combat/run
// code. The menu layer doesn't need to change.
// =====================================================

[Serializable]
public class SaveData
{
    public int saveVersion = 1;
    public string slotName = "New Run";   // shown in the Load menu
    public string sceneName = "FirstArea"; // scene to resume into
    public string createdAtIso = "";       // when the run was first created (ISO 8601)
    public string savedAtIso = "";         // when it was last saved (ISO 8601)
    public float playSeconds = 0f;         // total time played, in seconds
    public int fightIndex = 0;             // example run state — expand freely
    public bool hasPlayerState = false;
    public int playerHP = 0;
    public int playerShields = 0;
    public int playerFocus = 0;
    public bool hasPlayerPosition = false;
    public float playerX = 0f;
    public float playerY = 0f;
    public float playerZ = 0f;

    public DateTime CreatedAt =>
        DateTime.TryParse(createdAtIso, out var dt) ? dt : DateTime.MinValue;

    public DateTime SavedAt =>
        DateTime.TryParse(savedAtIso, out var dt) ? dt : DateTime.MinValue;

    /// <summary>Playtime formatted like "2h 15m" or "8m 03s".</summary>
    public string PlayTimeText
    {
        get
        {
            var t = TimeSpan.FromSeconds(Mathf.Max(0f, playSeconds));
            if (t.TotalHours >= 1) return $"{(int)t.TotalHours}h {t.Minutes:00}m";
            if (t.TotalMinutes >= 1) return $"{t.Minutes}m {t.Seconds:00}s";
            return $"{t.Seconds}s";
        }
    }
}

/// <summary>One save file on disk: its data plus the file path it came from.</summary>
public class SaveSlot
{
    public string filePath;
    public SaveData data;

    public string SlotName => data != null ? data.slotName : Path.GetFileNameWithoutExtension(filePath);
}

public static class SaveSystem
{
    private const string Folder = "saves";
    private const string Extension = ".json";
    private const string CurrentRunId = "current-run";

    private static string SaveDir => Path.Combine(Application.persistentDataPath, Folder);

    /// <summary>All saved games on disk, newest first. Empty list = no saves.</summary>
    public static List<SaveSlot> ListSaves()
    {
        var slots = new List<SaveSlot>();
        if (!Directory.Exists(SaveDir)) return slots;

        foreach (string file in Directory.GetFiles(SaveDir, "*" + Extension))
        {
            try
            {
                string json = File.ReadAllText(file);
                SaveData data = JsonUtility.FromJson<SaveData>(json);
                if (data == null) continue;
                data.slotName = $"Area {Mathf.Max(0, data.fightIndex) + 1}";
                slots.Add(new SaveSlot { filePath = file, data = data });
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Echoform] Skipping unreadable save '{file}': {e.Message}");
            }
        }

        slots.Sort((a, b) => b.data.SavedAt.CompareTo(a.data.SavedAt)); // newest first
        return slots;
    }

    public static bool HasAnySave() => ListSaves().Count > 0;

    /// <summary>Write a save. Pass a stable id (e.g. "slot1" or a run guid) to overwrite a slot.</summary>
    public static void Save(SaveData data, string saveId = null)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));

        Directory.CreateDirectory(SaveDir);
        if (string.IsNullOrEmpty(saveId)) saveId = Guid.NewGuid().ToString("N");

        string path = Path.Combine(SaveDir, saveId + Extension);
        string now = DateTime.Now.ToString("o");

        if (string.IsNullOrEmpty(data.createdAtIso) && File.Exists(path))
        {
            SaveData previous = Load(path);
            if (previous != null) data.createdAtIso = previous.createdAtIso;
        }

        if (string.IsNullOrEmpty(data.createdAtIso)) data.createdAtIso = now; // set once, on first save
        data.savedAtIso = now;

        File.WriteAllText(path, JsonUtility.ToJson(data, true));
        Debug.Log($"[Echoform] Saved '{data.slotName}' to {path}");
    }

    /// <summary>
    /// ECHOFORM has one active run. Every Area overwrites this same file, and
    /// legacy/test slots are removed so Load Game always shows one row.
    /// </summary>
    public static void SaveCurrentRun(SaveData data)
    {
        data.slotName = $"Area {Mathf.Max(0, data.fightIndex) + 1}";
        Save(data, CurrentRunId);

        string currentPath = Path.Combine(SaveDir, CurrentRunId + Extension);
        foreach (string file in Directory.GetFiles(SaveDir, "*" + Extension))
            if (!string.Equals(file, currentPath, StringComparison.OrdinalIgnoreCase))
                Delete(file);
    }

    /// <summary>Read a save back from its file path (from a SaveSlot). Null if it fails.</summary>
    public static SaveData Load(string filePath)
    {
        try
        {
            return JsonUtility.FromJson<SaveData>(File.ReadAllText(filePath));
        }
        catch (Exception e)
        {
            Debug.LogError($"[Echoform] Failed to load save '{filePath}': {e.Message}");
            return null;
        }
    }

    public static void Delete(string filePath)
    {
        try { if (File.Exists(filePath)) File.Delete(filePath); }
        catch (Exception e) { Debug.LogWarning($"[Echoform] Could not delete save '{filePath}': {e.Message}"); }
    }

    public static void DeleteAll()
    {
        if (!Directory.Exists(SaveDir)) return;

        foreach (string file in Directory.GetFiles(SaveDir, "*" + Extension))
            Delete(file);
    }
}
