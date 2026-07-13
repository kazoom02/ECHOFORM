using System.Collections.Generic;
using UnityEngine;

// =====================================================
// ECHOFORM — Deck
// Plain C# helper (not a MonoBehaviour) that manages the
// draw pile, hand, discard and exhaust piles. Auto-reshuffles
// discard into draw when empty. Supports generated mechanic chips
// (AddToHand) and Loom "Glitch" corruption (AddToDiscard).
// =====================================================

public class Deck
{
    public readonly List<CardData> DrawPile = new List<CardData>();
    public readonly List<CardData> Hand = new List<CardData>();
    public readonly List<CardData> DiscardPile = new List<CardData>();
    public readonly List<CardData> ExhaustPile = new List<CardData>();

    public System.Action OnChanged;

    public Deck(IEnumerable<CardData> startingCards)
    {
        DrawPile.AddRange(startingCards);
        Shuffle(DrawPile);
    }

    public void Draw(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (DrawPile.Count == 0)
            {
                if (DiscardPile.Count == 0) break;   // truly out of cards
                DrawPile.AddRange(DiscardPile);
                DiscardPile.Clear();
                Shuffle(DrawPile);
            }

            int last = DrawPile.Count - 1;
            Hand.Add(DrawPile[last]);
            DrawPile.RemoveAt(last);
        }
        OnChanged?.Invoke();
    }

    /// <summary>Move a played card to discard or exhaust depending on its flag.</summary>
    public void ResolvePlayed(CardData card)
    {
        Hand.Remove(card);
        if (card.exhaustOnPlay) ExhaustPile.Add(card);
        else DiscardPile.Add(card);
        OnChanged?.Invoke();
    }

    public void DiscardHand(CardData retainedCard = null)
    {
        // Corruption stays stuck in neural memory. A generated mechanic chip can
        // also remain installed so it is available on the following turn.
        for (int i = Hand.Count - 1; i >= 0; i--)
        {
            if (Hand[i] != null && Hand[i].isGlitch) continue;   // corrupted stays
            if (retainedCard != null && Hand[i] == retainedCard) continue;
            DiscardPile.Add(Hand[i]);
            Hand.RemoveAt(i);
        }
        OnChanged?.Invoke();
    }

    /// <summary>Draw until the hand is refilled to the cap, leaving stuck corruption in place.</summary>
    public void DrawUpTo(int handCap)
    {
        int need = handCap - Hand.Count;
        if (need > 0) Draw(need);
    }

    /// <summary>Loom corruption: overwrite a random CLEAN slot with a glitch chip. Permanent until purged. Returns the hand slot, or -1 if nothing changed.</summary>
    public int CorruptHand(CardData glitch, int handCap, CardData protectedCard = null)
    {
        if (glitch == null) return -1;

        List<int> cleanSlots = new List<int>();
        for (int i = 0; i < Hand.Count; i++)
            if ((Hand[i] == null || !Hand[i].isGlitch) && Hand[i] != protectedCard)
                cleanSlots.Add(i);

        if (cleanSlots.Count == 0) return -1;              // already fully corrupted
        int slot = cleanSlots[Random.Range(0, cleanSlots.Count)];
        DiscardPile.Add(Hand[slot]);                        // displaced memory recirculates
        Hand[slot] = glitch;                                // corruption takes the slot
        OnChanged?.Invoke();
        return slot;
    }

    /// <summary>Counterplay: purge one corrupted chip from hand for the rest of combat. Returns true if one was removed.</summary>
    public bool PurgeOneCorrupted()
    {
        for (int i = 0; i < Hand.Count; i++)
            if (Hand[i] != null && Hand[i].isGlitch)
            {
                ExhaustPile.Add(Hand[i]);
                Hand.RemoveAt(i);
                OnChanged?.Invoke();
                return true;
            }
        return false;
    }

    /// <summary>Drop a generated mechanic chip straight into the hand.</summary>
    public void AddToHand(CardData card)
    {
        Hand.Add(card);
        OnChanged?.Invoke();
    }

    /// <summary>Consume a generated mechanic chip without putting it into the reusable deck.</summary>
    public void ConsumeGenerated(CardData card)
    {
        Hand.Remove(card);
        OnChanged?.Invoke();
    }

    /// <summary>Loom corruption: shove a Glitch card into the discard so it recirculates.</summary>
    public void AddToDiscard(CardData card)
    {
        DiscardPile.Add(card);
        OnChanged?.Invoke();
    }

    private static void Shuffle(List<CardData> pile)
    {
        for (int i = pile.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pile[i], pile[j]) = (pile[j], pile[i]);
        }
    }
}
