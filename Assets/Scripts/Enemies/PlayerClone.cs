using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// =====================================================
// ECHOFORM — PlayerClone
// Area 3 boss: a literal copy of the player. CombatManager hands it a fresh
// copy of the player's deck at combat start. On its turn it draws a hand and
// plays cards until it runs out of energy — DealDamage hits Vestige, while
// GainBlock / GainShield / Heal / GainFocus / DrawCards / GainEnergy /
// DuplicateCard apply to the Clone itself. A true mirror match.
//
// Animation follows the same Animator.Play(stateName) convention as
// VestigeCombatAnimator: the CloneController just needs states named to match
// the fields below (Idle / Attack / Walk). Idle plays by default; Attack fires
// when the Clone plays a damage card.
//
// Drop it into an Area 3 scene and add it to the CombatManager's Starting
// Enemies. Set its Max HP in the Inspector (defaults to 90).
// =====================================================

public class PlayerClone : Enemy
{
    private const float MinimumDeathWait = 0.75f;

    [Header("Clone turn")]
    [SerializeField] private int energyPerTurn = 3;
    [SerializeField] private int handSize = 5;
    [Tooltip("Pause between each card the Clone plays, so its turn reads clearly.")]
    [SerializeField] private float playInterval = 0.5f;

    [Header("Clone resources (player-mirror)")]
    [Tooltip("Max discrete shields, like the player. Each negates one full hit.")]
    [SerializeField] private int maxShields = 4;
    [Tooltip("Optional custom deck. Leave EMPTY to use a copy of the player's starting deck (set by CombatManager).")]
    [SerializeField] private List<CardData> deckOverride = new List<CardData>();

    [Header("Animation (Animator.Play — states must exist in CloneController)")]
    [Tooltip("The Clone's Animator. Auto-found on this object if left empty.")]
    [SerializeField] private Animator animator;
    [SerializeField] private string idleState   = "Idle";
    [SerializeField] private string attackState = "Attack";
    [SerializeField] private string walkState   = "Walking";
    [Tooltip("Optional. If assigned, damage cards reuse this walk-in/attack/walk-back choreography "
        + "(the enemy-side attack). Leave empty to just play the Attack state in place.")]
    [SerializeField] private EnemyMeleeAnimator melee;
    [Tooltip("Seconds the Attack state is held before returning to Idle after a damage card.")]
    [SerializeField] private float attackHold = 0.45f;

    [Header("Death")]
    [Tooltip("Animator state played when the Clone reaches 0 HP.")]
    [SerializeField] private string deathState = "CloneDeath";
    [Tooltip("Fallback wait if the death clip cannot be found in the controller.")]
    [SerializeField] private float deathDuration = 0.75f;

    public int Shields { get; private set; }
    public int Focus { get; private set; }
    public int Energy { get; private set; }

    private Deck deck;
    private CardData lastPlayed;
    private bool deathPlayed;

    public override string DisplayName => "Echo of Vestige";

    protected override void Awake()
    {
        if (maxHP <= 10) maxHP = 90;          // boss default if left at the base value
        base.Awake();
        if (animator == null) animator = GetComponent<Animator>();
        if (melee == null)    melee = GetComponent<EnemyMeleeAnimator>();
        if (deck == null && deckOverride.Count > 0) deck = new Deck(deckOverride);
    }

    /// <summary>Give the Clone a fresh copy of the player's deck. Called by CombatManager at combat start.</summary>
    public void InitDeck(IEnumerable<CardData> playerDeck)
    {
        if (playerDeck != null) deck = new Deck(playerDeck);
    }

    public override void RollIntent()
    {
        intentType = IntentType.Attack;   // it always acts on its turn
        intentValue = 0;                  // variable — it plays a whole hand
    }

    // Player attacks the Clone: a shield eats one whole hit, else normal block-then-HP.
    public override DamageResult TakeDamage(int amount)
    {
        amount = Mathf.Max(0, amount);
        if (amount <= 0) return DamageResult.Survived;

        if (Shields > 0)
        {
            Shields--;
            OnStateChanged?.Invoke();
            return DamageResult.Survived;   // shield absorbs the whole hit, then breaks
        }
        return base.TakeDamage(amount);
    }

    /// <summary>Run the Clone's whole turn: reset block, draw a hand, play cards vs the player.</summary>
    public IEnumerator TakeTurn(PlayerCombatant player, System.Action<string> log)
    {
        if (IsDead) yield break;
        if (deck == null) { log?.Invoke($"{DisplayName} has no deck."); yield break; }

        Block = 0;                    // block is fresh each of the Clone's own turns
        Energy = energyPerTurn;
        deck.DrawUpTo(handSize);
        OnStateChanged?.Invoke();

        while (true)
        {
            CardData card = ChooseCard();
            if (card == null) break;   // nothing affordable / playable left

            Energy -= card.energyCost;

            bool isAttack = card.HasEffect(CardEffectType.DealDamage);

            if (isAttack && melee != null)
            {
                // reuse the enemy walk-in / attack / walk-back choreography;
                // the card resolves on the hit frame
                yield return melee.PlayAttack(player.transform, () => ResolveCard(card, player, log));
            }
            else
            {
                if (isAttack) Play(attackState);   // no melee animator: swing in place
                ResolveCard(card, player, log);
                if (isAttack && attackHold > 0f) yield return new WaitForSeconds(attackHold);
                Play(idleState);
            }

            lastPlayed = card;
            deck.ResolvePlayed(card);
            OnStateChanged?.Invoke();

            if (player.IsDead) yield break;                          // killed Vestige — stop instantly
            if (playInterval > 0f) yield return new WaitForSeconds(playInterval);
        }

        Play(idleState);
        deck.DiscardHand();
        OnStateChanged?.Invoke();
    }

    public IEnumerator PlayDeathAndWait()
    {
        if (deathPlayed) yield break;
        deathPlayed = true;

        if (animator == null) animator = GetComponent<Animator>();

        if (animator != null && !string.IsNullOrEmpty(deathState))
        {
            animator.Play(deathState, 0, 0f);
            animator.Update(0f);
        }
        else
        {
            Play(deathState);
        }
        OnStateChanged?.Invoke();

        float wait = Mathf.Max(MinimumDeathWait, deathDuration, ResolveClipLength(deathState, MinimumDeathWait));
        float elapsed = 0f;
        while (elapsed < wait)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    // Same approach as VestigeCombatAnimator: play a state directly by name.
    private void Play(string state)
    {
        if (animator != null && !string.IsNullOrEmpty(state)) animator.Play(state);
    }

    private float ResolveClipLength(string stateName, float fallback)
    {
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip != null && clip.name == stateName)
                    return clip.length;
            }
        }

        return Mathf.Max(0.05f, fallback);
    }

    // Pick a random affordable, non-glitch card; never gain shields when already capped.
    private CardData ChooseCard()
    {
        var playable = deck.Hand.Where(c =>
            c != null && !c.isGlitch && c.energyCost <= Energy &&
            !(c.HasEffect(CardEffectType.GainShield) && Shields >= maxShields)
        ).ToList();
        if (playable.Count == 0) return null;
        return playable[Random.Range(0, playable.Count)];
    }

    // Card effects from the Clone's perspective: damage -> player, everything else -> self.
    private void ResolveCard(CardData card, PlayerCombatant player, System.Action<string> log)
    {
        foreach (var effect in card.effects)
        {
            switch (effect.type)
            {
                case CardEffectType.DealDamage:
                    int dmg = effect.amount + Focus;
                    player.TakeDamage(dmg);
                    log?.Invoke($"{DisplayName} plays {card.cardName} — {dmg} to Vestige.");
                    break;

                case CardEffectType.GainBlock:  Block += Mathf.Max(0, effect.amount); break;
                case CardEffectType.GainShield: Shields = Mathf.Clamp(Shields + effect.amount, 0, maxShields); break;
                case CardEffectType.Heal:       CurrentHP = Mathf.Min(MaxHP, CurrentHP + Mathf.Max(0, effect.amount)); break;
                case CardEffectType.GainFocus:  Focus += effect.amount; break;
                case CardEffectType.DrawCards:  deck.Draw(effect.amount); break;
                case CardEffectType.GainEnergy: Energy += effect.amount; break;

                case CardEffectType.DuplicateCard:
                    if (lastPlayed != null) { deck.AddToHand(lastPlayed); log?.Invoke($"{DisplayName} echoes {lastPlayed.cardName}."); }
                    break;
            }
        }
    }
}
