using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// =====================================================
// ECHOFORM — CombatManager
// The turn-loop state machine that ties everything together:
//   • deals a hand, spends energy, resolves cards
//   • applies damage with OVERKILL CARRY-THROUGH into slime splits
//   • runs the enemy turn: MERGE phase first, then attacks
//   • checks win / lose
//
// Wire the two enemy prefabs (a slime prefab and a merger
// prefab, each with the matching component) plus the player
// and the starting deck in the Inspector. UI is intentionally
// left out — hook a view to the OnXxx events / public state.
// =====================================================

public enum CombatState { Idle, PlayerTurn, EnemyTurn, Win, Lose }

public class CombatManager : MonoBehaviour
{
    [Header("Actors")]
    [SerializeField] private PlayerCombatant player;
    [SerializeField] private VestigeCombatAnimator vestige;   // Vestige visuals, for the death animation
    [SerializeField] private Slime slimePrefab;
    [SerializeField] private Merger mergerPrefab;
    [SerializeField] private Transform enemyRow;      // parent + anchor for the enemy line

    [Header("Encounter")]
    [Tooltip("Enemies present in the scene at combat start (drag existing ones here).")]
    [SerializeField] private List<Enemy> startingEnemies = new List<Enemy>();

    [Header("Deck")]
    [SerializeField] private List<CardData> startingDeck = new List<CardData>();

    [Header("Rules")]
    [SerializeField] private int energyPerTurn = 3;
    [SerializeField] private int handSize = 5;
    [SerializeField] private float enemyRowSpacing = 2.2f;   // horizontal gap between enemies

    [Header("Formation (area)")]
    [Tooltip("Max enemies per horizontal row before wrapping to a new row.")]
    [SerializeField] private int enemiesPerRow = 4;
    [Tooltip("Vertical gap between wrapped rows.")]
    [SerializeField] private float rowSpacing = 1.8f;
    [Tooltip("Vertical offset on alternating columns for an organic, non-straight look.")]
    [SerializeField] private float columnStagger = 0.4f;
    [Tooltip("Draw lower enemies in front for natural overlap. Off if it fights your sorting setup.")]
    [SerializeField] private bool sortByDepth = true;

    [Header("Corruption")]
    [Tooltip("Fallback corrupted chip — used only before the player has played anything. Normally the Loom copies your last-played chip. Needs Is Glitch = true.")]
    [SerializeField] private CardData corruptedChip;
    [Tooltip("Inject one corrupted chip every N player turns (0 = never).")]
    [SerializeField] private int corruptEveryNTurns = 3;

    // ---- runtime state ----
    public CombatState State { get; private set; } = CombatState.Idle;
    public int Energy { get; private set; }
    public Deck Deck { get; private set; }
    public IReadOnlyList<Enemy> Enemies => enemies;
    /// <summary>How many corrupted chips are clogging the current hand (drives the overload readout).</summary>
    public int CorruptedInHand => Deck != null ? Deck.Hand.Count(c => c != null && c.isGlitch) : 0;

    private readonly List<Enemy> enemies = new List<Enemy>();
    private CardData lastPlayedCard;
    private int turnNumber;

    // events for a UI layer to subscribe to
    public System.Action OnCombatChanged;
    public System.Action<CombatState> OnStateChanged;
    public System.Action<string> OnLog;

    private void Start()
    {
        StartCombat();
    }

    // ------------------------------------------------------------------ setup

    public void StartCombat()
    {
        enemies.Clear();
        enemies.AddRange(startingEnemies.Where(e => e != null));

        turnNumber = 0;
        Deck = new Deck(startingDeck);
        foreach (var e in enemies) if (e is PlayerClone pc) pc.InitDeck(startingDeck);   // Area 3 clone mirrors your deck
        RepositionEnemies();
        UpdateMergerTelegraph();

        Log("The Loom hums. Cut the thread.");
        StartPlayerTurn();
    }

    // ------------------------------------------------------------- player turn

    private void StartPlayerTurn()
    {
        if (CheckEndOfCombat()) return;

        SetState(CombatState.PlayerTurn);

        // fresh spawns can now act next enemy turn
        foreach (var e in enemies) e.SpawnedThisTurn = false;

        player.ResetBlock();
        Energy = energyPerTurn;
        Deck.DrawUpTo(handSize);   // fills clean slots; stuck corruption stays

        // The Loom copies your LAST-PLAYED chip into memory as corruption, every N turns.
        turnNumber++;
        if (corruptEveryNTurns > 0 && turnNumber % corruptEveryNTurns == 0)
        {
            CardData copy = MakeCorruptedCopy();
            if (copy != null)
            {
                Deck.CorruptHand(copy, handSize);
                Log($"The Loom copies your {(lastPlayedCard != null ? lastPlayedCard.cardName : "memory")} — now corrupted.");
            }
        }

        // Corrupted chips clogging memory overload Vestige — escalating damage, 5 = death.
        if (ApplyOverload()) { OnCombatChanged?.Invoke(); return; }

        UpdateMergerTelegraph();

        Log("Your move.");
        OnCombatChanged?.Invoke();
    }

    /// <summary>Called by the UI when the player clicks a card (target may be null for Self/AllEnemies).</summary>
    /// <summary>Can this card be played right now? Covers glitch, energy and the
    /// "shields already full" rule. Targeting is checked separately at play time.
    /// Used by the HUD to show the deny shake before the install animation runs.</summary>
    public bool CanPlayCard(CardData card)
    {
        if (card == null || card.isGlitch) return false;
        if (State != CombatState.PlayerTurn) return false;
        if (card.energyCost > Energy) return false;
        if (card.HasEffect(CardEffectType.GainShield) && !player.CanGainShield) return false;
        return true;
    }

    public bool TryPlayCard(CardData card, Enemy target = null)
    {
        if (State != CombatState.PlayerTurn) return false;
        if (card == null || card.isGlitch) return false;
        if (card.energyCost > Energy) { Log("Not enough energy."); return false; }
        if (card.HasEffect(CardEffectType.GainShield) && !player.CanGainShield)
        { Log("Shields already full."); return false; }
        if (card.target == CardTarget.SingleEnemy && (target == null || target.IsDead)) return false;

        Energy -= card.energyCost;
        ResolveCard(card, target);

        // Echo should copy the card played BEFORE it, so update this after resolving.
        if (!card.HasEffect(CardEffectType.DuplicateCard)) lastPlayedCard = card;

        Deck.ResolvePlayed(card);

        CheckEndOfCombat();
        OnCombatChanged?.Invoke();
        return true;
    }

    private void ResolveCard(CardData card, Enemy target)
    {
        foreach (var effect in card.effects)
        {
            switch (effect.type)
            {
                case CardEffectType.DealDamage:
                    int dmg = effect.amount + player.Focus;
                    if (card.target == CardTarget.AllEnemies)
                    {
                        // iterate a copy — splits mutate the list mid-loop
                        foreach (var e in enemies.ToList())
                            if (!e.IsDead) DealDamageToEnemy(e, dmg);
                    }
                    else if (target != null)
                    {
                        DealDamageToEnemy(target, dmg);
                    }
                    break;

                case CardEffectType.GainBlock: player.AddBlock(effect.amount); break;
                case CardEffectType.GainShield: player.AddShield(effect.amount); break;
                case CardEffectType.Heal:      player.Heal(effect.amount); break;
                case CardEffectType.GainFocus: player.AddFocus(effect.amount); break;
                case CardEffectType.DrawCards: Deck.Draw(effect.amount); break;
                case CardEffectType.GainEnergy: Energy += effect.amount; break;

                case CardEffectType.DuplicateCard:
                    if (lastPlayedCard != null)
                    {
                        Deck.AddToHand(lastPlayedCard);
                        Log($"Echo: {lastPlayedCard.cardName} copied.");
                    }
                    break;
            }
        }
    }

    // --------------------------------------------------- damage & slime splits

    /// <summary>
    /// Deal damage to one enemy. If it dies and it's a slime, the leftover
    /// (overkill) is carried into the split children — the core mechanic.
    /// </summary>
    public void DealDamageToEnemy(Enemy enemy, int amount)
    {
        DamageResult result = enemy.TakeDamage(amount);
        if (!result.killed) return;

        bool animatedDeath = false;

        if (enemy is Slime slime)
        {
            List<SlimeSpawn> children = slime.PlanChildren(result.overkill);

            if (children.Count == 0)
                Log(result.overkill > 0 ? $"Clean cut — {slime.DisplayName} erased." : $"{slime.DisplayName} down.");
            else
                Log(result.overkill > 0 ? $"Overkill! {slime.DisplayName} splits, children wounded." : $"{slime.DisplayName} splits.");

            if (children.Count > 0)
            {
                slime.PlayDivideSfx();                 // SlimeDividing SFX
                SpawnSlimeChildren(slime, children);
            }
            else
            {
                slime.PlayDeath();                     // small slime dies for good: dying animation + SFX
                animatedDeath = true;                  // ...so do not destroy it instantly below
            }
        }
        else
        {
            Log($"{enemy.DisplayName} destroyed.");
        }

        RemoveEnemy(enemy, destroy: !animatedDeath);
        UpdateMergerTelegraph();
        RepositionEnemies();
    }

    private void SpawnSlimeChildren(Slime parent, List<SlimeSpawn> children)
    {
        foreach (var spec in children)
        {
            Slime child = Instantiate(slimePrefab, parent.transform.position, Quaternion.identity, enemyRow);
            child.Configure(spec.tier, spec.hp);
            enemies.Add(child);
        }
    }

    // ---------------------------------------------------------- enemy turn

    public void EndTurn()
    {
        if (State != CombatState.PlayerTurn) return;
        Deck.DiscardHand();
        StartCoroutine(EnemyTurn());
    }

    private IEnumerator EnemyTurn()
    {
        SetState(CombatState.EnemyTurn);

        // 1) MERGE PHASE — mergers that survived the turn fuse in pairs.
        yield return RunMergePhase();
        if (CheckEndOfCombat()) yield break;

        // 2) ATTACK PHASE — everything that isn't freshly spawned/fused hits.
        foreach (var e in enemies.ToList())
        {
            if (e == null || e.IsDead || e.SpawnedThisTurn) continue;

            if (e is PlayerClone clone)
            {
                yield return clone.TakeTurn(player, Log);
                if (player.IsDead) SetState(CombatState.Lose);
                if (State == CombatState.Lose) yield break;   // Clone killed Vestige: stop the turn
                yield return new WaitForSeconds(0.15f);
                continue;
            }

            if (e.intentType == IntentType.Attack)
            {
                int dmg = e.intentValue;
                var melee = e.GetComponent<EnemyMeleeAnimator>();
                if (melee != null)
                {
                    // walk across, land damage on the hit frame, walk back
                    yield return melee.PlayAttack(player.transform, () =>
                    {
                        player.TakeDamage(dmg);
                        Log($"{e.DisplayName} strikes for {dmg}.");
                        if (player.IsDead) SetState(CombatState.Lose);   // Vestige dies on the hit frame: play death instantly
                    });
                }
                else
                {
                    player.TakeDamage(dmg);
                    Log($"{e.DisplayName} strikes for {dmg}.");
                    if (player.IsDead) SetState(CombatState.Lose);
                }
                if (State == CombatState.Lose) yield break;   // Vestige is dead: stop the enemy turn, no other enemy attacks
                yield return new WaitForSeconds(0.15f);
            }
        }

        if (CheckEndOfCombat()) yield break;

        // 3) telegraph next turn's intents
        foreach (var e in enemies) e.RollIntent();

        StartPlayerTurn();
    }

    private IEnumerator RunMergePhase()
    {
        var eligible = enemies.OfType<Merger>()
                              .Where(m => !m.IsDead && !m.SpawnedThisTurn)
                              .OrderBy(m => (int)m.Tier)
                              .ToList();

        for (int i = 0; i + 1 < eligible.Count; i += 2)
        {
            Merger a = eligible[i];
            Merger b = eligible[i + 1];

            MergerTier fused = Merger.FusedTier(a.Tier, b.Tier);
            int hp = a.CurrentHP + b.CurrentHP;                 // HP carries — chipping is never wasted
            Vector3 pos = (a.transform.position + b.transform.position) * 0.5f;

            RemoveEnemy(a);
            RemoveEnemy(b);

            Merger merged = Instantiate(mergerPrefab, pos, Quaternion.identity, enemyRow);
            merged.Configure(fused, hp);
            enemies.Add(merged);

            Log($"Two mergers fuse into a {merged.DisplayName} ({hp} HP).");
            yield return new WaitForSeconds(0.2f);
        }

        UpdateMergerTelegraph();
        RepositionEnemies();
    }

    // --------------------------------------------------------------- helpers

    private void UpdateMergerTelegraph()
    {
        var mergers = enemies.OfType<Merger>().Where(m => !m.IsDead).ToList();
        bool willFuse = mergers.Count(m => !m.SpawnedThisTurn) >= 2;
        foreach (var m in mergers) m.IsFusing = willFuse && !m.SpawnedThisTurn;
    }

    private void RemoveEnemy(Enemy e, bool destroy = true)
    {
        enemies.Remove(e);
        if (destroy && e != null) Destroy(e.gameObject);
    }

    private void RepositionEnemies()
    {
        if (enemyRow == null) return;

        int n = 0;
        for (int i = 0; i < enemies.Count; i++) if (enemies[i] != null && !enemies[i].KeepScenePosition) n++;
        if (n == 0) return;

        int perRow = Mathf.Max(1, enemiesPerRow);

        int slot = 0;
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] == null) continue;
            if (enemies[i].KeepScenePosition) continue;   // hand-placed boss (e.g. the Clone) keeps its scene position

            int row = slot / perRow;
            int col = slot % perRow;

            // enemies actually in this row (last row may be partial) — used to center it
            int inThisRow = Mathf.Min(perRow, n - row * perRow);
            float rowStartX = -(inThisRow - 1) * enemyRowSpacing * 0.5f;

            float x = rowStartX + col * enemyRowSpacing;
            float y = -row * rowSpacing + ((col % 2 == 1) ? columnStagger : 0f);

            Vector3 pos = enemyRow.position + new Vector3(x, y, 0f);
            enemies[i].transform.position = pos;

            if (sortByDepth)
            {
                var sr = enemies[i].GetComponentInChildren<SpriteRenderer>();
                if (sr != null) sr.sortingOrder = Mathf.RoundToInt(-pos.y * 100f);
            }

            slot++;
        }
    }

    /// <summary>Build a corrupted, unplayable duplicate of the chip the player last used — the Loom copying you.</summary>
    private CardData MakeCorruptedCopy()
    {
        CardData source = lastPlayedCard != null ? lastPlayedCard : corruptedChip;
        if (source == null) return null;

        CardData copy = Instantiate(source);   // runtime clone of the ScriptableObject
        copy.isGlitch = true;                   // now unplayable corruption
        copy.name = source.cardName + " (Corrupted)";
        return copy;
    }

    /// <summary>
    /// Neural memory overload: corrupted chips left clogging the hand deal escalating
    /// damage each turn — 3 → 1, 4 → 2, 5 → Vestige is overwritten (instant loss).
    /// Returns true if combat ended (death), so the caller stops the turn.
    /// </summary>
    private bool ApplyOverload()
    {
        int corrupted = Deck.Hand.Count(c => c != null && c.isGlitch);

        if (corrupted >= 5)
        {
            SetState(CombatState.Lose);
            Log("Neural memory overload — Vestige is overwritten. The Loom prints copy №10.");
            return true;
        }

        if (corrupted >= 3)
        {
            int dmg = corrupted - 2;                 // 3 -> 1, 4 -> 2
            player.TakeDamage(dmg);
            Log($"Overload: {corrupted} corrupted chips clog memory — {dmg} damage.");
            if (CheckEndOfCombat()) return true;      // damage could be lethal
        }

        return false;
    }

    private bool CheckEndOfCombat()
    {
        if (player != null && player.IsDead) { SetState(CombatState.Lose); Log("Vestige falls. The Loom prints copy №10."); return true; }
        if (enemies.All(e => e == null || e.IsDead)) { SetState(CombatState.Win); Log("The thread is cut — for now."); return true; }
        return false;
    }

    private void SetState(CombatState s)
    {
        bool enteringLose = s == CombatState.Lose && State != CombatState.Lose;
        State = s;
        if (enteringLose && vestige != null) vestige.PlayDeath();   // Vestige death animation + SFX
        OnStateChanged?.Invoke(s);
    }

    private void Log(string message)
    {
        Debug.Log($"[Echoform] {message}");
        OnLog?.Invoke(message);
    }
}
