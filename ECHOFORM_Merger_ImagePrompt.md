# ECHOFORM — The Merger (Fuser Enemy) · Pixel-Art Sprite Prompt

The inverse of the slime: it never splits — two Mergers that both survive a turn FUSE into a bigger one. So the silhouette must read "made of fused copies." Design rules: **violet**, built from visibly fused lobes, with **X'd-out / dead eyes** showing the copies it has absorbed. Four tiers: Ooze → Confluence → Amalgam → Colossus (the Colossus has five dead eyes because it ate five).

**Pixel art** to match the Loom lab. Generate each tier as a **single creature, side-on view, centered, on a transparent background**, so it drops straight into the combat row. Emissive violet meant to bloom against near-black.

---

## Base prompt (Ooze — tier 1)
> Detailed pixel-art game enemy sprite of a gelatinous violet slime-creature, side view, centered, on a transparent background. It is a "fuser": a single blob that clearly looks like two smaller blobs mashed together, with a visible seam where they merged. Glossy translucent purple goo glowing violet from within, a couple of small glowing pixel eyes plus one dim X-shaped "dead" eye marking an absorbed copy. Menacing but simple, heavy and lumpy. Crisp clean pixels, no anti-aliasing, limited violet palette, dithered inner glow, dark rim light, no background.

**Always append:** `pixel art, true pixel grid, nearest-neighbor, crisp hard edges, no anti-aliasing, no blur, limited palette, 2D game sprite, transparent background`

## Tier variations (same pixel style + palette, change lobe & eye count)
- **Ooze (T1):** 1–2 fused lobes, small, 1 dim dead eye. The base unit.
- **Confluence (T2):** clearly 2 fused blobs, bigger, **2** X'd dead eyes.
- **Amalgam (T3):** 3–4 lumpy fused lobes, bulky and asymmetric, **3–4** dead eyes, a few live eyes.
- **Colossus (T4):** a towering mass of **5 fused lobes**, hulking, with **5 X'd-out dead eyes** and one large glaring live eye — "it ate five."

## Palette & mood
Deep violet / purple body (`#8A2BE2`-ish), brighter magenta-violet inner glow, near-black surroundings, cold. Keep it clearly **violet** so it contrasts your **coral** slimes and **cyan** Vestige — color tells the player which creature does what.

## Negative prompt
> background, scenery, floor, coral, orange, red, cyan, blue, cute, friendly, smiling, text, watermark, 3D render, smooth shading, anti-aliasing, blurry, photorealistic, multiple separate creatures.

---

### Tips
- **Pixel-art gotcha:** many generators fake it with anti-aliased "pixel-ish" output. If that happens, generate at a small resolution (e.g. 128×128 or 96×96) and upscale nearest-neighbor, and keep "true pixel grid, no blur" in the prompt.
- **Consistency:** generate the Ooze first, then reuse its exact wording + seed for the higher tiers, only changing lobe/eye counts and size, so the four read as one family and one resolution/pixel density.
- **Telegraph:** the fused seams and dead eyes ARE the mechanic — make them obvious, the way the coral slime wears a mitosis seam.
- **For Unity:** export PNG with transparency; set Filter Mode = Point, Compression = None (matches your existing import settings) so the pixels stay crisp.
- **Fusion VFX (optional):** also generate a "two Mergers merging" pixel frame — two violet blobs pulling into one with a bright seam — for the fuse animation.
