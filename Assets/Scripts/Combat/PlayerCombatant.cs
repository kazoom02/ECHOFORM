using UnityEngine;

// =====================================================
// ECHOFORM — PlayerCombatant
// Mantém o estado de combate do jogador, incluindo vida, bloqueio, foco
// e escudos, e aplica os efeitos de dano, cura e reforço.
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

    public int MaxShields => maxShields;
    public int Shields { get; private set; }
    public bool CanGainShield => Shields < maxShields;

    public int Focus { get; private set; }

    public bool IsDead => CurrentHP <= 0;

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

        public void AddShield(int count)
    {
        Shields = Mathf.Clamp(Shields + Mathf.Max(0, count), 0, maxShields);
        OnStateChanged?.Invoke();
    }

        public void TakeDamage(int amount)
    {
        amount = Mathf.Max(0, amount);
        if (amount <= 0) return;

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
