# ECHOFORM — Main Menu setup

Four scripts drive the title screen. This is the one-time Inspector wiring.

**Scripts**
- `MainMenuController.cs` — the router (New Game / Load Game / Settings / Quit)
- `LoadGameMenu.cs` + `LoadGameRow.cs` — the save list ("No games saved" when empty)
- `SettingsMenu.cs` — master volume + fullscreen
- `SaveSystem.cs` (in `Scripts/Save/`) — reads/writes save files

---

## 1. Build Settings (do this first — currently only SampleScene is registered)

File ▸ Build Settings ▸ Scenes In Build. Add and order:
1. `Scenes/MainMenu` (index 0 — the game must boot here)
2. `Scenes/FirstArea` (Area 1)

If `FirstArea` isn't in this list, **New Game will fail** — that's the #1 gotcha.

---

## 2. Canvas hierarchy (in MainMenu.unity)

Build one Canvas with three sibling panels:

```
Canvas
├─ MainPanel            (active)
│   ├─ NewGameButton
│   ├─ LoadGameButton
│   ├─ SettingsButton
│   └─ QuitButton
├─ LoadPanel            (inactive)
│   ├─ NoSavesLabel     (TMP text: "No games saved")
│   ├─ ScrollView ▸ Viewport ▸ Content   (Content = the list container)
│   └─ BackButton
└─ SettingsPanel        (inactive)
    ├─ VolumeSlider     (Slider, min 0 max 1)
    ├─ FullscreenToggle (Toggle)
    └─ BackButton
```

Add a **Vertical Layout Group** + **Content Size Fitter** to the `Content` object so spawned rows stack.

---

## 3. LoadGameRow prefab

1. Create a UI ▸ Button. Give it a Horizontal Layout Group and add **three TMP text children**:
   - `NameLabel` (the save name)
   - `PlaytimeLabel` (e.g. "2h 15m")
   - `DateLabel` (creation date)
2. Add the `LoadGameRow` component and drag each TMP child into its matching field
   (`Name Label`, `Playtime Label`, `Date Label`). Optionally set `Date Format` (default `dd MMM yyyy`).
3. Drag it into `Assets/Prefabs/` to make it a prefab, delete from scene.

---

## 4. Wire the components

**On a `MainMenuController` (put it on the Canvas or an empty "MenuManager"):**
- Area1 Scene Name = `FirstArea`
- Main Panel / Load Panel / Settings Panel → the three panels
- NewGame/LoadGame/Settings/Quit Button → the four buttons
- Load Game Menu → the `LoadGameMenu` (below)

**On `LoadPanel`, add `LoadGameMenu`:**
- List Content → the ScrollView's `Content`
- Row Prefab → your `LoadGameRow` prefab
- No Saves Label → the `NoSavesLabel` object

**On `SettingsPanel`, add `SettingsMenu`:**
- Volume Slider → the slider
- Fullscreen Toggle → the toggle
- Back Button → the panel's Back button
- Menu → the `MainMenuController`

**Back buttons on Load/Settings panels:** in their OnClick, call `MainMenuController.ShowMain` (SettingsMenu also does this via its `menu` reference, so either works).

---

## 5. Test

Press Play in MainMenu.
- **New Game** → loads FirstArea.
- **Load Game** → shows "No games saved" (nothing's saved yet).
- **Settings** → volume/fullscreen work and persist across launches.
- **Quit** → exits play mode (or the built game).

---

## 6. Later — making Load Game show real saves

Call this from your run/combat code (e.g. at the start of each fight):

```csharp
SaveSystem.Save(new SaveData {
    slotName  = "Fight 2 — Vestige",
    sceneName = "FirstArea",
    fightIndex = 1,
    playerHP  = player.CurrentHP,
});
```

Then `LoadGameMenu` lists it automatically. When a save is picked, the chosen data
is available as `LoadGameMenu.PendingLoad` in the loaded scene — read it there to
restore run state. Expand `SaveData` with whatever you need (deck, seed, etc.).

## 7. Playtime tracking (PlayTimeTracker)

Drop a `PlayTimeTracker` component on any object in **FirstArea** (or a bootstrap scene).
It's a `DontDestroyOnLoad` singleton that counts total run time using unscaled time, so
combat hitstop doesn't stop the clock.

- **New Game:** starts from 0 automatically (its `countOnStart` is on).
- **Loading a save:** in FirstArea, read the pending save and resume its time:
  ```csharp
  if (LoadGameMenu.PendingLoad != null)
      PlayTimeTracker.Instance.ResumeFrom(LoadGameMenu.PendingLoad.playSeconds);
  ```
- **Saving:** pass the live value in:
  ```csharp
  playSeconds = PlayTimeTracker.Instance.TotalSeconds,
  ```
- **Pause menu:** call `PlayTimeTracker.Instance.Pause()` when it opens, `Resume()` when it closes.

The row's `PlaytimeLabel` then shows this as "2h 15m" / "8m 03s" automatically.
```
