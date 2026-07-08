using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// =====================================================
// ECHOFORM — Slime
// The theme's core enemy: killing it makes MORE of it.
// A Large slime splits into 2 Medium, each Medium into
// 2 Small, Small dies for good  (3 → 2×2 → 4×1 → gone).
//
// OVERKILL CARRY-THROUGH lives here: leftover damage from
// the killing blow pours into the children one at a time.
// Enough overkill and a child never spawns at all — a
// "clean cut". This is the game's signature skill mechanic.
// =====================================================

public enum SlimeTier { Small = 1, Medium = 2, Large = 3 }

/// <summary>A pending child produced by a split (tier + the HP it should spawn with).</summary>
public struct SlimeSpawn
{
    public SlimeTier tier;
    public int hp;
}

public class Slime : Enemy
{
    [Header("Slime")]
    [SerializeField] private SlimeTier tier = SlimeTier.Large;
    [Tooltip("The Prime is the boss slime — same rules, higher stats, used in Fight III.")]
    [SerializeField] private bool isPrime = false;
    [Tooltip("If ON, use the Max HP set in the Inspector instead of the tier's default HP.")]
    [SerializeField] private bool overrideTierHP = false;

    [Header("Split animation")]
    [SerializeField] private float splitDuration = 0.35f;

    [Header("Tier visuals (assign all three on the prefab)")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite largeSprite;
    [SerializeField] private Sprite mediumSprite;
    [SerializeField] private Sprite smallSprite;

    [Header("Tier animations (optional — an Animator drives the sprite itself)")]
    [SerializeField] private Animator animator;
    [SerializeField] private RuntimeAnimatorController largeController;
    [SerializeField] private RuntimeAnimatorController mediumController;
    [SerializeField] private RuntimeAnimatorController smallController;

    public SlimeTier Tier => tier;
    public bool IsPrime => isPrime;

    public override string DisplayName =>
        isPrime ? "The Prime" : tier switch
        {
            SlimeTier.Large => "Slime",
            SlimeTier.Medium => "Blob",
            _ => "Splitling"
        };

    protected override void Awake()
    {
        // Derive stats from tier unless the Inspector HP is being overridden.
        if (CurrentHP == 0)
        {
            if (!overrideTierHP) maxHP = MaxHpForTier(tier, isPrime);
            CurrentHP = maxHP;
        }
        ApplyTierVisual();
        RollIntent();
    }

    /// <summary>Set up a slime spawned at runtime (child of a split).</summary>
    public void Configure(SlimeTier newTier, int hp, bool prime = false)
    {
        tier = newTier;
        isPrime = prime;
        InitHealth(hp, MaxHpForTier(newTier, prime));
        SpawnedThisTurn = true;   // fresh children sit out one turn
        ApplyTierVisual();
        RollIntent();
    }

    /// <summary>Swap sprite + animator controller to match the current tier.</summary>
    private void ApplyTierVisual()
    {
        // static sprite (fallback / used when there's no Animator)
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            Sprite s = tier switch
            {
                SlimeTier.Large  => largeSprite,
                SlimeTier.Medium => mediumSprite,
                SlimeTier.Small  => smallSprite,
                _ => null
            };
            if (s != null) spriteRenderer.sprite = s;
        }

        // per-tier idle animation (overrides the static sprite while it plays)
        if (animator == null) animator = GetComponent<Animator>();
        if (animator != null)
        {
            RuntimeAnimatorController c = tier switch
            {
                SlimeTier.Large  => largeController,
                SlimeTier.Medium => mediumController,
                SlimeTier.Small  => smallController,
                _ => null
            };
            if (c != null) animator.runtimeAnimatorController = c;
        }
    }

    public override void RollIntent()
    {
        intentType = IntentType.Attack;
        intentValue = AttackForTier(tier, isPrime);
    }

    /// <summary>
    /// Decide which children this slime produces when it dies, given the
    /// overkill damage carried in. Small slimes produce none. Overkill is
    /// spent sequentially: a child whose full HP is covered is erased.
    /// </summary>
    public List<SlimeSpawn> PlanChildren(int overkill)
    {
        var children = new List<SlimeSpawn>();
        if (tier == SlimeTier.Small) return children;   // nothing smaller

        SlimeTier childTier = tier - 1;
        int childMax = MaxHpForTier(childTier, false);   // children are never Prime

        int remaining = Mathf.Max(0, overkill);
        const int childCount = 2;

        for (int i = 0; i < childCount; i++)
        {
            if (remaining >= childMax)
            {
                // Clean cut: the overkill wipes this child before it forms.
                remaining -= childMax;
                continue;
            }

            int hp = childMax - remaining;   // wounded survivor (or full if no overkill left)
            remaining = 0;
            children.Add(new SlimeSpawn { tier = childTier, hp = hp });
        }

        return children;
    }

    /// <summary>
    /// Visual split. Call from CombatManager after PlanChildren; it hands the
    /// spawn list to <paramref name="onSpawn"/> at the animation's midpoint.
    /// </summary>
    public IEnumerator Split(List<SlimeSpawn> children, System.Action<List<SlimeSpawn>> onSpawn)
    {
        Vector3 baseScale = transform.localScale;
        float t = 0f;

        // squash outward
        while (t < splitDuration)
        {
            t += Time.deltaTime;
            float k = t / splitDuration;
            transform.localScale = baseScale * (1f + 0.25f * Mathf.Sin(k * Mathf.PI));
            yield return null;
        }

        onSpawn?.Invoke(children);   // CombatManager instantiates the children here
    }

    // ---- tier stat tables -------------------------------------------------

    public static int MaxHpForTier(SlimeTier tier, bool prime)
    {
        int baseHp = tier switch
        {
            SlimeTier.Large => 24,
            SlimeTier.Medium => 12,
            _ => 6
        };
        return prime ? Mathf.RoundToInt(baseHp * 1.5f) : baseHp;
    }

    public static int AttackForTier(SlimeTier tier, bool prime)
    {
        int atk = tier switch
        {
            SlimeTier.Large => 8,
            SlimeTier.Medium => 5,
            _ => 3
        };
        return prime ? atk + 3 : atk;
    }
}
