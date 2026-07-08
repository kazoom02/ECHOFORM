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
        // Corrupted chips burn away instead of recirculating, so the Loom's
        // "1 every N turns" cadence stays exact rather than piling up in the deck.
        foreach (var card in Hand)
        {
            if (card != null && card.isGlitch) ExhaustPile.Add(card);
            else DiscardPile.Add(card);
        }
        Hand.Clear();
        OnChanged?.Invoke();
    }

    /// <summary>Loom corruption: overwrite a random hand slot with a glitch chip, keeping hand size.</summary>
    public void CorruptHand(CardData glitch, int handCap)
    {
        if (glitch == null) return;
        if (Hand.Count == 0 || Hand.Count < handCap)
        {
            Hand.Add(glitch);                 // free slot — just drop it in
        }
        else
        {
            int i = Random.Range(0, Hand.Count);
            DiscardPile.Add(Hand[i]);          // displaced memory recirculates
            Hand[i] = glitch;                  // corruption takes the slot
        }
        OnChanged?.Invoke();
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
