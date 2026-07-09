# Cyberpunk Katana Slash VFX

Full-screen horizontal energy slash. 12 frames, 1920×1080, transparent PNG. ~0.6s at 20 fps.

## ⚠️ Why the old sheet wouldn't slice
A 1×12 strip at full res is **23040 px wide**, which exceeds Unity's **max texture size (16384)**.
Unity silently downscales the whole texture on import, so a 12-cell grid no longer lands on the
frame boundaries and slicing looks broken. The sheets below stay within the limit.

## Files
- `frames/slash_00.png` … `slash_11.png` — individual transparent frames (1920×1080). Most reliable.
- `slash_spritesheet.png` — **4×3 grid, 7680×3240**, transparent. Within Unity's texture limit.
- `slash_spritesheet_black.png` — same grid on pure black (additive / chroma-key).
- `slash_strip_native_1x12.png` — **native-res 1×12 strip, 5760×270**, transparent. Tiny, crisp pixel art.
- `slash_strip_native_1x12_black.png` — native strip on black.
- `slash_preview.gif` — motion preview only (not for import).

## Recommended: individual frames
Select `slash_00…11` → Texture Type **Sprite (2D and UI)**, Filter Mode **Point (no filter)**,
Compression **None**. Drag all 12 into the scene together → Unity auto-creates the Animation Clip.
Set the clip's Samples to **20** for ~0.6s. This never has a texture-size issue.

## 4×3 grid sheet (`slash_spritesheet.png`)
1. Import. Texture Type **Sprite (2D and UI)**, Sprite Mode **Multiple**, Filter **Point**, Compression **None**.
2. Set **Max Size = 8192** (default 2048 would downscale this 7680-wide sheet).
3. Sprite Editor → **Slice → Grid By Cell Count 