using UnityEngine;

// =====================================================
// ECHOFORM — PlayerCombatant
// Holds Vestige's combat state: HP, block, and the Focus
// buff (Slay-the-Spire "Strength") that adds to every
// attack. Damage soaks into block before HP.
// =====================================================

public class PlayerCombatant : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHP = 60;

    [Header("Shields")]
    [Tooltip("Max discrete shields. Each shield negates one full enemy hit, then breaks.")]
    [SerializeField] private int maxShields = 4;

    public int MaxHP => maxHP;
    public int CurrentHP { get; private set; }
    public int Block { get; private set; }

    // Discrete shields (0..maxShields). Persist across turns until broken by a hit.
    public int MaxShields => maxShields;
    public int Shields { get; private set; }
    public bool CanGainShield => Shields < maxShields;

    // Focus adds flat damage to each attack action the player plays.
    public int Focus { get; private set; }

    public bool IsDead => CurrentHP <= 0;

    // Fired whenever HP / block / focus change, so UI can refresh.
    public System.Action OnStateChanged;

    private void Awake()
    {
        CurrentHP = maxHP;
    }

    public void RestoreCheckpoint(int hp, int shields, int focus)
    {
        CurrentHP = Mathf.Clamp(hp, 1, maxHP);
        Shields = Mathf.Clamp(shields, 0, maxShields);
        Focus = Mathf.Max(0, focus);
        Block = 0;
        OnStateChanged?.Invoke();
    }

    /// <summary>Block is temporary — cleared at the start of each player turn.</summary>
    public void ResetBlock()
    {
        Block = 0;
        OnStateChanged?.Invoke();
    }

    public void AddBlock(int amount)
    {
        Block += Mathf.Max(0, amount);
        OnStateChanged?.Invoke();
    }

    public void Heal(int amount)
    {
        CurrentHP = Mathf.Min(maxHP, CurrentHP + Mathf.Max(0, amount));
        OnStateChanged?.Invoke();
    }

    public void AddFocus(int amount)
    {
        Focus += amount;
        OnStateChanged?.Invoke();
    }

    /// <summary>Add discrete shields, capped at maxShields. Each shield negates one full hit.</summary>
    public void AddShield(int count)
    {
        Shields = Mathf.Clamp(Shields + Mathf.Max(0, count), 0, maxShields);
        OnStateChanged?.Invoke();
    }

    /// <summary>Incoming enemy damage: one shield negates a whole hit, else block then HP.</summary>
    public void TakeDamage(int amount)
    {
        amount = Mathf.Max(0, amount);
        if (amount <= 0) return;

        // a single shield fully absorbs this hit, then breaks
        if (Shields > 0)
        {
            Shields--;
            OnStateChanged?.Invoke();
            return;
        }

        int absorbed = Mathf.Min(Block, amount);
        Block -= absorbed;
        amount -= absorbed;

        CurrentHP = Mathf.Max(0, CurrentHP - amount);
        OnStateChanged?.Invoke();
    }
}
