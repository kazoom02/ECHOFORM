using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// =====================================================
// ECHOFORM — LoadGameRow
// One clickable entry in the Load Game list. Put this on a
// Button prefab with three TMP_Text children:
//   nameLabel     -> the save's name
//   playtimeLabel -> how long the player has played (e.g. "2h 15m")
//   dateLabel     -> the date the save was created
// LoadGameMenu spawns one per save and binds it.
// =====================================================

[RequireComponent(typeof(Button))]
public class LoadGameRow : MonoBehaviour
{
    [Header("Labels")]
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text playtimeLabel;
    [SerializeField] private TMP_Text dateLabel;

    [Tooltip("Date format for the creation date. e.g. \"dd MMM yyyy\" -> 09 Jul 2026")]
    [SerializeField] private string dateFormat = "dd MMM yyyy";

    private Button button;
    private SaveSlot slot;
    private Action<SaveSlot> onClick;

    void Awake() => button = GetComponent<Button>();

    public void Bind(SaveSlot slot, Action<SaveSlot> onClick)
    {
        if (button == null) button = GetComponent<Button>(); // in case Awake hasn't run yet
        if (slot == null || slot.data == null) return;

        this.slot = slot;
        this.onClick = onClick;
        SaveData d = slot.data;

        if (nameLabel != null)
            nameLabel.text = slot.SlotName;

        if (playtimeLabel != null)
            playtimeLabel.text = d.PlayTimeText;

        if (dateLabel != null)
            dateLabel.text = d.CreatedAt == DateTime.MinValue
                ? "—"
                : d.CreatedAt.ToString(dateFormat);

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => this.onClick?.Invoke(this.slot));
    }
}
