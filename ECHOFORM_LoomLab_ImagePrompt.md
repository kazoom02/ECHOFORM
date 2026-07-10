# ECHOFORM — The Loom's Laboratory (Boss Arena) · Pixel-Art Image Prompt

For generating the Loom core chamber as a **pixel-art** background. Use the provided Loom concept image as a composition/lighting reference. Target a wide 16:9 side-on plate so it works as the Fight III arena (Vestige bottom-left, enemies right, catwalk floor across the bottom).

---

## Main prompt

> Detailed pixel-art background of a vast, decaying underground laboratory chamber built around a colossal machine called the Loom. The Loom dominates the centre-background: a huge circular reactor of concentric metal rings, and at its heart a brilliant cyan `#00FFFF` core radiating thin thread-like filaments of light in a woven starburst, wrapped in glowing haze. Surround it with dark industrial detail — riveted steel gantries, pipes and hanging cables, side control panels and monitors flickering faint cyan, tall grimy machinery and railings receding into shadow. A metal catwalk with a railing runs across the foreground as the floor. Heavy volumetric haze, strong cyan bloom from the core lighting the wet dark surfaces, near-black teal shadows. Ominous, cold, cathedral-like scale. Side-scrolling game background, horizontal composition, empty foreground floor for characters.

## Style tags (append / emphasise)
> pixel art, detailed high-resolution pixel art, crisp clean pixels, hard edges, no anti-aliasing, limited palette, dithered gradients for the glow and haze, 2D game environment art, side view, cohesive retro-modern look.

## Palette
Near-black and dark teal-green base, cyan `#00FFFF` as the single dominant light source (core, bloom, screen flickers), tiny sparse red warning-LED accents on the side monitors. Keep it monochromatic-leaning cyan so Vestige's blade reads as part of the world.

## Negative prompt
> blurry, soft focus, smooth anti-aliased rendering, 3D render, photorealistic, painterly brushstrokes, text, watermark, characters, people, bright daylight, warm orange lighting, cluttered foreground floor.

---

### Tips
- **Aspect / size:** ask for 16:9 (e.g. 1920x1080 or a 320x180 pixel grid upscaled) so it slots into your arena camera.
- **Two layers help:** generate once as described, then optionally ask for a **matching parallax back-layer** (just the Loom + haze, no gantries) so you can scroll depth in Unity.
- **Keep the core centred and the bottom third clear** — that empty catwalk floor is where combat happens.
- **Consistency:** same cyan `#00FFFF` and near-black as Vestige and the intro shots — this chamber is the destination the whole game builds toward.
