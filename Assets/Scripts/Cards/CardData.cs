using System.Collections.Generic;
using UnityEngine;

// =====================================================
// ECHOFORM — CardData
// A ScriptableObject so cards are authored in the Inspector,
// not in code. Each card is a list of actions (deal damage,
// gain block, heal, buff, draw, duplicate...) plus a target
// rule. Right-click in Project ▸ Create ▸ Echoform ▸ Card.
// =====================================================

public enum CardTarget
{
    SingleEnemy,   // player picks one enemy (Strike)
    AllEnemies,    // hits the whole row (Cleave)
    Self           // buffs / block / heal
}

public enum CardEffectType
{
    DealDamage,     // damage to the target(s); scaled by player Focus
    GainBlock,      // block for the player this turn
    Heal,           // restore player HP
    GainFocus,      // +Focus (Strength) — adds to every future attack
    DrawCards,      // draw N extra cards
    GainEnergy,     // refund / add energy this turn
    DuplicateCard,  // Echo — immediately repeat the last card's effects
    GainShield      // +N discrete shields; each negates one full enemy hit
}

[System.Serializable]
public struct CardEffect
{
    public CardEffectType type;
    [Tooltip("Damage, block, heal, focus, cards, or energy — depending on type.")]
    public int amount;
}

[CreateAssetMenu(fileName = "NewCard", menuName = "Echoform/Card")]
public class CardData : ScriptableObject
{
    [Header("Identity")]
    public string cardName = "Strike";
    [TextArea] public string description = "Deal 6 damage.";
    public Sprite art;
    public Color tint = Color.white;

    [Header("Play cost & targeting")]
    [Min(0)] public int energyCost = 1;
    public CardTarget target = CardTarget.SingleEnemy;

    [Header("What it does")]
    public List<CardEffect> effects = new List<CardEffect>();

    [Header("Flags")]
    [Tooltip("Loom 'Glitch' corruption cards: unplayable filler that clogs the hand.")]
    public bool isGlitch = false;
    [Tooltip("If true the card is removed from the deck for the rest of combat once played (e.g. Echo copies).")]
    public bool exhaustOnPlay = false;

    [Header("Charged Slash mechanic")]
    [Tooltip("Playing this card lands a 'slash' — it ticks the counter that unlocks charged abilities (set on the basic Attack).")]
    public bool countsAsSlash = false;
    [Tooltip("Slashes that must be landed this combat before this card can be played at all (0 = always available).")]
    [Min(0)] public int slashesToUnlock = 0;
    [Tooltip("Two-step weapon: the first play charges the blade (no effect, card stays in hand); playing it again unleashes its effects.")]
    public bool chargeBeforeUse = false;

    public bool HasEffect(CardEffectType t)
    {
        foreach (var e in effects)
            if (e.type == t) return true;
        return false;
    }
}
