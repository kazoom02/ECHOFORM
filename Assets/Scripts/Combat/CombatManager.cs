using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

// =====================================================
// ECHOFORM — CombatManager
// Coordena o ciclo de turnos, o baralho, a execução das cartas, as ações
// dos inimigos e as condições de vitória ou derrota de cada combate.
// =====================================================

public enum CombatState { Idle, PlayerTurn, EnemyTurn, Win, Lose }

public class CombatManager : MonoBehaviour
{
    [Header("Actors")]
    [SerializeField] private PlayerCombatant player;
    [SerializeField] private VestigeCombatAnimator vestige;
    [SerializeField] private Slime slimePrefab;
    [SerializeField] private Merger mergerPrefab;
    [SerializeField] private Transform enemyRow;

    [Header("Fallback Encounter")]
    [Tooltip("Used only in scenes without an active AreaEncounter. Area 1/2/3 enemy counts are configured on their AreaEncounter components.")]
    [SerializeField] private List<Enemy> startingEnemies = new List<Enemy>();

    [Header("Deck")]
    [SerializeField] private List<CardData> startingDeck = new List<CardData>();
    [Tooltip("Generated into the rack after the player lands the required number of Strikes. Keep it out of Starting Deck.")]
    [SerializeField] private CardData heavySlashChip;

    [Header("Rules")]
    [Tooltip("CPU restored automatically at the beginning of every player turn.")]
    [SerializeField] private int energyPerTurn = 3;
    [Tooltip("Maximum CPU that restoration chips can build up to during a turn.")]
    [SerializeField] private int maxEnergy = 5;
    [SerializeField] private int handSize = 5;
    [SerializeField] private float enemyRowSpacing = 2.2f;

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

    [Header("Finale")]
    [Tooltip("Optional final video transition triggered after the Clone death animation finishes.")]
    [SerializeField] private AreaTransition finalVideoTransition;
    [SerializeField] private VideoClip finalVideoClip;
    [SerializeField] private bool triggerFinalVideoAfterCloneDeath = true;
    [Tooltip("Scene loaded after the ending video. Leave empty to remain in the combat scene.")]
    [SerializeField] private string creditsSceneName = "Credits";

    public CombatState State { get; private set; } = CombatState.Idle;
    public int Energy { get; private set; }
    public int MaxEnergy => maxEnergy;
    public Deck Deck { get; private set; }
    public IReadOnlyList<Enemy> Enemies => enemies;
        public int CorruptedInHand => Deck != null ? Deck.Hand.Count(c => c != null && c.isGlitch) : 0;

        public int SlashCount => slashCount;
        public int SlashesRemaining(CardData card) => card == null ? 0 : Mathf.Max(0, card.slashesToUnlock - slashCount);
        public bool IsPrimed(CardData card) => card != null && primedCards.Contains(card);
        public CardData GetEffectSource(CardData card) =>
        card != null && card.HasEffect(CardEffectType.DuplicateCard) ? lastPlayedCard : card;
    public CardTarget GetEffectiveTarget(CardData card)
    {
        CardData source = GetEffectSource(card);
        return source != null ? source.target : (card != null ? card.target : CardTarget.Self);
    }
    public bool WillDealDamage(CardData card)
    {
        CardData source = GetEffectSource(card);
        return source != null && source.HasEffect(CardEffectType.DealDamage);
    }

    private readonly List<Enemy> enemies = new List<Enemy>();
    private readonly HashSet<Enemy> runtimeEncounterEnemies = new HashSet<Enemy>();
    private Transform encounterEnemyRow;
    private CardData lastPlayedCard;
    private int turnNumber;
    private int slashCount;
    private bool heavySlashGenerated;
    private int heavySlashUsableTurn = int.MaxValue;
    private readonly HashSet<CardData> primedCards = new HashSet<CardData>();
    private Coroutine delayedRepositionRoutine;
    private float holdRepositionUntil;
    private bool endingCombat;
    private bool encounterStarted;
    private SaveData currentCheckpoint;

    public System.Action OnCombatChanged;
    public System.Action<CombatState> OnStateChanged;
    public System.Action<string> OnLog;
        public System.Action<CardData> OnCardPlayed;
        public System.Action<int, CardData> OnHandCorrupted;
        public System.Action<int> OnSlashCountChanged;
        public System.Action<CardData> OnBladeCharged;

    private void Start()
    {
        if (encounterStarted) return;

        RestorePendingCheckpoint();

        AreaEncounter activeEncounter = FindAnyObjectByType<AreaEncounter>();
        if (activeEncounter != null) activeEncounter.BeginEncounter();

        if (!encounterStarted) StartCombat();
    }

    public void SaveCheckpoint(int checkpointIndex, string checkpointName)
    {
        if (player == null) return;

        PlayTimeTracker tracker = PlayTimeTracker.EnsureInstance();
        Vector3 position = player.transform.position;
        int safeIndex = Mathf.Max(0, checkpointIndex);

        SaveData data = new SaveData
        {
            slotName = $"Area {safeIndex + 1}",
            sceneName = gameObject.scene.name,
            playSeconds = tracker != null ? tracker.TotalSeconds : 0f,
            fightIndex = safeIndex,
            hasPlayerState = true,
            playerHP = player.CurrentHP,
            playerShields = player.Shields,
            playerFocus = player.Focus,
            hasPlayerPosition = true,
            playerX = position.x,
            playerY = position.y,
            playerZ = position.z
        };

        currentCheckpoint = data;
        SaveSystem.SaveCurrentRun(data);
    }

        public bool SaveCurrentProgress()
    {
        if (currentCheckpoint == null)
        {
            AreaEncounter activeEncounter = FindAnyObjectByType<AreaEncounter>();
            if (activeEncounter == null) return false;

            SaveCheckpoint(activeEncounter.CheckpointIndex, activeEncounter.CheckpointName);
            return currentCheckpoint != null;
        }

        PlayTimeTracker tracker = PlayTimeTracker.EnsureInstance();
        currentCheckpoint.playSeconds = tracker != null ? tracker.TotalSeconds : currentCheckpoint.playSeconds;
        SaveSystem.SaveCurrentRun(currentCheckpoint);
        return true;
    }

    private void RestorePendingCheckpoint()
    {
        SaveData data = LoadGameMenu.ConsumePendingLoad();
        if (data == null) return;

        PlayTimeTracker.EnsureInstance().ResumeFrom(data.playSeconds);

        AreaEncounter[] encounters = Resources.FindObjectsOfTypeAll<AreaEncounter>()
            .Where(e => e != null && e.gameObject.scene == gameObject.scene)
            .OrderBy(e => e.CheckpointIndex)
            .ToArray();

        AreaEncounter selected = null;
        if (encounters.Length > 0)
        {
            int targetIndex = Mathf.Clamp(data.fightIndex, 0, encounters.Length - 1);
            selected = encounters.FirstOrDefault(e => e.CheckpointIndex == targetIndex) ?? encounters[targetIndex];

            foreach (AreaEncounter encounter in encounters)
                if (encounter != selected && encounter.gameObject.activeSelf)
                    encounter.gameObject.SetActive(false);

            if (!selected.gameObject.activeSelf)
                selected.gameObject.SetActive(true);
        }

        if (player != null)
        {
            bool restoreLegacyState = !data.hasPlayerState && data.playerHP > 0;
            if (data.hasPlayerState || restoreLegacyState)
                player.RestoreCheckpoint(data.playerHP, data.playerShields, data.playerFocus);

            Vector3 position = selected != null ? selected.CheckpointSpawnPosition : player.transform.position;
            if (data.hasPlayerPosition)
                position = new Vector3(data.playerX, data.playerY, data.playerZ);
            player.transform.position = position;
        }

        Debug.Log($"[Echoform] Restored checkpoint '{data.slotName}' (Area {data.fightIndex + 1}).");
    }

    public void StartCombat()
    {
        StartCombat(startingEnemies, null);
    }

        public void StartCombat(IEnumerable<Enemy> configuredEnemies)
    {
        StartCombat(configuredEnemies, null);
    }

        public void StartCombat(IEnumerable<Enemy> configuredEnemies, Transform enemyRowOverride)
    {
        encounterStarted = true;
        encounterEnemyRow = enemyRowOverride != null ? enemyRowOverride : enemyRow;

        foreach (Enemy runtimeEnemy in runtimeEncounterEnemies)
            if (runtimeEnemy != null) Destroy(runtimeEnemy.gameObject);
        runtimeEncounterEnemies.Clear();
        enemies.Clear();
        IEnumerable<Enemy> encounterConfig = configuredEnemies ?? Enumerable.Empty<Enemy>();
        foreach (Enemy configuredEnemy in encounterConfig.Where(e => e != null))
        {

            Enemy encounterEnemy = configuredEnemy.gameObject.scene.IsValid()
                ? configuredEnemy
                : Instantiate(configuredEnemy, encounterEnemyRow);

            enemies.Add(encounterEnemy);
            if (encounterEnemy != configuredEnemy)
                runtimeEncounterEnemies.Add(encounterEnemy);
        }

        turnNumber = 0;
        slashCount = 0;
        lastPlayedCard = null;
        heavySlashGenerated = false;
        heavySlashUsableTurn = int.MaxValue;
        endingCombat = false;
        primedCards.Clear();
        OnSlashCountChanged?.Invoke(slashCount);
        List<CardData> reusableDeck = startingDeck
            .Where(card => card != null && card != heavySlashChip)
            .ToList();
        Deck = new Deck(reusableDeck);
        foreach (var e in enemies) if (e is PlayerClone pc) pc.InitDeck(reusableDeck);
        RepositionEnemies();
        UpdateMergerTelegraph();

        Log("The Loom hums. Cut the thread.");
        StartPlayerTurn();
    }

    private void StartPlayerTurn()
    {
        if (CheckEndOfCombat()) return;

        SetState(CombatState.PlayerTurn);

        foreach (var e in enemies) e.SpawnedThisTurn = false;

        player.ResetBlock();
        Energy = energyPerTurn;
        Deck.DrawUpTo(handSize);

        turnNumber++;
        if (corruptEveryNTurns > 0 && turnNumber % corruptEveryNTurns == 0)
        {
            CardData copy = MakeCorruptedCopy();
            if (copy != null)
            {
                int corruptedSlot = Deck.CorruptHand(
                    copy,
                    handSize,
                    heavySlashGenerated ? heavySlashChip : null);
                if (corruptedSlot >= 0)
                {
                    Log($"The Loom copies your {(lastPlayedCard != null ? lastPlayedCard.cardName : "memory")} — now corrupted.");
                    OnHandCorrupted?.Invoke(corruptedSlot, copy);
                }
            }
        }

        if (ApplyOverload()) { OnCombatChanged?.Invoke(); return; }

        UpdateMergerTelegraph();

        Log("Your move.");
        OnCombatChanged?.Invoke();
    }

        public bool CanPlayCard(CardData card)
    {
        if (card == null || card.isGlitch) return false;
        if (endingCombat) return false;
        if (State != CombatState.PlayerTurn) return false;
        if (IsHeavySlash(card) && (!heavySlashGenerated || turnNumber < heavySlashUsableTurn)) return false;
        if (card.HasEffect(CardEffectType.DuplicateCard) && lastPlayedCard == null) return false;
        if (card.slashesToUnlock > 0 && slashCount < card.slashesToUnlock) return false;
        if (card.energyCost > Energy) return false;
        CardData effectSource = GetEffectSource(card);
        if (effectSource != null && effectSource.HasEffect(CardEffectType.GainShield) && !player.CanGainShield) return false;
        return true;
    }

    public bool TryPlayCard(CardData card, Enemy target = null)
    {
        if (State != CombatState.PlayerTurn) return false;
        if (endingCombat) return false;
        if (card == null || card.isGlitch) return false;
        if (IsHeavySlash(card) && (!heavySlashGenerated || turnNumber < heavySlashUsableTurn))
        { Log($"{card.cardName} is loaded, but it will be ready next turn."); return false; }
        if (card.HasEffect(CardEffectType.DuplicateCard) && lastPlayedCard == null)
        { Log("Echo has no previous chip to repeat."); return false; }
        if (card.slashesToUnlock > 0 && slashCount < card.slashesToUnlock)
        { Log($"{card.cardName} is inert — land {card.slashesToUnlock} slashes first ({slashCount}/{card.slashesToUnlock})."); return false; }
        if (card.energyCost > Energy) { Log("Not enough energy."); return false; }
        CardData effectSource = GetEffectSource(card);
        if (effectSource != null && effectSource.HasEffect(CardEffectType.GainShield) && !player.CanGainShield)
        { Log("Shields already full."); return false; }

        if (card.chargeBeforeUse && !primedCards.Contains(card))
        {
            Energy -= card.energyCost;
            primedCards.Add(card);
            Log($"{card.cardName}: the blade charges. Play it again to unleash.");
            OnBladeCharged?.Invoke(card);
            OnCardPlayed?.Invoke(card);
            OnCombatChanged?.Invoke();
            return true;
        }

        if (GetEffectiveTarget(card) == CardTarget.SingleEnemy && (target == null || target.IsDead)) return false;

        Energy -= card.energyCost;
        ResolveCard(card, target);

        if (IsHeavySlash(card))
        {
            slashCount = 0;
            heavySlashGenerated = false;
            heavySlashUsableTurn = int.MaxValue;
            OnSlashCountChanged?.Invoke(slashCount);
        }
        else if (card.slashesToUnlock > 0)
        {
            slashCount = 0;
            OnSlashCountChanged?.Invoke(slashCount);
        }
        else if (card.countsAsSlash)
        {
            RegisterBasicSlash();
        }
        primedCards.Remove(card);

        if (!card.HasEffect(CardEffectType.DuplicateCard)) lastPlayedCard = card;

        if (IsHeavySlash(card)) Deck.ConsumeGenerated(card);
        else Deck.ResolvePlayed(card);
        OnCardPlayed?.Invoke(card);

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
                case CardEffectType.GainEnergy:
                    Energy = Mathf.Clamp(Energy + effect.amount, 0, maxEnergy);
                    break;

                case CardEffectType.DuplicateCard:
                    if (lastPlayedCard != null)
                    {
                        CardData echoedCard = lastPlayedCard;
                        Log($"Echo instantly repeats {echoedCard.cardName}.");
                        ResolveCard(echoedCard, target);
                    }
                    break;
            }
        }
    }

    private void RegisterBasicSlash()
    {
        if (heavySlashGenerated) return;

        slashCount++;
        int threshold = heavySlashChip != null && heavySlashChip.slashesToUnlock > 0
            ? heavySlashChip.slashesToUnlock
            : 3;
        slashCount = Mathf.Min(slashCount, threshold);
        OnSlashCountChanged?.Invoke(slashCount);

        if (heavySlashChip == null || slashCount < threshold) return;

        heavySlashGenerated = true;
        heavySlashUsableTurn = turnNumber + 1;
        Deck.AddToHand(heavySlashChip);
        Log($"{heavySlashChip.cardName} loaded into the Neural Rack. Ready next turn.");
        OnBladeCharged?.Invoke(heavySlashChip);
    }

    private bool IsHeavySlash(CardData card) =>
        card != null && heavySlashChip != null && card == heavySlashChip;

        public void DealDamageToEnemy(Enemy enemy, int amount)
    {
        DamageResult result = enemy.TakeDamage(amount);
        if (!result.killed) return;

        bool animatedDeath = false;
        float repositionDelay = 0f;

        if (enemy is Slime slime)
        {
            List<SlimeSpawn> children = slime.PlanChildren(result.overkill);

            if (children.Count == 0)
                Log(result.overkill > 0 ? $"Clean cut — {slime.DisplayName} erased." : $"{slime.DisplayName} down.");
            else
                Log(result.overkill > 0 ? $"Overkill! {slime.DisplayName} splits, children wounded." : $"{slime.DisplayName} splits.");

            if (children.Count > 0)
            {
                slime.PlayDivideSfx();
                SpawnSlimeChildren(slime, children);
            }
            else
            {
                slime.PlayDeath();
                animatedDeath = true;
                repositionDelay = slime.DeathDuration;
            }
        }
        else
        {
            if (enemy is PlayerClone clone)
            {
                Log($"{enemy.DisplayName} unravels.");
                endingCombat = true;
                StartCoroutine(FinishCloneDeath(clone));
                UpdateMergerTelegraph();
                OnCombatChanged?.Invoke();
                return;
            }

            if (enemy is Merger merger)
                merger.PlayDeathSfx();

            Log($"{enemy.DisplayName} destroyed.");
        }

        RemoveEnemy(enemy, destroy: !animatedDeath);
        UpdateMergerTelegraph();
        RequestReposition(repositionDelay);
    }

    private void SpawnSlimeChildren(Slime parent, List<SlimeSpawn> children)
    {
        foreach (var spec in children)
        {
            Slime child = Instantiate(slimePrefab, parent.transform.position, Quaternion.identity, encounterEnemyRow);
            child.Configure(spec.tier, spec.hp);
            enemies.Add(child);
            runtimeEncounterEnemies.Add(child);
        }
    }

    public void EndTurn()
    {
        if (endingCombat) return;
        if (State != CombatState.PlayerTurn) return;
        Deck.DiscardHand(heavySlashGenerated ? heavySlashChip : null);
        StartCoroutine(EnemyTurn());
    }

    private IEnumerator FinishCloneDeath(PlayerClone clone)
    {
        if (clone != null)
            yield return clone.PlayDeathAndWait();

        RemoveEnemy(clone, destroy: false);
        endingCombat = false;
        SetState(CombatState.Win);
        Log("The thread is cut — for now.");
        OnCombatChanged?.Invoke();

        if (triggerFinalVideoAfterCloneDeath && finalVideoTransition != null && finalVideoClip != null)
            finalVideoTransition.PlayClipWithoutSwap(finalVideoClip, creditsSceneName);
        else if (!string.IsNullOrWhiteSpace(creditsSceneName))
            SceneManager.LoadScene(creditsSceneName);
    }

    private IEnumerator EnemyTurn()
    {
        SetState(CombatState.EnemyTurn);

        yield return RunMergePhase();
        if (CheckEndOfCombat()) yield break;

        foreach (var e in enemies.ToList())
        {
            if (e == null || e.IsDead || e.SpawnedThisTurn) continue;

            if (e is PlayerClone clone)
            {
                yield return clone.TakeTurn(player, Log);
                if (player.IsDead) SetState(CombatState.Lose);
                if (State == CombatState.Lose) yield break;
                yield return new WaitForSeconds(0.15f);
                continue;
            }

            if (e.intentType == IntentType.Attack)
            {
                int dmg = e.intentValue;
                var melee = e.GetComponent<EnemyMeleeAnimator>();
                if (melee != null)
                {

                    yield return melee.PlayAttack(player.transform, () =>
                    {
                        player.TakeDamage(dmg);
                        Log($"{e.DisplayName} strikes for {dmg}.");
                        if (player.IsDead) SetState(CombatState.Lose);
                    });
                }
                else
                {
                    player.TakeDamage(dmg);
                    Log($"{e.DisplayName} strikes for {dmg}.");
                    if (player.IsDead) SetState(CombatState.Lose);
                }
                if (State == CombatState.Lose) yield break;
                yield return new WaitForSeconds(0.15f);
            }
        }

        if (CheckEndOfCombat()) yield break;

        foreach (var e in enemies) e.RollIntent();

        StartPlayerTurn();
    }

    private IEnumerator RunMergePhase()
    {
        var eligible = enemies.OfType<Merger>()
                              .Where(m => !m.IsDead && !m.SpawnedThisTurn && m.CanFuse)
                              .OrderBy(m => (int)m.Tier)
                              .ToList();

        for (int i = 0; i + 1 < eligible.Count; i += 2)
        {
            Merger a = eligible[i];
            Merger b = eligible[i + 1];

            MergerTier fused = Merger.FusedTier(a.Tier, b.Tier);
            int carriedHP = a.CurrentHP + b.CurrentHP;
            Vector3 pos = (a.transform.position + b.transform.position) * 0.5f;

            a.PlayFusionSfx(pos);

            RemoveEnemy(a);
            RemoveEnemy(b);

            Merger merged = Instantiate(mergerPrefab, pos, Quaternion.identity, encounterEnemyRow);
            merged.Configure(fused, carriedHP);
            enemies.Add(merged);
            runtimeEncounterEnemies.Add(merged);

            if (merged.CurrentHP > carriedHP)
                Log($"Two mergers fuse into a {merged.DisplayName}. Its core stabilizes at {merged.CurrentHP}/{merged.MaxHP} HP.");
            else
                Log($"Two mergers fuse into a {merged.DisplayName} ({merged.CurrentHP}/{merged.MaxHP} HP).");
            yield return new WaitForSeconds(0.2f);
        }

        UpdateMergerTelegraph();
        RepositionEnemies();
    }

    private void UpdateMergerTelegraph()
    {
        var allMergers = enemies.OfType<Merger>().Where(m => !m.IsDead).ToList();
        foreach (Merger merger in allMergers)
            merger.IsFusing = false;

        var eligible = allMergers.Where(m => m.CanFuse && !m.SpawnedThisTurn)
                                 .OrderBy(m => (int)m.Tier)
                                 .ToList();

        int pairedCount = eligible.Count - eligible.Count % 2;
        for (int i = 0; i < pairedCount; i++)
            eligible[i].IsFusing = true;
    }

    private void RemoveEnemy(Enemy e, bool destroy = true)
    {
        enemies.Remove(e);
        runtimeEncounterEnemies.Remove(e);
        if (destroy && e != null) Destroy(e.gameObject);
    }

    private void RepositionEnemies()
    {
        if (encounterEnemyRow == null) return;

        int n = 0;
        for (int i = 0; i < enemies.Count; i++) if (enemies[i] != null && !enemies[i].KeepScenePosition) n++;
        if (n == 0) return;

        int perRow = Mathf.Max(1, enemiesPerRow);

        int slot = 0;
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] == null) continue;
            if (enemies[i].KeepScenePosition) continue;

            int row = slot / perRow;
            int col = slot % perRow;

            int inThisRow = Mathf.Min(perRow, n - row * perRow);
            float rowStartX = -(inThisRow - 1) * enemyRowSpacing * 0.5f;

            float x = rowStartX + col * enemyRowSpacing;
            float y = -row * rowSpacing + ((col % 2 == 1) ? columnStagger : 0f);

            Vector3 tierOffset = enemies[i] is Merger merger
                ? merger.FormationOffset
                : Vector3.zero;
            Vector3 pos = encounterEnemyRow.position + new Vector3(x, y, 0f) + tierOffset;
            enemies[i].transform.position = pos;

            if (sortByDepth)
            {
                var sr = enemies[i].GetComponentInChildren<SpriteRenderer>();
                if (sr != null) sr.sortingOrder = Mathf.RoundToInt(-pos.y * 100f);
            }

            slot++;
        }
    }

    private void RequestReposition(float delay = 0f)
    {
        if (delay > 0f)
            holdRepositionUntil = Mathf.Max(holdRepositionUntil, Time.time + delay);

        if (Time.time < holdRepositionUntil)
        {
            if (delayedRepositionRoutine == null)
                delayedRepositionRoutine = StartCoroutine(DelayedReposition());
            return;
        }

        RepositionEnemies();
    }

    private IEnumerator DelayedReposition()
    {
        while (Time.time < holdRepositionUntil)
            yield return null;

        delayedRepositionRoutine = null;
        RepositionEnemies();
    }

        private CardData MakeCorruptedCopy()
    {
        CardData source = lastPlayedCard != null ? lastPlayedCard : corruptedChip;
        if (source == null) return null;

        CardData copy = Instantiate(source);
        copy.isGlitch = true;
        copy.name = source.cardName + " (Corrupted)";
        return copy;
    }

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
            int dmg = corrupted - 2;
            player.TakeDamage(dmg);
            Log($"Overload: {corrupted} corrupted chips clog memory — {dmg} damage.");
            if (CheckEndOfCombat()) return true;
        }

        return false;
    }

    private bool CheckEndOfCombat()
    {
        if (endingCombat) return true;

        if (player != null && player.IsDead) { SetState(CombatState.Lose); Log("Vestige falls. The Loom prints copy №10."); return true; }
        if (enemies.All(e => e == null || e.IsDead)) { SetState(CombatState.Win); Log("The thread is cut — for now."); return true; }
        return false;
    }

    private void SetState(CombatState s)
    {
        bool enteringLose = s == CombatState.Lose && State != CombatState.Lose;
        State = s;
        if (enteringLose && vestige != null) vestige.PlayDeath();
        OnStateChanged?.Invoke(s);
    }

    private void Log(string message)
    {
        Debug.Log($"[Echoform] {message}");
        OnLog?.Invoke(message);
    }
}
