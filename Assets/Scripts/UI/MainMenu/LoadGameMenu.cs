using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// =====================================================
// ECHOFORM — LoadGameMenu
// Preenche o menu de carregamento com as gravações disponíveis e restaura
// a partida selecionada na cena correspondente.
// =====================================================

public class LoadGameMenu : MonoBehaviour
{
    [Header("List")]
    [SerializeField] private Transform listContent;
    [SerializeField] private LoadGameRow rowPrefab;
    [Tooltip("Optional — assign to snap the list back to the top when it opens.")]
    [SerializeField] private ScrollRect scrollRect;

    [Header("Empty state")]
    [SerializeField] private GameObject noSavesLabel;

    private readonly List<GameObject> spawnedRows = new List<GameObject>();

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
            row.gameObject.SetActive(true);
            row.Bind(slot, LoadSlot);
            spawnedRows.Add(row.gameObject);
        }

        ScrollToTop();
        UiSelectionHelper.SelectFirst(listContent != null ? listContent.gameObject : gameObject);
    }

        private void ScrollToTop()
    {
        if (scrollRect == null) return;
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 1f;
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
