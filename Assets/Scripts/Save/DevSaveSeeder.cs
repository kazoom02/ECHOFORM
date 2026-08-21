using UnityEngine;

// =====================================================
// ECHOFORM — DevSaveSeeder
// Ferramenta de desenvolvimento que cria e elimina gravações de teste
// para permitir validar a apresentação do menu de carregamento.
// =====================================================

public class DevSaveSeeder : MonoBehaviour
{
    [ContextMenu("Create 6 Test Saves")]
    public void CreateTestSaves()
    {
        SaveSystem.Save(new SaveData {
            slotName = "Vestige — Fight I",
            sceneName = "FirstArea",
            playSeconds = 512f,
            fightIndex = 0, playerHP = 40,
        }, "slot1");

        SaveSystem.Save(new SaveData {
            slotName = "Vestige — Fight I (Clean Cut)",
            sceneName = "FirstArea",
            playSeconds = 205f,
            fightIndex = 0, playerHP = 46,
        }, "slot2");

        SaveSystem.Save(new SaveData {
            slotName = "Vestige — The Merger Race",
            sceneName = "FirstArea",
            playSeconds = 3915f,
            fightIndex = 1, playerHP = 28,
        }, "slot3");

        SaveSystem.Save(new SaveData {
            slotName = "Vestige — Swarm Overload",
            sceneName = "FirstArea",
            playSeconds = 5460f,
            fightIndex = 1, playerHP = 20,
        }, "slot4");

        SaveSystem.Save(new SaveData {
            slotName = "Vestige — Boss: The Prime",
            sceneName = "FirstArea",
            playSeconds = 8130f,
            fightIndex = 2, playerHP = 12,
        }, "slot5");

        SaveSystem.Save(new SaveData {
            slotName = "Vestige — The Loom's Core",
            sceneName = "FirstArea",
            playSeconds = 10980f,
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
