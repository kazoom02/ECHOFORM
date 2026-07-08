# ECHOFORM — Design & Unity Build Document

*GameJam entry · theme "Duplication" · Unity 6 (Universal 2D / URP) · single-player card battler*

This document collects everything about ECHOFORM in one place: the story, the world and environment, the creatures and combat mechanics, the art direction, the Unity project setup, and the C# script architecture — including the core combat scripts that already live in `Assets/Scripts/`.

---

## 1. The pitch

A turn-based card battler where **killing an enemy creates more enemies**. The theme "Duplication" isn't a gimmick bolted onto one monster — it's the whole game: the enemies duplicate, one of your cards duplicates, and *you* are a duplicate. You win not by hitting hardest but by learning *when* to strike.

Layout and feel are modelled on *Slay the Spire*: Vestige stands bottom-left, enemies stack on the right, and the "I can see myself hitting them" feeling comes entirely from 2D juice — a lunge, a slash flash, an enemy flinch and knockback, screen shake, and a micro-freeze on impact (hitstop). No 3D needed; the side view is the argument *for* 2D.

---

## 2. Story

The world was kept alive by **the Loom**, a machine built to copy living things so nothing would ever truly die. It worked — until it forgot how to *stop*. Now it copies without end. The first things it touched were slimes: simple cells never meant to think, now dividing forever, an ocean of imperfect copies drowning everything.

You are **Vestige**, the ninth copy of a hero who fell to the Loom long ago. Each time you die, the Loom prints another you — a little more degraded than the last. But you carry the **memory-cards** of every version before you: their skills, their instincts, their last good moves. You fight with the accumulated hand of a thousand dead selves.

To end the copying you must reach the Loom's core and cut the thread. The cruel joke in your way: **to kill a slime is to make two more.** Every death is a birth.

That frame makes duplication the entire game rather than one enemy's trick.

---

## 3. World & environment

The setting is a decaying facility built around the Loom — cold, dim, humming with the sound of endless copying. Visually it reads as *neon-in-the-dark*: near-black surroundings lit by the cyan glow of Vestige's blade and the sickly light of the creatures.

For the jam scope, the "environment" is a single combat stage rather than an explorable world:

- A dark side-on arena, floor plane implied by lighting, not modelled in depth.
- **Vestige anchored bottom-left**, facing right.
- **Enemies stacked in a row on the right**, spaced along the x-axis (handled by `CombatManager.RepositionEnemies`).
- HUD framing: energy pip bottom-left, End-Turn bottom-right, the hand fanned along the bottom edge, a scrolling combat log.
- Lighting: one **dim Global Light 2D** for mood, a **cyan point light** parented to Vestige's blade, and a brief **point light flash at each impact point** tied to the hit event. Pair with **Bloom** in a URP Volume so the `#00FFFF` blade and slash arcs actually glow.

Progression across a run is expressed as three fights of escalating shape (see §5), each teaching one layer of the duplication puzzle, ending at the Loom's core.

---

## 4. Characters & creatures

**Vestige (player)** — jade/cyan swordsman, copy №9, drawn with a faint offset duplicate behind him (a copy of a copy). Combat stats live in `PlayerCombatant`: HP, temporary Block, and a **Focus** buff (Slay-the-Spire "Strength") that adds flat damage to every attack.

**Slime (splitter)** — coral, carries a faint *mitosis seam* marking where it will divide. Three tiers — **Slime (Large) → Blob (Medium) → Splitling (Small)** — halving as they split. Killing one spawns two of the next tier down; Small slimes die for good. Freshly split children sit out one turn before attacking. Punishes killing *fast*.

**The Prime (boss slime)** — same rules, ~1.5× stats. Drawn as two lobes already pulling apart with a glowing gold seam, so it reads as "about to split" at a glance. Anchors Fight III.

**Merger (fuser)** — violet, the inverse of the slime. It never splits; instead, if **two Mergers both survive a turn they fuse** into a bigger one, carrying their **combined HP** (so chipping is never wasted). Four tiers — **Ooze → Confluence → Amalgam → Colossus** — built from visibly fused lobes, with X'd-out dim eyes showing the copies they've absorbed (the Colossus has five dead eyes because it ate five). Punishes killing *slow*.

**Stretch creatures (only if time):** the **Breeder** (spits a copy of itself every turn it lives — burst it now), the **Mirror** (replays your last attack back at you), and the **Echo Wraith** (boss that plays cards pulled from your own discard pile — you fight a copy of your deck).

---

## 5. Core mechanics

**Turn structure.** Energy 3 per turn, draw 5. Block is temporary and clears at the start of each of your turns. On End Turn the hand is discarded, then the enemy turn runs its **merge phase first, then attacks**, then telegraphs next turn's intents.

**Starter cards.**

- **Strike** — deal damage to one enemy (scaled by Focus).
- **Cleave** — deal damage to all enemies (scaled by Focus).
- **Focus** — gain +Focus, snowballing every future attack.
- **Guard** — gain Block for the turn.
- **Mend** — heal.
- **Echo** — the player-side duplication card: copy the last card you played back into your hand (exhausts itself).

**Splitting.** A slime splits on death: Large → 2× Medium → 4× Small → gone. Clearing carelessly swarms you, because every kill is two new mouths.

**Overkill carry-through (the signature mechanic).** Leftover damage from the killing blow *tears into the split children sequentially*. A precise big hit spawns wounded children, or — with enough overkill — erases a child before it forms (a **"clean cut"**). This turns *when to strike* into real skill instead of a coin flip. It lives in `Slime.PlanChildren(overkill)`.

**Fusion.** Two surviving Mergers fuse into the next tier, summing their current HP. Fresh fusions sit out one turn. A telegraph (`Merger.IsFusing`) warns the player when a fuse is coming so it's a readable decision, not a surprise.

**Fight shapes.**

1. **Fight I — splitting + overkill.** A Slime (or Prime) that teaches the split, and rewards a stacked hit with a clean cut.
2. **Fight II — the Merger race.** Several Oozes; the pressure is to burst them before they snowball, and chipping is never wasted because HP carries into the fusion. Opposite instinct from Fight I.
3. **Fight III — the boss.** The Prime splits *and* Mergers fuse at once: "which threat do I feed?" — the tension of the whole game paying off.

**Stretch mechanic — deck corruption.** The Loom shoves a useless duplicate **"Glitch"** card into your deck each fight, so managing bloat becomes the long game and ties duplication into deckbuilding, not just combat. Groundwork is in place: `CardData.isGlitch` and `Deck.AddToDiscard`.

**Tuning knobs to watch in playtest:** split-child HP, Cleave cost, how much overkill should matter, and Merger tier attack values (they climb fast — Ooze 3 → Colossus 12; lower these first if Fight III feels brutal).

---

## 6. Art direction

Design rule: **the silhouette should tell you what the creature does.** Slimes wear a mitosis seam (about to divide); Mergers are built from fused lobes with the dead eyes of what they ate; Vestige has his offset duplicate marked №9; the card back is the Loom itself — threads weaving toward a single dividing cell.

**Palette:** coral slimes, violet mergers, jade/cyan (`#00FFFF`) Vestige blade and VFX (emissive, meant to bloom).

**Assets already produced** (in the prior working session, ready to import):

- A **vector sprite set** — editable SVG plus 512px PNG — for all four slime tiers, all four merger tiers, Vestige, and the card back.
- A **65-PNG Vestige character pack** extracted from the reference sheet, transparency-keyed and Unity-ready: `character/` (full side pose `vestige_side.png`), `parts/` (19 body pieces for a skeletal rig), `weapon/`, `effects/` (9 slash arcs + 18 particle bits), `poses/` (idle/run/dash/jump/attacks/5-frame death), plus `README_UNITY.md`.

**Import settings (do this once, up front):** Filter Mode = **Point (no filter)**, Compression = **None**, so crisp edges don't turn to mush. For the jam, use the whole `vestige_side.png` as one sprite (Path A) and only reach for the skeletal rig if time allows. Optional Blender path: model creatures, render orthographic to 2D sprite sheets, drop them in where placeholder blobs are.

---

## 7. Unity project setup

- **Version:** Unity 6 (project is on 6000.3.17f1; the jam plan was 6.5, which deprecates the Built-In Render Pipeline — so URP is the unambiguous choice now).
- **Template:** **Universal 2D** (URP) — ships the 2D Renderer and Light2D out of the box, which is what makes the neon glow cheap.
- **Packages to add when needed** (Window ▸ Package Manager): *2D Animation* + *PSD Importer* only if you go the skeletal-rig route.
- **Lighting:** add a Global Light 2D first, then point lights for the blade and impact flashes; put Bloom in a URP Volume.
- **Game feel to add on the UI/VFX pass:** hitstop (`Time.timeScale = 0` for ~0.05s on impact, then restore), screen shake (damped random camera offset, or Cinemachine Impulse), and a slash sprite scaled+faded on the target.

---

## 8. Script architecture

Scripts live under `Assets/Scripts/` in three folders. The **core combat systems are already written** (this batch); the UI/VFX layer is intentionally left to hook onto the events these expose.

### Built now — `Assets/Scripts/`

**Cards/**

- `CardData.cs` — a `ScriptableObject` (Create ▸ Echoform ▸ Card). Authors every card in the Inspector as a list of `CardEffect`s (DealDamage, GainBlock, Heal, GainFocus, DrawCards, GainEnergy, DuplicateCard) plus a `CardTarget` (SingleEnemy / AllEnemies / Self). Includes `isGlitch` and `exhaustOnPlay` flags for Echo and future corruption.

**Combat/**

- `PlayerCombatant.cs` — Vestige's HP, temporary Block, and the Focus buff; block-then-HP damage.
- `Deck.cs` — plain C# draw/hand/discard/exhaust piles with auto-reshuffle, Echo `AddToHand`, and corruption `AddToDiscard`.
- `CombatManager.cs` — the turn-loop state machine: deals hands, spends energy, resolves cards, applies **overkill carry-through** into slime splits, runs the enemy **merge-then-attack** phase, and checks win/lose. Exposes `OnCombatChanged`, `OnStateChanged`, and `OnLog` events for a UI layer.

**Enemies/**

- `Enemy.cs` — abstract base: HP, Block, telegraphed intent, and a `TakeDamage` that returns the **overkill** (the leftover that powers clean cuts).
- `Slime.cs` — tiers, per-tier stat tables, `PlanChildren(overkill)` (the overkill/clean-cut logic), and a `Split()` coroutine for the visual.
- `Merger.cs` — tiers, `FusedTier()` escalation, summed-HP fusion config, and the `IsFusing` telegraph flag.

### To build next (in order)

1. **Card & enemy prefabs + a combat scene** — a Slime prefab (with `Slime`), a Merger prefab (with `Merger`), a `CombatManager` in the scene wired to the player, the enemy row anchor, and a starting deck of `CardData` assets. This makes the current code playable.
2. **Card hand UI + targeting** — render the hand, click/drag a card onto an enemy, call `CombatManager.TryPlayCard(card, target)`, and an End-Turn button calling `CombatManager.EndTurn()`.
3. **Attack juice / VFX** — subscribe to combat events to fire the lunge, slash arc, flinch, knockback, screen shake, hitstop, and impact light.
4. **Content pass** — author the three fights and the starter deck as assets; tune the knobs from §5.
5. **Stretch** — deck corruption (Glitch cards), then Mirror / Breeder / Echo Wraith.

> Note: Kigy's earlier "DV Project" (a farming/life-sim: `InventoryManager`, `TimeManager`, `DayNightVisuals`, `QueenInteractable`, etc.) is a *different* game. It's not reusable code for ECHOFORM, but it's a good reference for the author's own C# conventions (`[Header]`/`[SerializeField]`, singletons, clean structure) — which these scripts follow.

---

## 9. Immediate next step

Create the two enemy prefabs and a combat scene, drop a `CombatManager` in, and author a handful of `CardData` assets (Strike/Cleave/Focus/Guard/Mend/Echo) plus one Slime for Fight I. At that point the split + overkill loop is playable in-engine, and the UI pass can begin.
