using UnityEngine;

// =====================================================
// ECHOFORM — Enemy (abstract base)
// Shared state for every creature: HP, block, and a
// telegraphed intent (what it will do on its turn).
// TakeDamage() returns the OVERKILL — damage left over
// after the enemy died — which is what makes the slime's
// "clean cut" carry-through possible.
// =====================================================

public enum IntentType { Attack, Fuse, Idle }

public struct DamageResult
{
    public bool killed;     // did this hit reduce HP to 0?
    public int overkill;    // leftover damage after the kill (>= 0)

    public static DamageResult Survived => new DamageResult { killed = false, overkill = 0 };
}

public abstract class Enemy : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] protected int maxHP = 10;

    public int MaxHP => maxHP;
    public int CurrentHP { get; protected set; }
    public int Block { get; protected set; }

    [Header("Turn")]
    public IntentType intentType = IntentType.Attack;
    public int intentValue = 3;

    // A freshly spawned / freshly fused enemy sits out one enemy turn.
    public bool SpawnedThisTurn { get; set; }

    [Header("Placement")]
    [Tooltip("If ON, CombatManager leaves this enemy where you placed it in the scene instead of "
        + "snapping it into the formation. Use for hand-placed bosses like the Clone.")]
    [SerializeField] private bool keepScenePosition = false;
    public bool KeepScenePosition => keepScenePosition;

    public bool IsDead => CurrentHP <= 0;

    /// <summary>Display name for logs / UI.</summary>
    public abstract string DisplayName { get; }

    // Fired on any state change so views can refresh.
    public System.Action OnStateChanged;

    protected virtual void Awake()
    {
        if (CurrentHP == 0) CurrentHP = maxHP;
    }

    /// <summary>Configure HP directly (used when spawning split children / fusions).</summary>
    public void InitHealth(int currentHp, int maximumHp)
    {
        maxHP = Mathf.Max(1, maximumHp);
        CurrentHP = Mathf.Clamp(currentHp, 0, maxHP);
        OnStateChanged?.Invoke();
    }

    /// <summary>
    /// Apply damage (block first, then HP). Returns whether it died and how
    /// much damage spilled past the kill so callers can carry it through.
    /// </summary>
    public virtual DamageResult TakeDamage(int amount)
    {
        amount = Mathf.Max(0, amount);

        int absorbed = Mathf.Min(Block, amount);
        Block -= absorbed;
        amount -= absorbed;

        if (amount < CurrentHP)
        {
            CurrentHP -= amount;
            OnStateChanged?.Invoke();
            return DamageResult.Survived;
        }

        int overkill = amount - CurrentHP;
        CurrentHP = 0;
        OnStateChanged?.Invoke();
        return new DamageResult { killed = true, overkill = overkill };
    }

    /// <summary>Roll the intent shown to the player for next turn. Overridden per type.</summary>
    public abstract void RollIntent();
}
