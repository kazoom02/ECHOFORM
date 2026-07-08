using System.Collections.Generic;
using UnityEngine;

// =====================================================
// ECHOFORM — Deck
// Plain C# helper (not a MonoBehaviour) that manages the
// draw pile, hand, discard and exhaust piles. Auto-reshuffles
// discard into draw when empty. Supports Echo duplication
// (AddToHand) and future Loom "Glitch" corruption (AddToDiscard).
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

    public void DiscardHand()
    {
        // Corruption stays stuck in neural memory — only clean chips cycle out.
        // This is what lets corrupted chips accumulate toward overload.
        for (int i = Hand.Count - 1; i >= 0; i--)
        {
            if (Hand[i] != null && Hand[i].isGlitch) continue;   // corrupted stays
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

    /// <summary>Loom corruption: overwrite a random CLEAN slot with a glitch chip. Permanent until purged.</summary>
    public void CorruptHand(CardData glitch, int handCap)
    {
        if (glitch == null) return;

        List<int> cleanSlots = new List<int>();
        for (int i = 0; i < Hand.Count; i++)
            if (Hand[i] == null || !Hand[i].isGlitch) cleanSlots.Add(i);

        if (cleanSlots.Count == 0) return;                 // already fully corrupted
        int slot = cleanSlots[Random.Range(0, cleanSlots.Count)];
        DiscardPile.Add(Hand[slot]);                        // displaced memory recirculates
        Hand[slot] = glitch;                                // corruption takes the slot
        OnChanged?.Invoke();
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

    /// <summary>Echo: drop a duplicate straight into the hand.</summary>
    public void AddToHand(CardData card)
    {
        Hand.Add(card);
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
