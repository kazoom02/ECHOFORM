# ECHOFORM — Fight II (Area 2): The Merger Race — Encounter Spec

Second of three fights. Teaches the **opposite instinct** to Area 1: slimes punish killing fast; Mergers punish killing *slow*. Tuned against the real values in the project.

## Reference values (from code/assets)
- **Player:** 60 HP, 3 energy/turn, draw 5. Attack 6 (1e, single, +Focus), Cleave 4 (2e, all, +Focus), Focus +2 (1e), Heal 8 (1e), Guard (1e).
- **Merger tiers:** Ooze 10 HP / 3 ATK · Confluence 20 / 6 · Amalgam 34 / 9 · Colossus 52 / 12.
- **Fusion rule (`RunMergePhase`):** at the start of the enemy turn, living Mergers pair up two-at-a-time → each pair becomes the next tier up, carrying their **combined current HP**. Fresh fusions sit out one turn (`SpawnedThisTurn`). So **fusions this turn = floor(livingMergers / 2)**.

## The encounter
**3 Oozes** in a row (10 HP, 3 ATK each). No pre-placed higher tiers.

Why 3: it's the smallest count that *forces the lesson*. With a starter deck you can't wipe all three in turn 1, so if you play single-target you WILL feed a fusion — the player sees the mechanic happen — but it never snowballs into two simultaneous fusions (which 4 Oozes would).

## The one rule the fight surfaces
> At the end of your turn, never leave **2 living Mergers** standing — every pair fuses.
Leave **0 or 1** alive → no fusion. Leave 2 → one Confluence. Leave 3 → one Confluence **plus** a leftover Ooze that fuses *with* the Confluence next turn into an Amalgam.

And the payoff insight (why chipping isn't wasted): a fusion inherits **current** HP, so damage you spread beforehand shrinks the thing you're about to create.

## Naive line vs intended line (same 3 Oozes)
**Naive (single-target):** T1 two Attacks kill Ooze A (12 dmg). B & C survive at full → fuse into a **full 20 HP** Confluence. You now grind 20 HP and eat 6/turn. Slower, you take hits.

**Intended (spread + finish):** T1 Cleave (all → 6 HP each) + Attack to finish one (A dead). B & C survive but *already at 6 HP* → fuse into a **12 HP** Confluence, not 20. T2 two Attacks (12) delete it. Fight over, barely scratched. This makes "chip damage carries into the fusion" tangible: the Cleave both softened the wave and shrank the merge.

Note: the fresh Confluence sits out the enemy turn it's born, so the fusion turn deals **0 damage** — a built-in breather that keeps the fight fair while the lesson lands.

## Difficulty knobs
- **Easier:** start Oozes at 8 HP so a Focus-boosted Cleave one-shots the wave.
- **Harder (or a "Fight II+"):** 4 Oozes → two pairs can fuse in one turn (two Confluences) — a real punish for greedy single-target play. Or pre-place 1 Confluence among 2 Oozes.
- **First lever if it feels brutal:** Merger ATK climbs fast (Confluence 6 → Colossus 12). Lower Confluence to 5 before touching HP.

## Bug/tuning flags spotted
- **Guard grants only 1 Block** (`Guard.asset amount: 1`) — near-useless vs Ooze 3 / Confluence 6. Bump to ~5–6 so defending is a real option in this fight.
- Consider giving Cleave a small Focus synergy callout in the tutorial text, since it's the intended answer here.
