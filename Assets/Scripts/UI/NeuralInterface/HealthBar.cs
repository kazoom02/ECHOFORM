using UnityEngine;
using UnityEngine.UI;
using TMPro;

// =====================================================
// ECHOFORM — HealthBar
// Apresenta a vida e o bloqueio do jogador ou de um inimigo e atualiza
// automaticamente os valores quando o estado do combatente se altera.
// =====================================================

public class HealthBar : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private Image fill;
    [SerializeField] private Image blockFill;
    [SerializeField] private TMP_Text hpLabel;
    [SerializeField] private TMP_Text blockLabel;

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

    System.Func<int> getHP, getMax, getBlock;
    System.Action unsubscribe;
    float shown = 1f;

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
