using System;
using UnityEngine;
using UnityEngine.UI;

// =====================================================
// ECHOFORM — ShieldController
// Player shields shown as up to N slots (max 4). Each shield
// negates one full enemy hit. When the player gains a shield
// the matching slot swaps to the "active" sprite (ShieldActivated);
// empty slots show the inactive sprite or hide. Binds to the
// PlayerCombatant and refreshes on OnStateChanged.
//
// Setup: make one Image per shield slot, drop this on a HUD
// object, drag the slots into the array (index 0 fills first),
// and assign Active Sprite (+ optional Inactive Sprite).
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
                else img.enabled = false;   // no inactive art -> just hide the slot
            }
        }
    }
}
