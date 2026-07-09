#!/usr/bin/env python3
"""
Cyberpunk katana slash VFX generator.
Pixel-art, 480x270 grid, nearest-neighbour upscaled x4 -> 1920x1080.
12 frames, transparent PNGs + horizontal sprite sheet + GIF preview.
Style: cyan energy, white-hot core, purple/magenta glitch, digital breakup.
"""
import numpy as np
from PIL import Image
import os

W, H = 480, 270
SCALE = 4
N = 12
OUT = "/sessions/vibrant-wizardly-pasteur/mnt/outputs/slash"
FRAMES_DIR = os.path.join(OUT, "frames")
os.makedirs(FRAMES_DIR, exist_ok=True)

rng = np.random.default_rng(7)

# ---- colour vectors (additive light) ----
CYAN    = np.array([40, 210, 255], float)
WHITE   = np.array([255, 255, 255], float)
MAGENTA = np.array([225, 55, 235], float)
PURPLE  = np.array([150, 70, 255], float)

# ---- per-frame timeline ----
TIP  = [0.05, 0.11, 0.34, 0.63, 0.86, 1.00, 1.00, 1.00, 1.00, 1.00, 1.00, 1.00]  # leading edge x (0..1)
INT  = [0.55, 0.80, 1.05, 1.25, 1.35, 1.85, 1.15, 0.82, 0.58, 0.40, 0.24, 0.11]  # overall intensity
BRK  = [0.00, 0.00, 0.00, 0.00, 0.02, 0.05, 0.18, 0.34, 0.52, 0.70, 0.86, 0.96]  # digital breakup amount
PART = [0.00, 0.00, 0.05, 0.12, 0.22, 0.45, 0.70, 0.85, 0.95, 0.90, 0.75, 0.55]  # particle amount
FLASH= [0.00, 0.00, 0.05, 0.15, 0.35, 1.00, 0.45, 0.20, 0.08, 0.03, 0.0, 0.0]    # white-hot centre flash

xs = np.arange(W)
ys = np.arange(H)
X, Y = np.meshgrid(xs, ys)          # (H,W)
xn = X / (W - 1)                    # normalised 0..1

def slash_y(xn_):
    """Slightly bowed, faintly tilted horizontal slash centreline (in px)."""
    cy = H * 0.53
    arc  = -20.0 * np.sin(np.pi * xn_)      # gentle upward bow
    tilt = (xn_ - 0.5) * 26.0               # subtle lower-left -> upper-right tilt
    return cy + arc + tilt

YC = slash_y(xn)                    # centreline y per pixel column
DY = Y - YC                         # vertical signed distance to centreline

def frame(i):
    acc = np.zeros((H, W, 3), float)   # additive light buffer

    tip = TIP[i] * (W - 1)
    inten = INT[i]

    # --- leading / trailing edge envelope along x ---
    lead = np.clip((tip - X) / 9.0, 0.0, 1.0)          # sharp cut just past the tip
    tail_start = 0.02 * W
    trail = np.clip((X - tail_start) / 30.0, 0.0, 1.0)  # fade in from far-left origin
    edge = lead * (0.35 + 0.65 * trail)

    # bright racing tip cap (radial), strongest before/at impact
    tip_col = TIP[i]
    tip_y = slash_y(tip_col)
    rt = np.sqrt((X - tip) ** 2 + (Y - tip_y) ** 2)
    tipcap = np.exp(-(rt ** 2) / (2 * 6.0 ** 2)) * (1.0 if i < 6 else 0.0)

    d = np.abs(DY)

    # --- energy profiles perpendicular to the blade ---
    core = np.exp(-(d ** 2) / (2 * 1.6 ** 2))     # thin white-hot core
    glow = np.exp(-(d ** 2) / (2 * 5.5 ** 2))     # cyan blade glow
    halo = np.exp(-(d ** 2) / (2 * 15.0 ** 2))    # wide soft halo
    ph   = np.exp(-(d ** 2) / (2 * 26.0 ** 2))    # purple/magenta outer bleed

    core *= edge; glow *= edge; halo *= edge; ph *= edge
    core = core + tipcap
    glow = glow + tipcap * 0.7

    # white-hot centre flash (gaussian along the blade around screen centre)
    cflash = np.exp(-((X - W * 0.5) ** 2) / (2 * (W * 0.22) ** 2))
    core = core + FLASH[i] * 1.4 * cflash * (glow > 0.02)
    # tight radial white-hot bloom at the impact point
    cy_mid = slash_y(0.5)
    rc = np.sqrt((X - W * 0.5) ** 2 + (Y - cy_mid) ** 2)
    bloom = np.exp(-(rc ** 2) / (2 * 26.0 ** 2))
    core = core + FLASH[i] * 1.6 * bloom

    # compose additive colour  (cyan-forward; white reserved for the hot core/impact)
    acc += (glow * 1.55)[..., None] * CYAN
    acc += (core * 1.15)[..., None] * WHITE
    acc += (halo * 0.55)[..., None] * CYAN
    acc += (ph   * 0.40)[..., None] * PURPLE
    # magenta accent hugging the lower side of the blade
    mside = np.exp(-((DY + 6.0) ** 2) / (2 * 7.0 ** 2)) * edge
    acc += (mside * 0.45)[..., None] * MAGENTA

    acc *= inten

    # --- glitch fragments: displaced rectangular slabs, colour-shifted ---
    gcount = int(6 + 34 * BRK[i])
    for _ in range(gcount):
        gx = int(rng.integers(int(tail_start), max(int(tip), int(tail_start) + 2)))
        gy = int(slash_y(gx / (W - 1)) + rng.integers(-24, 24))
        gw = int(rng.integers(3, 16)); gh = int(rng.integers(1, 5))
        sx = int(rng.integers(-14, 16))   # horizontal shear/displacement
        dx0 = int(np.clip(gx + sx, 0, W)); y0 = int(np.clip(gy, 0, H))
        y1 = int(np.clip(gy + gh, 0, H))
        wdt = int(min(gw, W - dx0))
        if wdt <= 0 or y1 - y0 <= 0:
            continue
        tint = MAGENTA if rng.random() < 0.5 else CYAN
        amp = (0.5 + 0.9 * BRK[i]) * inten * (0.6 + 0.4 * rng.random())
        acc[y0:y1, dx0:dx0 + wdt] += amp * tint * 0.6

    # --- digital particles: scattered pixels trailing the slash ---
    pcount = int(140 * PART[i])
    for _ in range(pcount):
        t = rng.random()
        px = int(np.clip(tip * (0.15 + 0.9 * t) - rng.exponential(22), 0, W - 1))
        py = int(np.clip(slash_y(px / (W - 1)) + rng.normal(0, 9 + 14 * BRK[i]), 0, H - 1))
        c = WHITE if rng.random() < 0.18 else CYAN
        if rng.random() < 0.12:
            c = MAGENTA
        a = (0.6 + 0.8 * rng.random()) * inten * (0.5 + 0.5 * PART[i])
        acc[py, px] += a * c
        if rng.random() < 0.3 and px + 1 < W:   # occasional 2px streak
            acc[py, px + 1] += a * 0.6 * c

    # --- pixel breakup mask: knock out blocky chunks late in the anim ---
    if BRK[i] > 0.01:
        bh, bw = 6, 6
        mask = rng.random((H // bh + 1, W // bw + 1))
        mask = np.repeat(np.repeat(mask, bh, 0), bw, 1)[:H, :W]
        keep = (mask > (BRK[i] * 0.75)).astype(float)
        keep = 0.25 + 0.75 * keep   # feather so it isn't a hard checker
        acc *= keep[..., None]

    # ---- resolve to RGBA ----
    rgb = np.clip(acc, 0, 255)
    lum = acc.max(axis=2)
    alpha = np.clip(lum / 210.0, 0, 1) ** 0.85
    alpha = np.clip(alpha * 1.25, 0, 1)
    out = np.zeros((H, W, 4), np.uint8)
    out[..., :3] = rgb.astype(np.uint8)
    out[..., 3] = (alpha * 255).astype(np.uint8)
    return out

frames = [frame(i) for i in range(N)]

# upscale x4 nearest-neighbour -> 1920x1080, save individual PNGs
imgs = []
for i, fr in enumerate(frames):
    im = Image.fromarray(fr, "RGBA").resize((W * SCALE, H * SCALE), Image.NEAREST)
    im.save(os.path.join(FRAMES_DIR, f"slash_{i:02d}.png"))
    imgs.append(im)

# --- FULL-RES GRID sheet: 4 cols x 3 rows (7680x3240) -----------------------
# A 1x12 full-res strip would be 23040px wide and exceed Unity's 16384 max
# texture size, so Unity downscales it and grid-slicing misaligns. A 4x3 grid
# stays within limits. Slice in Unity: Grid By Cell Count = 4 cols, 3 rows.
GCOLS, GROWS = 4, 3
gw, gh = W * SCALE, H * SCALE
grid = Image.new("RGBA", (GCOLS * gw, GROWS * gh), (0, 0, 0, 0))
for i, im in enumerate(imgs):
    r, c = divmod(i, GCOLS)      # row-major = Unity's slice order
    grid.paste(im, (c * gw, r * gh))
grid.save(os.path.join(OUT, "slash_spritesheet.png"))
grid_blk = Image.new("RGBA", grid.size, (0, 0, 0, 255))
grid_blk.alpha_composite(grid)
grid_blk.convert("RGB").save(os.path.join(OUT, "slash_spritesheet_black.png"))

# --- NATIVE-RES horizontal strip: 12 x (480x270) = 5760x270 -----------------
strip = Image.new("RGBA", (W * N, H), (0, 0, 0, 0))
for i, fr in enumerate(frames):
    strip.paste(Image.fromarray(fr, "RGBA"), (i * W, 0))
strip.save(os.path.join(OUT, "slash_strip_native_1x12.png"))
strip_blk = Image.new("RGBA", strip.size, (0, 0, 0, 255))
strip_blk.alpha_composite(strip)
strip_blk.convert("RGB").save(os.path.join(OUT, "slash_strip_native_1x12_black.png"))

# GIF preview (on black so glow reads) ~20fps -> 50ms, hold last frame
gif_frames = []
for im in imgs:
    bg = Image.new("RGBA", im.size, (0, 0, 0, 255))
    bg.alpha_composite(im)
    gif_frames.append(bg.convert("P", palette=Image.ADAPTIVE))
durations = [50] * N
durations[5] = 90
durations[-1] = 350
gif_frames[0].save(os.path.join(OUT, "slash_preview.gif"), save_all=True,
                   append_images=gif_frames[1:], duration=durations, loop=0, disposal=2)

print("done grid:", grid.size, "strip:", strip.size)
