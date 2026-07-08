using UnityEngine;

// =====================================================
// ECHOFORM — Merger
// The inverse of the slime. It never splits; instead, if
// two Mergers both survive a turn they FUSE into a bigger
// one, carrying their COMBINED HP (so chipping is never
// wasted). Punishes killing slow, the way the slime
// punishes killing fast — "which threat do I feed?"
//
// Fusion itself is orchestrated by CombatManager during
// the enemy turn's merge phase; this class holds the tier
// stats and the "about to fuse" telegraph flag.
// =====================================================

public enum MergerTier { Ooze = 0, Confluence = 1, Amalgam = 2, Colossus = 3 }

public class Merger : Enemy
{
    [Header("Merger")]
    [SerializeField] private MergerTier tier = MergerTier.Ooze;

    // Set by CombatManager when 2+ mergers are alive: shows the ⛓ telegraph.
    public bool IsFusing { get; set; }

    public MergerTier Tier => tier;

    public override string DisplayName => tier switch
    {
        MergerTier.Confluence => "Confluence",
        MergerTier.Amalgam => "Amalgam",
        MergerTier.Colossus => "Colossus",
        _ => "Ooze"
    };

    protected override void Awake()
    {
        if (CurrentHP == 0)
        {
            maxHP = MaxHpForTier(tier);
            CurrentHP = maxHP;
        }
        RollIntent();
    }

    /// <summary>Set up a merger spawned at runtime (result of a fusion).</summary>
    public void Configure(MergerTier newTier, int hp)
    {
        tier = newTier;
        // A fusion carries summed HP, but never above the new tier's ceiling + a bonus pool.
        InitHealth(hp, Mathf.Max(hp, MaxHpForTier(newTier)));
        SpawnedThisTurn = true;   // fresh fusions sit out one turn
        RollIntent();
    }

    public override void RollIntent()
    {
        intentType = IntentType.Attack;
        intentValue = AttackForTier(tier);
    }

    /// <summary>The tier a fusion of two mergers becomes (escalates, capped at Colossus).</summary>
    public static MergerTier FusedTier(MergerTier a, MergerTier b)
    {
        int highest = Mathf.Max((int)a, (int)b);
        return (MergerTier)Mathf.Min(highest + 1, (int)MergerTier.Colossus);
    }

    // ---- tier stat tables -------------------------------------------------

    public static int MaxHpForTier(MergerTier tier) => tier switch
    {
        MergerTier.Confluence => 20,
        MergerTier.Amalgam => 34,
        MergerTier.Colossus => 52,
        _ => 10
    };

    // Merger attack climbs fast — first knob to lower if Fight III feels brutal.
    public static int AttackForTier(MergerTier tier) => tier switch
    {
        MergerTier.Confluence => 6,
        MergerTier.Amalgam => 9,
        MergerTier.Colossus => 12,
        _ => 3
    };
}
