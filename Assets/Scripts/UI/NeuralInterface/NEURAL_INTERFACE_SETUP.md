# Neural Memory Interface — Setup

The bottom HUD that racks Vestige's combat memories and runs the install sequence:
**eject → slide into the Neural Slot → INSTALLING MEMORY… → visor flash → Vestige attacks.**

## Files

Art — `Assets/Art/UI/`
- `HUD/HUD_Frame.png` — the OS-style console bar (header plate, 5 bays, CPU cluster)
- `HUD/NeuralSlot.png` — the central socket a chip installs into
- `HUD/CpuPip_On.png` / `CpuPip_Off.png` — energy pips
- `Chips/Chip_Base.png` — chip body · `Chip_Glow_White.png` — tintable glow · `Chip_Scanline.png`

Scripts — `Assets/Scripts/UI/NeuralInterface/`
- `NeuralInterfaceHUD.cs` — controller + install sequence
- `ChipView.cs` — one chip in the rack (binds a `CardData`)
- `VisorFlash.cs` — cyan visor flash
- `CpuCycleMeter.cs` — the CPU-cycle pips

## Hierarchy

```
Canvas  (Screen Space - Overlay, + Graphic Raycaster; scene needs an EventSystem)
└── NeuralHUD                      [NeuralInterfaceHUD]
    ├── HUD_Frame                  Image = HUD_Frame.png   (Raycast Target OFF)
    │   ├── HeaderLabel            TMP  "NEURAL MEMORY INTERFACE"
    │   ├── RackSlot_0 … RackSlot_4  empty RectTransforms, one over each bay
    │   └── CpuMeter               [CpuCycleMeter]
    │       └── Pip_0 Pip_1 Pip_2  Image = CpuPip_On.png
    ├── NeuralSlot                 Image = NeuralSlot.png  (Raycast Target OFF)
    │   └── InstallGroup           [CanvasGroup] alpha 0
    │       ├── InstallLabel       TMP  (INSTALLING MEMORY…)
    │       └── InstallBar         Image, Image Type = Filled / Horizontal
    └── VisorOverlay               Image (cyan, alpha 0)   [VisorFlash]
```

The **ChipView prefab** (drag into `chipPrefab`):
```
Chip  [ChipView] [CanvasGroup]   RectTransform ~230x250
├── Base       Image = Chip_Base.png     (Raycast Target ON  ← receives the click)
├── Glow       Image = Chip_Glow_White.png (Raycast Target OFF)
├── Icon       Image  (Raycast Target OFF)
├── NameText   TMP
└── CostText   TMP
```
On the ChipView component: Art→Icon, Glow→Glow, Name/Cost→the TMPs, Group→the CanvasGroup.

## Wiring NeuralInterfaceHUD

- **Combat** → your `CombatManager`. **Max Cycles** = 3 (your `energyPerTurn`).
- **Rack Slots** → RackSlot_0..4 · **Chip Prefab** → the ChipView prefab.
- **Neural Slot** → the NeuralSlot RectTransform · **Installed Scale** → e.g. `(1,1,1)` (shrink if the slot is smaller than a rack chip).
- **Install Group / Label / Bar** → the three under InstallGroup.
- **Visor Flash** → VisorOverlay · **Cpu Meter** → CpuMeter.
- **On Chip Installed** → hook Vestige's dash + attack (see below).

Note: `NeuralSlot` and the rack live under the same Canvas, so the chip's slide math (`neuralSlot.anchoredPosition`) just works. Keep NeuralSlot's pivot centered.

## How it drives combat

`NeuralInterfaceHUD` is a pure view over `CombatManager`:

- It subscribes to `CombatManager.OnCombatChanged` and rebuilds the rack from `combat.Deck.Hand` + refreshes the CPU pips from `combat.Energy` every time state changes (draw, play, new turn).
- Clicking a chip runs the install animation, then calls `combat.TryPlayCard(card, target)` — which spends energy and resolves the card. Single-target cards auto-hit the first living enemy; override `ResolveTarget()` for click-to-target.
- Glitch cards and cards you can't afford do a red shake instead (`Deny`).

You don't push data into the HUD — `CombatManager` already fires `OnCombatChanged`, so the rack stays in sync on its own.

## Hooking Vestige's attack

`onChipInstalled(CardData)` fires the instant the memory seats — before effects resolve — so the dash lines up with the hit. In the Inspector, add a listener that calls your Vestige animation, e.g. `VestigeController.PlayAttack(card)`. If you want the numbers to land on a specific animation frame, delay the `TryPlayCard` call by moving it behind a short `WaitForSeconds` after `onChipInstalled` in `InstallRoutine`.

## ChipView card frame

Use `ChipCard_Base.png` as the chip body and `ChipCard_Glow_White.png` as the tintable glow. The frame is authored at **440×500** (bay-friendly ~0.88 ratio). Place the child elements at these slots — set each child's anchors/pivot to center and use the fractions below (fraction × card size):

| Child | Slot | Center (x,y frac) | Size (w,h frac) |
|---|---|---|---|
| CostText (TMP, centered) | hex badge, top-left | 0.164, 0.144 | 0.145, 0.128 |
| Icon (Image) | main window | 0.500, 0.472 | 0.727, 0.432 |
| NameText (TMP, centered) | name plate | 0.500, 0.772 | 0.773, 0.096 |

So the ChipView prefab becomes:
```
Chip  [ChipView] [CanvasGroup]        RectTransform 440x500 (scale to taste)
├── Base       Image = ChipCard_Base.png        (Raycast Target ON)
├── Glow       Image = ChipCard_Glow_White.png  (Raycast Target OFF)  ← tinted by card.tint
├── Icon       Image  (anchored to the window slot, Raycast Target OFF)
├── CostText   TMP    (anchored to the badge slot)
└── NameText   TMP    (anchored to the plate slot)
```
The window has a dark inset built in, so cards with no `art` still read as a screen. `ChipView.Bind` already sets `glowImage.color = card.tint`, so assign `ChipCard_Glow_White.png` to the **Glow** field for per-type coloring.

## Card-back & 9-slice frame

- **`ChipCard_Back.png`** (440×500) — the draw-pile back, duplication emblem. Use on a draw-pile Image, or a face-down chip. No text.
- **`ChipCard_Frame9.png`** (256×256) — a reusable, stretchable border. In the **Sprite Editor** set **Border L/T/R/B = 48**, then on the Image set **Image Type = Sliced**. Now you can resize a card to any dimensions and the corners/rails stay crisp — handy for a big "card preview" popup or differently-sized rack chips. Put your window/icon/text on top of it.

## Materials (optional polish)

For the "power up" look, set the **Glow** and **Scanline** Image materials to an additive UI material. Without it, plain alpha still brightens — just softer.

## Test without combat

Leave **Combat** empty and call `Refresh()` won't populate a rack, so for a quick animation test drop a couple of ChipViews directly under a rack slot, assign a `CardData` in `card`, and click. `onChipInstalled` still fires.
