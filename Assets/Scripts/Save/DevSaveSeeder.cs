using UnityEngine;

// =====================================================
// ECHOFORM — DevSaveSeeder (development tool)
// Creates fake saves so you can see and style the Load Game
// list without playing through the game. Put this on any
// GameObject, then use the component's right-click (⋮) context
// menu in the Inspector — works in Edit mode, no Play needed.
//
// Delete this script before shipping.
// =====================================================

public class DevSaveSeeder : MonoBehaviour
{
    [ContextMenu("Create 6 Test Saves")]
    public void CreateTestSaves()
    {
        SaveSystem.Save(new SaveData {
            slotName = "Vestige — Fight I",
            sceneName = "FirstArea",
            playSeconds = 512f,     // 8m 32s
            fightIndex = 0, playerHP = 40,
        }, "slot1");

        SaveSystem.Save(new SaveData {
            slotName = "Vestige — Fight I (Clean Cut)",
            sceneName = "FirstArea",
            playSeconds = 205f,     // 3m 25s
            fightIndex = 0, playerHP = 46,
        }, "slot2");

        SaveSystem.Save(new SaveData {
            slotName = "Vestige — The Merger Race",
            sceneName = "FirstArea",
            playSeconds = 3915f,    // ~1h 05m
            fightIndex = 1, playerHP = 28,
        }, "slot3");

        SaveSystem.Save(new SaveData {
            slotName = "Vestige — Swarm Overload",
            sceneName = "FirstArea",
            playSeconds = 5460f,    // ~1h 31m
            fightIndex = 1, playerHP = 20,
        }, "slot4");

        SaveSystem.Save(new SaveData {
            slotName = "Vestige — Boss: The Prime",
            sceneName = "FirstArea",
            playSeconds = 8130f,    // ~2h 15m
            fightIndex = 2, playerHP = 12,
        }, "slot5");

        SaveSystem.Save(new SaveData {
            slotName = "Vestige — The Loom's Core",
            sceneName = "FirstArea",
            playSeconds = 10980f,   // ~3h 03m
            fightIndex = 3, playerHP = 8,
        }, "slot6");

        Debug.Log("[Echoform] Created 6 test saves. Open the Load menu to see them.");
    }

    [ContextMenu("Clear All Saves")]
    public void ClearAllSaves()
    {
        foreach (SaveSlot slot in SaveSystem.ListSaves())
            SaveSystem.Delete(slot.filePath);
        Debug.Log("[Echoform] Cleared all saves.");
    }
}
