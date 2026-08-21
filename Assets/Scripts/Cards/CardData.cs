using System.Collections.Generic;
using UnityEngine;

// =====================================================
// ECHOFORM — CardData
// Define os dados, os alvos, os efeitos e a apresentação das cartas
// criadas como ScriptableObjects no Editor do Unity.
// =====================================================

public enum CardTarget
{
    SingleEnemy,
    AllEnemies,
    Self
}

public enum CardEffectType
{
    DealDamage,
    GainBlock,
    Heal,
    GainFocus,
    DrawCards,
    GainEnergy,
    DuplicateCard,
    GainShield
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
