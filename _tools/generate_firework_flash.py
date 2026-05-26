"""Generate a soft generic particle texture for firework effects.

This material (eff_par_1_add) is shared by flash plane, ring, tail, and one-shot
emission — so the texture must be a clean general-purpose soft glow, not a
flash-specific star burst.

Output: 1024x1024 RGB TGA with:
- Bright warm-white core (tight gaussian)
- Soft wide halo extending out
- Pure black at corners (Additive: black = invisible, masks the quad shape)
"""

from PIL import Image
import math
import os

SIZE = 1024
OUT_PATH = os.path.join(
    os.path.dirname(__file__),
    "..", "Assets", "_Project", "Sandbox", "Wenao", "firework",
    "KTK_FireWorks_Effects_Volume1", "Textures", "par_1.tga"
)

CORE_COLOR = (255, 250, 232)   # slightly warm white
INTENSITY = 0.70               # global brightness multiplier (was effectively 1.0)
FALLOFF_START = 0.30           # smoothstep start — gradient begins fading here
FALLOFF_END = 0.65             # smoothstep end — pure black at this radius and beyond
HARD_ZERO_THRESHOLD = 2        # 0..255 — any final channel value ≤ this is forced to 0


def smoothstep(edge0: float, edge1: float, x: float) -> float:
    t = max(0.0, min(1.0, (x - edge0) / (edge1 - edge0)))
    return t * t * (3.0 - 2.0 * t)


def main() -> None:
    img = Image.new("RGB", (SIZE, SIZE), (0, 0, 0))
    pixels = img.load()

    cx = cy = (SIZE - 1) / 2.0
    max_r = SIZE * 0.5

    for y in range(SIZE):
        dy = y - cy
        for x in range(SIZE):
            dx = x - cx
            r = math.sqrt(dx * dx + dy * dy) / max_r  # 0..1+

            if r >= FALLOFF_END:
                continue  # huge pure-black margin → no quad shape, no compression artifacts

            # Tight bright core
            core = math.exp(-(r * 6.0) ** 2)

            # Wider soft halo
            halo = math.exp(-(r * 3.0) ** 2) * 0.45

            # Outer mask — gradient REACHES zero well before texture edge
            outer_mask = 1.0 - smoothstep(FALLOFF_START, FALLOFF_END, r)

            intensity = (core + halo) * outer_mask * INTENSITY
            intensity = min(1.0, intensity)

            r_ch = int(CORE_COLOR[0] * intensity)
            g_ch = int(CORE_COLOR[1] * intensity)
            b_ch = int(CORE_COLOR[2] * intensity)

            # Force near-zero values to exactly zero so DXT compression doesn't
            # introduce visible noise in the dark areas
            if max(r_ch, g_ch, b_ch) <= HARD_ZERO_THRESHOLD:
                pixels[x, y] = (0, 0, 0)
            else:
                pixels[x, y] = (r_ch, g_ch, b_ch)

    img.save(OUT_PATH, format="TGA")
    print(f"Wrote {OUT_PATH} ({img.size[0]}x{img.size[1]} {img.mode})")


if __name__ == "__main__":
    main()
