# Neural Interface — Full In-Game Setup (start to finish)

Follow top to bottom. ~20 min. Assumes your `CombatManager` scene already works headless (it logs to Console).

---

## 0 · Import the art

Select each PNG in `Assets/Art/UI/…` and in the Inspector set:
- **Texture Type:** Sprite (2D and UI)
- **Sprite Mode:** Single
- **Max Size:** 2048 · **Compression:** Normal · click **Apply**

Extra:
- `ChipCard_Frame9.png` → **Open Sprite Editor** → set **Border L/T/R/B = 48** → Apply. (Only needed if you use the stretchable frame.)

Sprites you'll use: `HUD/HUD_Frame`, `HUD/NeuralSlot`, `HUD/CpuPip_On`, `HUD/CpuPip_Off`, `Chips/ChipCard_Base`, `Chips/ChipCard_Glow_White`, `Chips/ChipCard_Glitch`, `Chips/Chip_Base`, `Chips/Chip_Glow_White`, `Chips/Chip_Scanline`.

---

## 1 · Canvas + EventSystem

If you don't already have a UI Canvas:
1. **GameObject ▸ UI ▸ Canvas** (this also creates an **EventSystem** — needed for clicks).
2. On the Canvas: **Canvas Scaler ▸ UI Scale Mode = Scale With Screen Size**, Reference Resolution `1920 × 1080`, Match `0.5`.
3. Confirm the Canvas has a **Graphic Raycaster** (added by default).

---

## 2 · Build the ChipView prefab

1. Right-click Canvas ▸ **Create Empty** → rename **Chip**. RectTransform **220 × 250**. Add **Canvas Group**. Add the **ChipView** script.
2. Add children (right-click Chip ▸ UI ▸ Image / Text - TextMeshPro):

   | Child | Type | Sprite / notes | Raycast Target |
   |---|---|---|---|
   | **Base** | Image | `ChipCard_Base`, stretch to fill Chip | **ON** |
   | **Glow** | Image | `ChipCard_Glow_White`, fill Chip, color white | OFF |
   | **Icon** | Image | anchor to window slot* | OFF |
   | **CostText** | TMP | badge slot*, center-aligned | OFF |
   | **NameText** | TMP | plate slot*, center-aligned | OFF |

   \* Slots as fractions of the 220×250 card (from the frame): **Icon** center (0.50, 0.47) size (0.73, 0.43) · **Cost** center (0.16, 0.14) · **Name** center (0.50, 0.77).

3. Select **Chip**, fill the ChipView fields: Base Image→Base, Art Image→Icon, Glow Image→Glow, Name/Cost→the TMPs, Group→the Canvas Group. Under **Corruption**: **Normal Frame** = `ChipCard_Base`, **Glitch Frame** = `ChipCard_Glitch`.
4. Drag **Chip** from Hierarchy into `Assets/Prefabs/` to make it a **prefab**, then delete it from the scene.

---

## 3 · Build the HUD hierarchy

Under the Canvas:

```
NeuralHUD                      [NeuralInterfaceHUD]
├── HUD_Frame                  Image = HUD_Frame   (anchor bottom, stretch width, Raycast OFF)
│   ├── HeaderLabel            TMP  "NEURAL MEMORY INTERFACE"
│   ├── RackSlot_0 … _4        empty RectTransforms, one centered over each bay
│   └── CpuMeter               [CpuCycleMeter]
│       └── Pip_0 Pip_1 Pip_2  Image = CpuPip_On  (over the three pip sockets)
├── NeuralSlot                 Image = NeuralSlot  (centered, above the bar, Raycast OFF)
│   └── InstallGroup           [Canvas Group] alpha 0
│       ├── InstallLabel       TMP  (leave blank; script fills it)
│       └── InstallBar         Image → Image Type = Filled, Fill Method Horizontal
└── VisorOverlay               Image (cyan, alpha 0)  [VisorFlash]   (over Vestige's visor, or full-screen)
```

Tips: put the **RackSlot_** anchors dead-center over each bay — chips spawn at their center. Keep **NeuralSlot** pivot at center.

---

## 4 · Wire the components

**NeuralInterfaceHUD** (on NeuralHUD):
- **Combat** → your CombatManager · **Max Cycles** → `3`
- **Rack Slots** → drag RackSlot_0…4 (in order) · **Chip Prefab** → the Chip prefab
- **Neural Slot** → NeuralSlot · **Installed Scale** → `(0.85, 0.85, 1)` (or `1,1,1`)
- **Install Group / Install Label / Install Bar** → the three nodes under InstallGroup
- **Visor Flash** → VisorOverlay · **Cpu Meter** → CpuMeter
- Timing → leave defaults (eject .12 / slide .25 / install .8)

**CpuCycleMeter** (on CpuMeter): **Pips** → Pip_0,1,2 · **On Sprite** → CpuPip_On · **Off Sprite** → CpuPip_Off

**VisorFlash** (on VisorOverlay): **Visor** → the VisorOverlay Image

---

## 5 · Hook Vestige's attack

On **NeuralInterfaceHUD ▸ On Chip Installed (CardData)** press **+**, drag in your Vestige controller, and pick the method that plays the dash+attack (e.g. `VestigeController.PlayAttack`). This fires the instant the memory seats, so the strike lines up with the flash. Card effects (`TryPlayCard`) resolve right after.

*No CombatManager yet?* Leave **Combat** empty — the animation and this event still run, so you can test the feel.

---

## 6 · Glitch (Loom filler) cards

Any `CardData` with **Is Glitch = true** automatically renders with `ChipCard_Glitch` and can't be played (it does a red shake). Nothing else to wire — `ChipView.Bind` swaps the frame. Feed these into the hand via `Deck.AddToDiscard(glitchCard)` so they recirculate.

---

## 7 · Play test

1. Make sure the CombatManager scene is set up (actors, starting deck) as you already have it.
2. Press **Play**. `CombatManager.Start()` deals a hand → `OnCombatChanged` fires → the rack fills and the CPU pips show `3`.
3. Click a chip → it ejects, slides to the Neural Slot, `INSTALLING MEMORY…` fills, the visor flashes, then the card resolves (watch the `[Echoform]` Console logs) and a pip drains.
4. Click a card you can't afford / a glitch → red shake, no play.

### If something's off
- **Nothing happens on click** → no EventSystem, or **Base**'s Raycast Target is OFF, or the chip has no Graphic under the cursor.
- **Rack is empty** → Combat not assigned, or `CombatManager` hasn't dealt yet (it deals in `StartPlayerTurn`).
- **Chip flies to a weird spot** → NeuralSlot isn't a child of the same Canvas, or its pivot isn't centered.
- **Glow won't tint** → you assigned the colored glow; use `ChipCard_Glow_White`.
- **Install bar doesn't move** → InstallBar Image Type isn't **Filled**.

---

## Optional polish
- End-turn button → call `combat.EndTurn()`.
- Additive material on Glow/Scanline for a brighter "power up".
- A `Chip_Scanline` child inside a RectMask2D over the slot for the sweeping line (see NEURAL_INTERFACE_SETUP.md).
