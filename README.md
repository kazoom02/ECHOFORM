# ECHOFORM

A 2D turn-based combat game built in **Unity**, centered around tactical chip-based combat and enemies that become more dangerous when destroyed or allowed to evolve.

In ECHOFORM, every attack matters. Slimes split into smaller enemies when defeated, excess damage carries through into their spawned forms, and other enemies can merge together to become increasingly powerful. The player must manage a limited hand of combat chips, CPU, defensive resources, and corruption while choosing how and when to strike.

---

## 🎮 Gameplay

ECHOFORM uses a turn-based combat system built around a deck of **chips**.

During the player's turn, chips can be used to attack enemies, defend, recover, gain resources, draw additional chips, or modify future attacks.

After the player's turn ends, surviving enemies perform their own actions.

The combat system includes mechanics such as:

* **CPU management** — chips require CPU to play.
* **Deck and hand system** — draw and manage a limited set of chips each turn.
* **Target selection** — attacks can target individual enemies or entire groups.
* **Focus** — increases the strength of future attacks.
* **Block and Shields** — different defensive options for surviving enemy attacks.
* **Echo effects** — repeat the effect of previously played chips.
* **Corrupted chips** — the Loom can inject unusable Glitch chips into the player's hand.
* **Charged attacks** — some abilities require enough successful slashes before becoming available.

---

## ⚔️ Enemy Mechanics

### Slimes

Slimes are built around one of ECHOFORM's main combat mechanics:

> **Killing one can create more enemies.**

They exist in three sizes:

```text
Large Slime
    ↓
2 × Medium Slimes
    ↓
4 × Small Slimes
    ↓
Destroyed
```

Simply dealing enough damage to kill a large enemy is therefore not always the best solution.

### Overkill Carry-Through

Damage beyond the amount required to kill a Slime is transferred into the Slimes created by its split.

With enough excess damage, a spawned Slime can be destroyed before it even appears.

This makes carefully setting up powerful attacks an important part of combat.

### Mergers

Mergers work in the opposite direction.

Surviving Mergers can combine into increasingly dangerous forms:

```text
2 × Tier 1
    ↓
Tier 2

2 × Tier 2
    ↓
Tier 3
```

Allowing too many Mergers to survive can therefore create significantly stronger enemies on later turns.

---

## 🃏 Chip System

Combat abilities are implemented as data-driven Unity `ScriptableObject` cards.

A chip can contain one or more effects, including:

| Effect | Description                                      |
| ------ | ------------------------------------------------ |
| Damage | Deal damage to an enemy or group of enemies      |
| Block  | Absorb incoming damage                           |
| Heal   | Restore HP                                       |
| Focus  | Increase the power of future attacks             |
| Draw   | Draw additional chips                            |
| CPU    | Restore CPU during the current turn              |
| Echo   | Repeat the effects of the previously played chip |
| Shield | Negate an entire enemy attack                    |

Chips can target:

* A single enemy
* All enemies
* The player

Some chips can also exhaust after use, require a certain number of slashes to unlock, or require a separate charging action before their effect is released.

---

## 🌀 Corruption

The Loom can interfere with combat by corrupting the player's hand.

Corrupted **Glitch** chips occupy valuable hand space but cannot be played normally, gradually restricting the player's available options.

Managing the deck while dealing with this corruption is another part of the combat system.

---

## 🗺️ Scenes

The current project contains the following main scenes:

```text
MainMenu
Intro
FirstArea
Credits
```

The game progresses from the main menu through the introduction and into the main gameplay area before reaching the ending/credits sequence.

---

## 🛠️ Built With

* **Unity 6000.5.2f1**
* **C#**
* **Universal Render Pipeline (URP) 17.6.0**
* **Unity Input System 1.19.0**
* Unity 2D Animation
* Unity 2D Sprite
* Unity 2D Tilemap
* Unity 2D Tilemap Extras
* Unity 2D Aseprite Importer
* Unity PSD Importer
* Unity Timeline
* Unity UI

---

## 📁 Project Structure

```text
ECHOFORM/
│
├── Assets/
│   ├── Animations/
│   ├── Art/
│   ├── Audio/
│   ├── Prefabs/
│   ├── Scenes/
│   ├── Scripts/
│   │   ├── Audio/
│   │   ├── Cards/
│   │   ├── Combat/
│   │   ├── Cutscene/
│   │   ├── Enemies/
│   │   ├── Environment/
│   │   ├── Save/
│   │   └── ...
│   └── InputSystem_Actions.inputactions
│
├── Packages/
│   ├── manifest.json
│   └── packages-lock.json
│
├── ProjectSettings/
│
├── .gitattributes
├── .gitignore
└── README.md
```

Unity-generated folders such as `Library`, `Temp`, `Logs`, builds, and user-specific editor settings should not be committed to the repository.

---

## 🚀 Opening the Project

### Requirements

Install:

* [Unity Hub](https://unity.com/download)
* **Unity 6000.5.2f1**

Using the same Unity version is recommended to avoid unnecessary project or package upgrades.

### Clone the Repository

```bash
git clone https://github.com/kazoom02/ECHOFORM.git
```

Then:

1. Open **Unity Hub**.
2. Select **Add → Add project from disk**.
3. Select the cloned `ECHOFORM` folder.
4. Make sure Unity **6000.5.2f1** is installed.
5. Open the project.
6. Allow Unity to import and regenerate the `Library` folder.
7. Open:

```text
Assets/Scenes/MainMenu.unity
```

8. Press **Play**.

The first import may take some time because Unity must regenerate local project data that is intentionally excluded from Git.

---

## 🏗️ Building the Game

To create a build:

1. Open the project in Unity.
2. Go to **File → Build Profiles**.
3. Select the desired target platform.
4. Make sure the required scenes are enabled.
5. Select **Build**.
6. Choose an output folder outside the tracked source folders.

The configured scene order is:

```text
0 — MainMenu
1 — Intro
2 — FirstArea
3 — Credits
```

---

## 🔧 Core Systems

The project is divided into several gameplay systems, including:

### Combat

Handles the turn loop, CPU, card resolution, targeting, enemy turns, victory/defeat conditions, corruption and encounter progression.

### Cards

Defines combat chips using `ScriptableObject` data, allowing card effects and properties to be configured directly through the Unity Inspector.

### Enemies

Contains the common enemy framework and specialized enemy behavior including:

* Slimes
* Mergers
* Player Clone encounters

### Save System

Stores player progress and checkpoint information between areas.

### Audio

Manages music and sound effects used throughout gameplay and combat.

### Cutscenes

Controls narrative and scene transition sequences.

---

## 🎯 Design Focus

ECHOFORM's combat is designed around a simple problem:

**Destroying an enemy does not necessarily make the fight easier.**

Slimes multiply when killed, Mergers become stronger when left alive, corruption reduces the player's available options, and powerful attacks reward careful setup.

The result is a combat system focused on planning damage rather than simply dealing as much damage as possible every turn.

---

## 📦 Repository

This repository contains the **Unity source project** for ECHOFORM.

Generated Unity files and compiled builds are intentionally excluded so that the repository contains only the files necessary to recreate and continue development of the project.

```text
Assets/
Packages/
ProjectSettings/
```

---

## 👤 Repository

Maintained at:

**[github.com/kazoom02/ECHOFORM](https://github.com/kazoom02/ECHOFORM)**

---

*ECHOFORM — built with Unity and C#.*
