using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// =====================================================
// ECHOFORM — PlayerClone
// Implementa o clone do jogador, que copia o baralho e executa cartas
// durante o próprio turno, com atributos e animações de combate próprios.
// =====================================================

public class PlayerClone : Enemy
{
    private const float MinimumDeathWait = 0.75f;

    [Header("Clone turn")]
    [SerializeField] private int energyPerTurn = 3;
    [SerializeField] private int maxEnergy = 5;
    [SerializeField] private int handSize = 5;
    [Tooltip("Hard action limit for the boss. Keep at 1 so it plays only one chip per turn.")]
    [Min(1)] [SerializeField] private int movesPerTurn = 1;
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

    [Header("SFX")]
    [Tooltip("The Clone reuses the player's one-shot SFX player.")]
    [SerializeField] private SfxPlayer sfx;
    [Tooltip("The same death sound used by the player.")]
    [SerializeField] private AudioClip deathSfx;

    public int Shields { get; private set; }
    public int Focus { get; private set; }
    public int Energy { get; private set; }

    private Deck deck;
    private CardData lastPlayed;
    private bool deathPlayed;

    public override string DisplayName => "Echo of Vestige";

    protected override void Awake()
    {
        if (maxHP <= 10) maxHP = 90;
        base.Awake();
        if (animator == null) animator = GetComponent<Animator>();
        if (melee == null)    melee = GetComponent<EnemyMeleeAnimator>();
        if (sfx == null)      sfx = GetComponent<SfxPlayer>();
        if (deck == null && deckOverride.Count > 0) deck = new Deck(deckOverride);
    }

        public void InitDeck(IEnumerable<CardData> playerDeck)
    {
        if (playerDeck != null) deck = new Deck(playerDeck);
        lastPlayed = null;
    }

    public override void RollIntent()
    {
        intentType = IntentType.Attack;
        intentValue = 0;
    }

    public override DamageResult TakeDamage(int amount)
    {
        amount = Mathf.Max(0, amount);
        if (amount <= 0) return DamageResult.Survived;

        if (Shields > 0)
        {
            Shields--;
            OnStateChanged?.Invoke();
            return DamageResult.Survived;
        }
        return base.TakeDamage(amount);
    }

        public IEnumerator TakeTurn(PlayerCombatant player, System.Action<string> log)
    {
        if (IsDead) yield break;
        if (deck == null) { log?.Invoke($"{DisplayName} has no deck."); yield break; }

        Block = 0;
        Energy = energyPerTurn;
        deck.DrawUpTo(handSize);
        OnStateChanged?.Invoke();

        for (int move = 0; move < movesPerTurn; move++)
        {
            CardData card = ChooseCard();
            if (card == null) break;

            Energy -= card.energyCost;

            CardData effectSource = GetEffectSource(card);
            bool isAttack = effectSource != null && effectSource.HasEffect(CardEffectType.DealDamage);

            if (isAttack && melee != null)
            {

                yield return melee.PlayAttack(player.transform, () => ResolveCard(card, player, log));
            }
            else
            {
                if (isAttack) Play(attackState);
                ResolveCard(card, player, log);
                if (isAttack && attackHold > 0f) yield return new WaitForSeconds(attackHold);
                Play(idleState);
            }

            if (!card.HasEffect(CardEffectType.DuplicateCard))
                lastPlayed = card;
            deck.ResolvePlayed(card);
            OnStateChanged?.Invoke();

            if (player.IsDead) yield break;
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

        if (sfx != null) sfx.PlayDetached(deathSfx);

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

    private CardData ChooseCard()
    {
        var playable = deck.Hand.Where(CanChooseCard).ToList();
        if (playable.Count == 0) return null;
        return playable[Random.Range(0, playable.Count)];
    }

    private bool CanChooseCard(CardData card)
    {
        if (card == null || card.isGlitch || card.energyCost > Energy) return false;
        if (card.HasEffect(CardEffectType.DuplicateCard) && lastPlayed == null) return false;

        CardData effectSource = GetEffectSource(card);
        if (effectSource == null) return false;
        if (effectSource.HasEffect(CardEffectType.GainEnergy)) return false;
        if (effectSource.HasEffect(CardEffectType.GainShield) && Shields >= maxShields) return false;
        return true;
    }

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
                case CardEffectType.GainEnergy: Energy = Mathf.Clamp(Energy + effect.amount, 0, maxEnergy); break;

                case CardEffectType.DuplicateCard:
                    if (lastPlayed != null)
                    {
                        CardData echoedCard = lastPlayed;
                        log?.Invoke($"{DisplayName} instantly echoes {echoedCard.cardName}.");
                        ResolveCard(echoedCard, player, log);
                    }
                    break;
            }
        }
    }

    private CardData GetEffectSource(CardData card) =>
        card != null && card.HasEffect(CardEffectType.DuplicateCard) ? lastPlayed : card;
}
