using System;
using UnityEngine;
using UnityEngine.UI;

// =====================================================
// ECHOFORM — BatteryBar
// Player health shown as N battery cells (default 4). HP is
// split equally, so each cell = 100/N %% of max HP. A cell's
// FILL overlay starts at 100%% and drains as the player takes
// damage; cells empty from index 0 upward as HP drops. When a
// cell reaches 0 its background sprite swaps to the "depleted"
// sprite you pick per cell in the Inspector, and restores to
// its normal frame if you heal past it.
//
// Setup: one Background + Fill Image per cell (the Fill must be
// Image Type = Filled). Drop this on a HUD object, set
// Batteries size = 4, wire each cell and its depleted sprite.
// =====================================================

public class BatteryBar : MonoBehaviour
{
    [Serializable]
    public class Cell
    {
        [Tooltip("The Filled Image overlay (starts full, drains with damage).")]
        public Image fill;
        [Tooltip("The frame/background Image (like HealthBarBackground).")]
        public Image background;
        [Tooltip("Background sprite shown once this cell is fully drained.")]
        public Sprite depletedSprite;

        [NonSerialized] public Sprite normalSprite;   // captured at startup
        [NonSerialized] public float shown;           // lerp state
    }

    [Header("Binding")]
    [SerializeField] private PlayerCombatant player;
    [SerializeField] private bool autoBind = true;

    [Header("Batteries (index 0 drains first)")]
    [SerializeField] private Cell[] batteries = new Cell[4];

    [Header("Feel")]
    [Tooltip("How fast each fill chases its value. 0 = snap.")]
    [SerializeField] private float lerpSpeed = 8f;
    [Tooltip("Hide the fill graphic when its cell is fully drained.")]
    [SerializeField] private bool hideEmptyFill = true;

    Action unsubscribe;

    void Awake()
    {
        if (autoBind && player == null)
            player = FindFirstObjectByType<PlayerCombatant>();

        // remember each cell's starting background sprite so we can restore it on heal
        if (batteries != null)
            foreach (var c in batteries)
                if (c != null && c.background != null)
                    c.normalSprite = c.background.sprite;
    }

    void OnEnable()
    {
        if (player != null)
        {
            player.OnStateChanged += OnChanged;
            unsubscribe = () => player.OnStateChanged -= OnChanged;
        }
        SnapAll();
    }

    void OnDisable()
    {
        unsubscribe?.Invoke();
        unsubscribe = null;
    }

    void OnChanged() { /* fills + swaps are applied each frame in Update */ }

    int Count => batteries != null ? batteries.Length : 0;

    // Fraction (0..1) this cell should show, given equal HP bands.
    float CellTarget(int i)
    {
        if (player == null || player.MaxHP <= 0) return 0f;
        int n = Mathf.Max(1, Count);
        float hpFrac = Mathf.Clamp01(player.CurrentHP / (float)player.MaxHP);
        return Mathf.Clamp01(hpFrac * n - i);
    }

    void SnapAll()
    {
        for (int i = 0; i < Count; i++)
            if (batteries[i] != null) batteries[i].shown = CellTarget(i);
        Apply(true);
    }

    void Update()
    {
        if (player == null) return;
        Apply(false);
    }

    void Apply(bool snap)
    {
        for (int i = 0; i < Count; i++)
        {
            var c = batteries[i];
            if (c == null) continue;

            float target = CellTarget(i);
            c.shown = (snap || lerpSpeed <= 0f)
                ? target
                : Mathf.MoveTowards(c.shown, target, lerpSpeed * Time.deltaTime);

            if (c.fill != null)
            {
                c.fill.fillAmount = c.shown;
                if (hideEmptyFill) c.fill.enabled = c.shown > 0.001f;
            }

            if (c.background != null)
            {
                // swap on the true target so it flips exactly at the band boundary
                Sprite s = target <= 0.0001f ? c.depletedSprite : c.normalSprite;
                if (s != null && c.background.sprite != s) c.background.sprite = s;
            }
        }
    }
}
