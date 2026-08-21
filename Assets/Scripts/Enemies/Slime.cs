using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

// =====================================================
// ECHOFORM — Slime
// Implementa os vários níveis de Slime; ao morrer, cada Slime não pequeno
// divide-se em dois inimigos menores com os respetivos visuais e animações.
// =====================================================

public enum SlimeTier { Small = 1, Medium = 2, Large = 3 }

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
    [Tooltip("Plays once when this slime divides (e.g. SlimeDividing).")]
    [SerializeField] private AudioClip divideSfx;
    [Tooltip("Route to the SFX group of your AudioMixer so the SFX slider controls it.")]
    [SerializeField] private AudioMixerGroup sfxGroup;

    [Header("Death (small slime / clean cut)")]
    [Tooltip("Animator Controller whose DEFAULT state is the SlimeDying clip. Swapped in when this slime dies for good, so the dying animation plays once. Leave empty to just linger then vanish.")]
    [SerializeField] private RuntimeAnimatorController deathController;
    [Tooltip("Sound played when this slime dies for good (e.g. slimeDying).")]
    [SerializeField] private AudioClip deathSfx;
    [Tooltip("Seconds the corpse lingers so the dying animation can finish before it is destroyed.")]
    [SerializeField] private float deathDuration = 0.5f;

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

    [Header("Tier world size (localScale per tier)")]
    [Tooltip("Set all three equal to make every tier the same on-screen size, or shrink them for the classic split look.")]
    [SerializeField] private float largeScale  = 1f;
    [SerializeField] private float mediumScale = 1f;
    [SerializeField] private float smallScale  = 1f;

    public SlimeTier Tier => tier;
    public bool IsPrime => isPrime;
    public float DeathDuration => Mathf.Max(0.05f, deathDuration);

    public override string DisplayName =>
        isPrime ? "The Prime" : tier switch
        {
            SlimeTier.Large => "Slime",
            SlimeTier.Medium => "Blob",
            _ => "Splitling"
        };

    protected override void Awake()
    {

        if (CurrentHP == 0)
        {
            if (!overrideTierHP) maxHP = MaxHpForTier(tier, isPrime);
            CurrentHP = maxHP;
        }
        ApplyTierVisual();
        RollIntent();
    }

        public void Configure(SlimeTier newTier, int hp, bool prime = false)
    {
        tier = newTier;
        isPrime = prime;
        InitHealth(hp, MaxHpForTier(newTier, prime));
        SpawnedThisTurn = true;
        ApplyTierVisual();
        RollIntent();
    }

        private void ApplyTierVisual()
    {

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

        float sc = tier switch
        {
            SlimeTier.Large  => largeScale,
            SlimeTier.Medium => mediumScale,
            SlimeTier.Small  => smallScale,
            _ => 1f
        };
        transform.localScale = Vector3.one * sc;
    }

    public override void RollIntent()
    {
        intentType = IntentType.Attack;
        intentValue = AttackForTier(tier, isPrime);
    }

        public void PlayDivideSfx()
    {
        SfxPlayer.PlayAt(divideSfx, sfxGroup, transform.position);
    }

        public void PlayDeath()
    {
        transform.SetParent(null, true);

        if (animator == null) animator = GetComponent<Animator>();
        if (animator != null && deathController != null)
            animator.runtimeAnimatorController = deathController;

        SfxPlayer.PlayAt(deathSfx, sfxGroup, transform.position);

        foreach (var col in GetComponentsInChildren<Collider2D>()) col.enabled = false;

        Destroy(gameObject, DeathDuration);
    }

        public List<SlimeSpawn> PlanChildren(int overkill)
    {
        var children = new List<SlimeSpawn>();
        if (tier == SlimeTier.Small) return children;

        SlimeTier childTier = tier - 1;
        int childMax = MaxHpForTier(childTier, false);

        const int childCount = 2;

        for (int i = 0; i < childCount; i++)
            children.Add(new SlimeSpawn { tier = childTier, hp = childMax });

        return children;
    }

        public IEnumerator Split(List<SlimeSpawn> children, System.Action<List<SlimeSpawn>> onSpawn)
    {
        Vector3 baseScale = transform.localScale;
        float t = 0f;

        while (t < splitDuration)
        {
            t += Time.deltaTime;
            float k = t / splitDuration;
            transform.localScale = baseScale * (1f + 0.25f * Mathf.Sin(k * Mathf.PI));
            yield return null;
        }

        onSpawn?.Invoke(children);
    }

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
