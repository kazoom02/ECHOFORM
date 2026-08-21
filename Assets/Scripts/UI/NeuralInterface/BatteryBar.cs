using System;
using UnityEngine;
using UnityEngine.UI;

// =====================================================
// ECHOFORM — BatteryBar
// Representa a vida do jogador através de células de bateria, atualizando
// o preenchimento e o aspeto de cada célula após dano ou cura.
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

        [NonSerialized] public Sprite normalSprite;
        [NonSerialized] public float shown;
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

    void OnChanged() {  }

    int Count => batteries != null ? batteries.Length : 0;

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

                Sprite s = target <= 0.0001f ? c.depletedSprite : c.normalSprite;
                if (s != null && c.background.sprite != s) c.background.sprite = s;
            }
        }
    }
}
