using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// =====================================================
// ECHOFORM — LoadGameMenu
// Lists every saved game from SaveSystem. If there are none,
// it shows the "No games saved" label instead of a list.
// Clicking a save loads its scene; the chosen SaveData is
// stashed in SaveSystem-side PendingLoad so the gameplay
// scene can restore from it.
//
// Inspector wiring:
//   listContent   -> the Content transform of a ScrollView (or any
//                    vertical layout group) that rows are spawned into
//   rowPrefab     -> a Button prefab with a TMP_Text child label
//   noSavesLabel  -> a GameObject with the "No games saved" text
// =====================================================

public class LoadGameMenu : MonoBehaviour
{
    [Header("List")]
    [SerializeField] private Transform listContent;
    [SerializeField] private LoadGameRow rowPrefab;
    [Tooltip("Optional — assign to snap the list back to the top when it opens.")]
    [SerializeField] private ScrollRect scrollRect;

    [Header("Empty state")]
    [SerializeField] private GameObject noSavesLabel;   // "No games saved"

    private readonly List<GameObject> spawnedRows = new List<GameObject>();

    /// <summary>Set by a save row just before the scene loads, so gameplay can restore it.</summary>
    public static SaveData PendingLoad { get; private set; }

    public static SaveData ConsumePendingLoad()
    {
        SaveData data = PendingLoad;
        PendingLoad = null;
        return data;
    }

    public static void ClearPendingLoad() => PendingLoad = null;

    void OnEnable() => Refresh();

    public void Refresh()
    {
        ClearRows();

        List<SaveSlot> saves = SaveSystem.ListSaves();

        bool hasSaves = saves.Count > 0;
        if (noSavesLabel != null) noSavesLabel.SetActive(!hasSaves);
        if (listContent != null) listContent.gameObject.SetActive(hasSaves);

        if (!hasSaves)
        {
            UiSelectionHelper.SelectFirst(gameObject);
            return;
        }

        foreach (SaveSlot slot in saves)
        {
            if (rowPrefab == null || listContent == null) break;
            LoadGameRow row = Instantiate(rowPrefab, listContent);
            row.gameObject.SetActive(true);   // ensure visible even if the prefab was saved inactive
            row.Bind(slot, LoadSlot);
            spawnedRows.Add(row.gameObject);
        }

        ScrollToTop();
        UiSelectionHelper.SelectFirst(listContent != null ? listContent.gameObject : gameObject);
    }

    /// <summary>Snap the list back to the top after it's rebuilt.</summary>
    private void ScrollToTop()
    {
        if (scrollRect == null) return;
        Canvas.ForceUpdateCanvases();                 // make sure the layout is built first
        scrollRect.verticalNormalizedPosition = 1f;   // 1 = top, 0 = bottom
    }

    private void LoadSlot(SaveSlot slot)
    {
        if (slot?.data == null) return;
        PendingLoad = slot.data;
        PlayTimeTracker.EnsureInstance().ResumeFrom(slot.data.playSeconds);

        string scene = string.IsNullOrEmpty(slot.data.sceneName) ? "FirstArea" : slot.data.sceneName;
        Debug.Log($"[Echoform] Loading save '{slot.SlotName}' into {scene}.");
        SceneManager.LoadScene(scene);
    }

    private void ClearRows()
    {
        foreach (GameObject go in spawnedRows)
            if (go != null) Destroy(go);
        spawnedRows.Clear();
    }
}
