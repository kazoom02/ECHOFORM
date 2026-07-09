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
    [SerializeField] private ScreenSlash screenSlash;           // full-screen cyan slash (fallback streak)
    [SerializeField] private ChargedSlashFX chargedSlashPrefab; // animated slash VFX prefab (preferred)
    [SerializeField] private Transform slashSpawnPoint;         // where the slash spawns (defaults to origin)
    [Tooltip("Only this card (by cardName) triggers the full-screen slash. Spaces/case are ignored.")]
    [SerializeField] private string slashCardName = "Charged Slash";
    [SerializeField] private CpuCycleMeter cpuMeter;
    [SerializeField] private OverloadReadout overloadReadout;   // "COPY #n/10" corruption counter
    [SerializeField] private CombatTargeting targeting;         // enemy picker for single-target chips
    [SerializeField] private VestigeCombatAnimator vestige;     // walk-in melee for attack chips

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

        if (overloadReadout != null)
            overloadReadout.Set(combat != null ? combat.CorruptedInHand : 0);
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
        // deny (shake) unplayable chips up front: glitch, not enough energy, or shields full
        bool cannotPlay = combat != null ? !combat.CanPlayCard(view.card) : view.card.isGlitch;
        if (cannotPlay) { StartCoroutine(Deny(view)); return; }

        // Single-target chips with more than one enemy: let the player pick.
        if (targeting != null && view.card.target == CardTarget.SingleEnemy && CountLivingEnemies() > 1)
        {
            busy = true;                                  // lock the rack while choosing
            targeting.Begin(
                picked => { busy = false; StartCoroutine(InstallRoutine(view, picked)); },
                ()     => { busy = false; }               // cancelled — nothing happens
            );
            return;
        }

        StartCoroutine(InstallRoutine(view, ResolveTarget(view.card)));
    }

    int CountLivingEnemies()
    {
        if (combat == null) return 0;
        int c = 0;
        foreach (var e in combat.Enemies) if (e != null && !e.IsDead) c++;
        return c;
    }

    IEnumerator InstallRoutine(ChipView view, Enemy target)
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
        Vector2 slotPos = neuralSlot.anchoredPosition;
        yield return Move(rt, up, slotPos, slideTime, EaseInOut, rt.localScale, installedScale);

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

        // 4) hand off the chip: hide the install UI and clear it from the slot
        if (installGroup) installGroup.alpha = 0f;
        if (view) Destroy(view.gameObject);
        onChipInstalled?.Invoke(card);                     // SFX / extra fx hook

        // 5) EXECUTE — attack chips send Vestige in and resolve on the hit frame; others resolve in place
        if (card != null && IsSlashCard(card))                 // full-screen slash only for ChargedSlash
        {
            if (chargedSlashPrefab != null)                    // preferred: animated slash VFX
                ChargedSlashFX.Play(chargedSlashPrefab,
                    slashSpawnPoint != null ? slashSpawnPoint.position : Vector3.zero);
            else if (screenSlash != null)
                screenSlash.Slash();                           // fallback: old streak overlay
        }
        if (vestige != null && target != null && CardDealsDamage(card))
        {
            yield return vestige.PlayAttack(target.transform,
                () => { if (combat != null) combat.TryPlayCard(card, target); });
        }
        else
        {
            // self-cast (block/heal) keeps the flash — but the slash card shows its own VFX, no flash
            if (visorFlash && !IsSlashCard(card)) visorFlash.Flash();
            if (combat != null) combat.TryPlayCard(card, target);
        }

        busy = false;
    }

    static bool CardDealsDamage(CardData c)
    {
        if (c == null) return false;
        foreach (var e in c.effects) if (e.type == CardEffectType.DealDamage) return true;
        return false;
    }

    // Matches the slash card ignoring spaces and case, so the asset's
    // display name ("Charged Slash") and the file name ("ChargedSlash")
    // both trigger the effect.
    bool IsSlashCard(CardData c)
    {
        if (c == null || string.IsNullOrEmpty(slashCardName)) return false;
        string a = (c.cardName ?? "").Replace(" ", "").ToLowerInvariant();
        string b = slashCardName.Replace(" ", "").ToLowerInvariant();
        return a == b && a.Length > 0;
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
