using System;
using UnityEngine;
using UnityEngine.UI;

// =====================================================
// ECHOFORM — ShieldController
// Apresenta os escudos do jogador em espaços individuais e atualiza o
// estado visual de cada espaço quando os escudos são ganhos ou consumidos.
// =====================================================

public class ShieldController : MonoBehaviour
{
    [Header("Binding")]
    [SerializeField] private PlayerCombatant player;
    [SerializeField] private bool autoBind = true;

    [Header("Shield slots (index 0 fills first)")]
    [SerializeField] private Image[] slots = new Image[4];

    [Header("Sprites")]
    [Tooltip("Shown on a slot while that shield is active (ShieldActivated).")]
    [SerializeField] private Sprite activeSprite;
    [Tooltip("Shown on an empty slot. Leave empty to hide empty slots instead.")]
    [SerializeField] private Sprite inactiveSprite;

    Action unsubscribe;

    void Awake()
    {
        if (autoBind && player == null)
            player = FindFirstObjectByType<PlayerCombatant>();
    }

    void OnEnable()
    {
        if (player != null)
        {
            player.OnStateChanged += Refresh;
            unsubscribe = () => player.OnStateChanged -= Refresh;
        }
        Refresh();
    }

    void OnDisable()
    {
        unsubscribe?.Invoke();
        unsubscribe = null;
    }

    void Refresh()
    {
        if (slots == null) return;
        int active = player != null ? player.Shields : 0;

        for (int i = 0; i < slots.Length; i++)
        {
            Image img = slots[i];
            if (img == null) continue;

            bool on = i < active;
            if (on)
            {
                if (activeSprite != null) img.sprite = activeSprite;
                img.enabled = true;
            }
            else
            {
                if (inactiveSprite != null) { img.sprite = inactiveSprite; img.enabled = true; }
                else img.enabled = false;
            }
        }
    }
}
