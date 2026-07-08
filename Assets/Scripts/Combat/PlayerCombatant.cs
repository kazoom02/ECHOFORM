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

    public int MaxHP => maxHP;
    public int CurrentHP { get; private set; }
    public int Block { get; private set; }

    // Focus adds flat damage to each attack action the player plays.
    public int Focus { get; private set; }

    public bool IsDead => CurrentHP <= 0;

    // Fired whenever HP / block / focus change, so UI can refresh.
    public System.Action OnStateChanged;

    private void Awake()
    {
        CurrentHP = maxHP;
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

    /// <summary>Incoming enemy damage: block first, then HP.</summary>
    public void TakeDamage(int amount)
    {
        amount = Mathf.Max(0, amount);

        int absorbed = Mathf.Min(Block, amount);
        Block -= absorbed;
        amount -= absorbed;

        CurrentHP = Mathf.Max(0, CurrentHP - amount);
        OnStateChanged?.Invoke();
    }
}
