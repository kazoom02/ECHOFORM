using UnityEngine;

// =====================================================
// ECHOFORM — Enemy
// Define a base comum dos inimigos, incluindo vida, bloqueio, intenção
// de turno e cálculo do dano excedente após um golpe fatal.
// =====================================================

public enum IntentType { Attack, Fuse, Idle }

public struct DamageResult
{
    public bool killed;
    public int overkill;

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

    public bool SpawnedThisTurn { get; set; }

    [Header("Placement")]
    [Tooltip("If ON, CombatManager leaves this enemy where you placed it in the scene instead of "
        + "snapping it into the formation. Use for hand-placed bosses like the Clone.")]
    [SerializeField] private bool keepScenePosition = false;
    public bool KeepScenePosition => keepScenePosition;

    public bool IsDead => CurrentHP <= 0;

        public abstract string DisplayName { get; }

    public System.Action OnStateChanged;

    protected virtual void Awake()
    {
        if (CurrentHP == 0) CurrentHP = maxHP;
    }

        public void InitHealth(int currentHp, int maximumHp)
    {
        maxHP = Mathf.Max(1, maximumHp);
        CurrentHP = Mathf.Clamp(currentHp, 0, maxHP);
        OnStateChanged?.Invoke();
    }

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

        public abstract void RollIntent();
}
