using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

// =====================================================
// ECHOFORM — NeuralInterfaceHUD
// The bottom OS-style HUD. Syncs the chip rack + CPU cycles
// from CombatManager, and on click runs the install sequence:
//   eject  ->  slide into the Neural Slot  ->  INSTALLING MEMORY...
//   ->  visor flash  ->  resolve the card (Vestige dashes & attacks).
//
// Assign `combat` to auto-sync hand + energy. Leave it empty to
// test the animation standalone (onChipInstalled still fires).
// =====================================================

[System.Serializable] public class CardEvent : UnityEvent<CardData> { }

public class NeuralInterfaceHUD : MonoBehaviour
{
    [Header("Combat")]
    [SerializeField] private CombatManager combat;
    [SerializeField] private int maxCycles = 3;   // matches CombatManager.energyPerTurn

    [Header("Rack")]
    [Tooltip("Empty RectTransforms placed over each bay, left to right.")]
    [SerializeField] private RectTransform[] rackSlots;
    [SerializeField] private ChipView chipPrefab;

    [Header("Neural slot")]
    [SerializeField] private RectTransform neuralSlot;             // target the chip slides into
    [SerializeField] private Vector3 installedScale = Vector3.one; // chip scale once seated
    [SerializeField] private CanvasGroup installGroup;            // wraps label + bar; alpha 0 at rest
    [SerializeField] private TMP_Text installLabel;               // "INSTALLING MEMORY..."
    [SerializeField] private Image installBar;                    // Image Type = Filled, Horizontal

    [Header("FX")]
    [SerializeField] private VisorFlash visorFlash;
    [SerializeField] private CpuCycleMeter cpuMeter;

    [Header("Timing (seconds)")]
    [SerializeField] private float ejectHeight = 60f;
    [SerializeField] private float ejectTime   = 0.12f;
    [SerializeField] private float slideTime   = 0.25f;
    [SerializeField] private float installTime = 0.80f;
    [SerializeField] private float settleTime  = 0.15f;

    [Header("Events")]
    [Tooltip("Fires the moment the memory is installed — hook Vestige's dash+attack, SFX, screen shake.")]
    public CardEvent onChipInstalled;

    readonly List<ChipView> rack = new List<ChipView>();
    bool busy;

    // ---------------------------------------------------------------- lifecycle
    void OnEnable()  { if (combat != null) combat.OnCombatChanged += Refresh; }
    void OnDisable() { if (combat != null) combat.OnCombatChanged -= Refresh; }
    void Start()     { if (installGroup) installGroup.alpha = 0f; Refresh(); }

    // ---------------------------------------------------------------- rack sync
    public void Refresh()
    {
        foreach (var v in rack) if (v) Destroy(v.gameObject);
        rack.Clear();

        var hand = (combat != null && combat.Deck != null) ? combat.Deck.Hand : null;
        if (hand != null)
            for (int i = 0; i < rackSlots.Length && i < hand.Count; i++)
                Spawn(hand[i], rackSlots[i]);

        if (cpuMeter != null)
            cpuMeter.Set(combat != null ? combat.Energy : maxCycles, maxCycles);
    }

    void Spawn(CardData card, RectTransform slot)
    {
        ChipView v = Instantiate(chipPrefab, slot);
        v.Rect.anchoredPosition = Vector2.zero;
        v.Rect.localScale = Vector3.one;
        v.Bind(card);
        v.Clicked += OnChipClicked;
        rack.Add(v);
    }

    // ---------------------------------------------------------------- play flow
    void OnChipClicked(ChipView view)
    {
        if (busy || view == null || view.card == null) return;
        bool tooExpensive = combat != null && view.card.energyCost > combat.Energy;
        if (view.card.isGlitch || tooExpensive) { StartCoroutine(Deny(view)); return; }
        StartCoroutine(InstallRoutine(view));
    }

    IEnumerator InstallRoutine(ChipView view)
    {
        busy = true;
        CardData card = view.card;               // cache — the view gets destroyed later
        rack.Remove(view);

        // lift the chip out of its bay into the neural slot's coordinate space
        RectTransform layer = (RectTransform)neuralSlot.parent;
        RectTransform rt = view.Rect;
        rt.SetParent(layer, worldPositionStays: true);
        rt.SetAsLastSibling();

        // 1) EJECT — pop upward out of the rack
        Vector2 from = rt.anchoredPosition;
        Vector2 up   = from + Vector2.up * ejectHeight;
        yield return Move(rt, from, up, ejectTime, EaseOut);

        // 2) SLIDE — travel to the neural slot and scale to fit
        Vector2 target = neuralSlot.anchoredPosition;
        yield return Move(rt, up, target, slideTime, EaseInOut, rt.localScale, installedScale);

        // 3) INSTALLING MEMORY... — fill the progress bar with animated dots
        if (installGroup) installGroup.alpha = 1f;
        float t = 0f, dotT = 0f; int dots = 0;
        while (t < installTime)
        {
            t += Time.deltaTime;
            if (installBar) installBar.fillAmount = Mathf.Clamp01(t / installTime);
            dotT += Time.deltaTime;
            if (dotT >= 0.2f) { dotT = 0f; dots = (dots + 1) % 4; if (installLabel) installLabel.text = "INSTALLING MEMORY" + new string('.', dots); }
            yield return null;
        }
        if (installBar) installBar.fillAmount = 1f;

        // 4) VISOR FLASH
        if (visorFlash) visorFlash.Flash();
        yield return new WaitForSeconds(settleTime);

        // 5) RESOLVE — clear UI, trigger Vestige's attack, then apply card effects
        if (installGroup) installGroup.alpha = 0f;
        if (view) Destroy(view.gameObject);

        onChipInstalled?.Invoke(card);                                   // Vestige dashes in & strikes
        if (combat != null) combat.TryPlayCard(card, ResolveTarget(card)); // spends energy, resolves, re-syncs rack

        busy = false;
    }

    /// <summary>Which enemy a single-target card hits. Override for click-to-target.</summary>
    protected virtual Enemy ResolveTarget(CardData card)
    {
        if (combat == null || card.target != CardTarget.SingleEnemy) return null;
        foreach (var e in combat.Enemies) if (e != null && !e.IsDead) return e;
        return null;
    }

    // little sideways shake when a chip can't be played
    IEnumerator Deny(ChipView view)
    {
        RectTransform rt = view.Rect;
        Vector2 home = rt.anchoredPosition;
        float t = 0f;
        while (t < 0.25f) { t += Time.deltaTime; rt.anchoredPosition = home + Vector2.right * Mathf.Sin(t * 60f) * 8f; yield return null; }
        rt.anchoredPosition = home;
    }

    // ---------------------------------------------------------------- tween utils
    delegate float Ease(float x);
    static float EaseOut(float x)   => 1f - (1f - x) * (1f - x);
    static float EaseInOut(float x) => x < 0.5f ? 2f * x * x : 1f - Mathf.Pow(-2f * x + 2f, 2f) / 2f;

    IEnumerator Move(RectTransform rt, Vector2 a, Vector2 b, float dur, Ease ease, Vector3? sa = null, Vector3? sb = null)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = ease(Mathf.Clamp01(t / dur));
            rt.anchoredPosition = Vector2.LerpUnclamped(a, b, p);
            if (sa.HasValue && sb.HasValue) rt.localScale = Vector3.LerpUnclamped(sa.Value, sb.Value, p);
            yield return null;
        }
        rt.anchoredPosition = b;
        if (sb.HasValue) rt.localScale = sb.Value;
    }
}
