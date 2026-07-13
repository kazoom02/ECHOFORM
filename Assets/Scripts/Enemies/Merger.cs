using UnityEngine;
using UnityEngine.Audio;

// The Merger has exactly three stages. Two surviving T1s become a T2,
// and two surviving T2s become a T3. T3 is the final form.
public enum MergerTier
{
    Tier1 = 0,
    Tier2 = 1,
    Tier3 = 2
}

public class Merger : Enemy
{
    [Header("Merger")]
    [SerializeField] private MergerTier tier = MergerTier.Tier1;

    [Header("Tier balance")]
    [SerializeField, Min(1)] private int tier1MaxHP = 30;
    [SerializeField, Min(1)] private int tier2MaxHP = 52;
    [SerializeField, Min(1)] private int tier3MaxHP = 85;
    [SerializeField, Min(0)] private int tier1Attack = 4;
    [SerializeField, Min(0)] private int tier2Attack = 8;
    [SerializeField, Min(0)] private int tier3Attack = 13;
    [Tooltip("A fusion cannot spawn below this percentage of its new tier's maximum HP. Damage still carries when the surviving total is higher.")]
    [SerializeField, Range(0f, 1f)] private float fusionMinimumHealthPercent = 0.75f;

    [Header("Tier visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite tier1Sprite;
    [SerializeField] private Sprite tier2Sprite;
    [SerializeField] private Sprite tier3Sprite;

    [Tooltip("Optional tier Animator Controllers. Static tier sprites are used when these are empty.")]
    [SerializeField] private Animator animator;
    [SerializeField] private RuntimeAnimatorController tier1Controller;
    [SerializeField] private RuntimeAnimatorController tier2Controller;
    [SerializeField] private RuntimeAnimatorController tier3Controller;

    [Tooltip("UI root that counter-scales so the health bar keeps the same world size at every tier.")]
    [SerializeField] private Transform fixedScaleHealthBar;

    [Header("Tier size")]
    [SerializeField] private float tier1Scale = 1f;
    [SerializeField] private float tier2Scale = 1.5f;
    [SerializeField] private float tier3Scale = 1.8f;

    [Header("Tier placement")]
    [Tooltip("Larger tiers are raised so scaling around the centre does not push them below the camera.")]
    [SerializeField] private float tier2YOffset = 0.45f;
    [SerializeField] private float tier3YOffset = 0.95f;

    [Header("SFX (shared with Slime)")]
    [Tooltip("The Slime divide sound, played when two Mergers fuse.")]
    [SerializeField] private AudioClip fusionSfx;
    [Tooltip("The Slime death sound, played when a Merger is destroyed.")]
    [SerializeField] private AudioClip deathSfx;
    [SerializeField] private AudioMixerGroup sfxGroup;

    // Set by CombatManager when this merger is scheduled to fuse.
    public bool IsFusing { get; set; }

    public MergerTier Tier => tier;
    public bool CanFuse => tier != MergerTier.Tier3;
    public Vector3 FormationOffset => new Vector3(0f, tier switch
    {
        MergerTier.Tier2 => tier2YOffset,
        MergerTier.Tier3 => tier3YOffset,
        _ => 0f
    }, 0f);

    public override string DisplayName => tier switch
    {
        MergerTier.Tier2 => "Merger T2",
        MergerTier.Tier3 => "Merger T3",
        _ => "Merger T1"
    };

    private Vector3 authoredScale;
    private Vector3 authoredHealthBarScale;

    protected override void Awake()
    {
        authoredScale = transform.localScale;

        if (fixedScaleHealthBar == null)
        {
            Canvas healthCanvas = GetComponentInChildren<Canvas>(true);
            if (healthCanvas != null) fixedScaleHealthBar = healthCanvas.transform;
        }
        if (fixedScaleHealthBar != null)
            authoredHealthBarScale = fixedScaleHealthBar.localScale;

        if (CurrentHP == 0)
        {
            maxHP = MaxHpForTier(tier);
            CurrentHP = maxHP;
        }

        ApplyTierVisual();
        RollIntent();
    }

    /// <summary>Sets up a merger created by the fusion phase.</summary>
    public void Configure(MergerTier newTier, int hp)
    {
        tier = newTier;

        // Carry surviving HP forward, but stabilize a newly fused body so the
        // final tier cannot arrive almost dead after the player chipped its parts.
        int tierMaximum = MaxHpForTier(newTier);
        int minimumFusionHP = Mathf.CeilToInt(tierMaximum * fusionMinimumHealthPercent);
        int fusedHP = Mathf.Max(hp, minimumFusionHP);
        InitHealth(fusedHP, Mathf.Max(fusedHP, tierMaximum));
        SpawnedThisTurn = true;

        ApplyTierVisual();
        RollIntent();
    }

    public override void RollIntent()
    {
        intentType = IntentType.Attack;
        intentValue = AttackForTier(tier);
    }

    public void PlayFusionSfx(Vector3 fusionPosition) =>
        SfxPlayer.PlayAt(fusionSfx, sfxGroup, fusionPosition);

    public void PlayDeathSfx() =>
        SfxPlayer.PlayAt(deathSfx, sfxGroup, transform.position);

    /// <summary>Returns the next tier, capped at the final T3 form.</summary>
    public static MergerTier FusedTier(MergerTier a, MergerTier b)
    {
        int highestTier = Mathf.Max((int)a, (int)b);
        return (MergerTier)Mathf.Min(highestTier + 1, (int)MergerTier.Tier3);
    }

    private int MaxHpForTier(MergerTier value) => value switch
    {
        MergerTier.Tier2 => tier2MaxHP,
        MergerTier.Tier3 => tier3MaxHP,
        _ => tier1MaxHP
    };

    private int AttackForTier(MergerTier value) => value switch
    {
        MergerTier.Tier2 => tier2Attack,
        MergerTier.Tier3 => tier3Attack,
        _ => tier1Attack
    };

    private void ApplyTierVisual()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            Sprite sprite = tier switch
            {
                MergerTier.Tier2 => tier2Sprite,
                MergerTier.Tier3 => tier3Sprite,
                _ => tier1Sprite
            };

            if (sprite != null)
                spriteRenderer.sprite = sprite;
        }

        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator != null)
        {
            RuntimeAnimatorController controller = tier switch
            {
                MergerTier.Tier2 => tier2Controller,
                MergerTier.Tier3 => tier3Controller,
                _ => tier1Controller
            };

            if (controller != null)
                animator.runtimeAnimatorController = controller;
        }

        float scaleMultiplier = tier switch
        {
            MergerTier.Tier2 => tier2Scale,
            MergerTier.Tier3 => tier3Scale,
            _ => tier1Scale
        };

        // Preserve the prefab's authored facing (its X scale may be negative).
        transform.localScale = authoredScale * scaleMultiplier;

        // The Merger grows, but its world-space health UI remains readable and
        // exactly the same size as it was at Tier 1.
        if (fixedScaleHealthBar != null && scaleMultiplier > 0f)
            fixedScaleHealthBar.localScale = authoredHealthBarScale / scaleMultiplier;
    }
}
