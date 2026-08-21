using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// =====================================================
// ECHOFORM — LoadGameRow
// Representa uma entrada selecionável do menu de carregamento e apresenta
// o nome, o tempo de jogo e a data da gravação.
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
        if (button == null) button = GetComponent<Button>();
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
