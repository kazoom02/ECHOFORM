using System.Collections.Generic;
using UnityEngine;

// =====================================================
// ECHOFORM — Deck
// Gere as pilhas de compra, mão, descarte e exílio do baralho, incluindo
// a compra, o baralhamento e a introdução de cartas geradas ou corrompidas.
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
                if (DiscardPile.Count == 0) break;
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

        public void ResolvePlayed(CardData card)
    {
        Hand.Remove(card);
        if (card.exhaustOnPlay) ExhaustPile.Add(card);
        else DiscardPile.Add(card);
        OnChanged?.Invoke();
    }

    public void DiscardHand(CardData retainedCard = null)
    {

        for (int i = Hand.Count - 1; i >= 0; i--)
        {
            if (Hand[i] != null && Hand[i].isGlitch) continue;
            if (retainedCard != null && Hand[i] == retainedCard) continue;
            DiscardPile.Add(Hand[i]);
            Hand.RemoveAt(i);
        }
        OnChanged?.Invoke();
    }

        public void DrawUpTo(int handCap)
    {
        int need = handCap - Hand.Count;
        if (need > 0) Draw(need);
    }

        public int CorruptHand(CardData glitch, int handCap, CardData protectedCard = null)
    {
        if (glitch == null) return -1;

        List<int> cleanSlots = new List<int>();
        for (int i = 0; i < Hand.Count; i++)
            if ((Hand[i] == null || !Hand[i].isGlitch) && Hand[i] != protectedCard)
                cleanSlots.Add(i);

        if (cleanSlots.Count == 0) return -1;
        int slot = cleanSlots[Random.Range(0, cleanSlots.Count)];
        DiscardPile.Add(Hand[slot]);
        Hand[slot] = glitch;
        OnChanged?.Invoke();
        return slot;
    }

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

        public void AddToHand(CardData card)
    {
        Hand.Add(card);
        OnChanged?.Invoke();
    }

        public void ConsumeGenerated(CardData card)
    {
        Hand.Remove(card);
        OnChanged?.Invoke();
    }

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
