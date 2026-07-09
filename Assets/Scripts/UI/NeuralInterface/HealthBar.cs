using UnityEngine;
using UnityEngine.UI;
using TMPro;

// =====================================================
// ECHOFORM — HealthBar
// One bar for both sides. Drop it under a PlayerCombatant
// or an Enemy and it auto-finds the combatant in its parents
// and subscribes to OnStateChanged — no per-instance wiring,
// so every split child gets its own bar for free.
//
// Setup: fill = an Image with Image Type = Filled, Fill Method
// = Horizontal (use InstallBar_Fill). Optional: a block Image
// (drawn as an extra segment) and TMP labels for HP / block.
// For spawned enemies where the bar is created separately,
// call Bind(enemy) / Bind(player) explicitly instead.
// =====================================================

public class HealthBar : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private Image fill;          // Image Type = Filled, Horizontal
    [SerializeField] private Image blockFill;     // optional: shown when Block > 0
    [SerializeField] private TMP_Text hpLabel;    // optional: "24/24"
    [SerializeField] private TMP_Text blockLabel; // optional: block value; hidden at 0

    [Header("Feel")]
    [Tooltip("How fast the fill chases the real value. 0 = snap instantly.")]
    [SerializeField] private float lerpSpeed = 10f;
    [SerializeField] private Color fullColor = new Color(0.30f, 0.95f, 0.75f);
    [SerializeField] private Color lowColor  = new Color(1.00f, 0.25f, 0.30f);
    [Tooltip("Fraction below which the bar tints toward lowColor.")]
    [SerializeField, Range(0f, 1f)] private float lowThreshold = 0.35f;

    [Header("Binding")]
    [Tooltip("If on, find a PlayerCombatant or Enemy in this object's parents on Awake.")]
    [SerializeField] private bool autoBind = true;

    // pull the live values without caring which type we bound to
    System.Func<int> getHP, getMax, getBlock;
    System.Action unsubscribe;
    float shown = 1f;   // the fraction currently displayed (for lerp)

    void Awake()
    {
        if (autoBind)
        {
            var player = GetComponentInParent<PlayerCombatant>();
            if (player != null) { Bind(player); return; }

            var enemy = GetComponentInParent<Enemy>();
            if (enemy != null) Bind(enemy);
        }
    }

    public void Bind(PlayerCombatant p)
    {
        Unbind();
        getHP = () => p.CurrentHP; getMax = () => p.MaxHP; getBlock = () => p.Block;
        p.OnStateChanged += Refresh;
        unsubscribe = () => p.OnStateChanged -= Refresh;
        SnapAndRefresh();
    }

    public void Bind(Enemy e)
    {
        Unbind();
        getHP = () => e.CurrentHP; getMax = () => e.MaxHP; getBlock = () => e.Block;
        e.OnStateChanged += Refresh;
        unsubscribe = () => e.OnStateChanged -= Refresh;
        SnapAndRefresh();
    }

    void Unbind()
    {
        unsubscribe?.Invoke();
        unsubscribe = null;
    }

    void OnDestroy() => Unbind();

    // Snap the displayed fill to the true value with no animation (on bind).
    void SnapAndRefresh()
    {
        shown = TargetFraction();
        Refresh();
    }

    float TargetFraction()
    {
        if (getMax == null) return 0f;
        int max = Mathf.Max(1, getMax());
        return Mathf.Clamp01(getHP() / (float)max);
    }

    // Called on every OnStateChanged: update text + block instantly; the
    // fill itself eases toward the value in Update for a smooth drain.
    void Refresh()
    {
        if (getMax == null) return;
        int hp = getHP(), max = Mathf.Max(1, getMax()), block = getBlock();

        if (hpLabel) hpLabel.text = $"{hp}/{max}";

        if (blockLabel)
        {
            blockLabel.gameObject.SetActive(block > 0);
            if (block > 0) blockLabel.text = block.ToString();
        }
        if (blockFill)
        {
            blockFill.enabled = block > 0;
            if (block > 0) blockFill.fillAmount = Mathf.Clamp01(block / (float)max);
        }

        if (lerpSpeed <= 0f && fill) { shown = TargetFraction(); ApplyFill(); }
    }

    void Update()
    {
        if (getMax == null || fill == null) return;

        float target = TargetFraction();
        shown = lerpSpeed <= 0f
            ? target
            : Mathf.MoveTowards(shown, target, lerpSpeed * Time.deltaTime);
        ApplyFill();
    }

    void ApplyFill()
    {
        fill.fillAmount = shown;
        fill.color = Color.Lerp(lowColor, fullColor,
            Mathf.InverseLerp(0f, Mathf.Max(0.0001f, lowThreshold), shown));
    }
}
