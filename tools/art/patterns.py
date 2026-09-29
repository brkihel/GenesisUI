"""Procedural, seamlessly tiling textures for animated UI (docs/DECISIONS.md D-009).

    tools/.venv/bin/python tools/art/patterns.py

bar_bubbles.png (64x128): translucent rising bubbles, white with alpha, tinted by the bar colour in
game and scrolled upward (the earlier wisps "flow" pattern was replaced after R-030). Every wave has whole-number frequencies over the
tile and sparks wrap around its edges, so the texture repeats without a seam in both axes.
Deterministic: same code, same pixels.
"""
import math
import os
import random

import numpy as np

from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
W, H = 64, 128

# (frequency across, frequency along, amplitude, phase) - integers keep it seamless
WAVES = [(1, 2, 1.0, 0.3), (2, 3, 0.7, 1.9), (3, 1, 0.5, 4.1), (1, 5, 0.45, 2.7), (4, 6, 0.25, 0.8)]


def smoothstep(a, b, x):
    t = min(1.0, max(0.0, (x - a) / (b - a)))
    return t * t * (3 - 2 * t)


def flow():
    img = Image.new("RGBA", (W, H))
    total = sum(a for _, _, a, _ in WAVES)
    rng = random.Random(7)
    sparks = [(rng.uniform(0, W), rng.uniform(0, H), rng.uniform(1.2, 2.4)) for _ in range(9)]
    px = img.load()
    for y in range(H):
        for x in range(W):
            v = sum(a * math.sin(2 * math.pi * (kx * x / W + ky * y / H) + p) for kx, ky, a, p in WAVES)
            v = (v / total + 1) / 2                      # 0..1
            wisp = smoothstep(0.58, 0.92, v) * 0.75
            spark = 0.0
            for sx, sy, r in sparks:
                dx = min(abs(x - sx), W - abs(x - sx))    # wrap-around distance
                dy = min(abs(y - sy), H - abs(y - sy))
                spark = max(spark, max(0.0, 1 - math.hypot(dx, dy) / r))
            a = min(1.0, wisp + spark)
            px[x, y] = (255, 255, 255, int(a * 255))
    return img


def bubbles():
    """Readable translucent bubbles (Diego, R-031): a soft rim, clear centre and a small
    highlight. Positions wrap around the tile edges, so the texture repeats without a seam."""
    img = Image.new("RGBA", (W, H))
    px = img.load()
    rng = random.Random(11)
    items = [(rng.uniform(0, W), rng.uniform(0, H), rng.uniform(4.5, 8.5)) for _ in range(10)]
    for y in range(H):
        for x in range(W):
            a = 0.0
            for bx, by, r in items:
                dx = min(abs(x + 0.5 - bx), W - abs(x + 0.5 - bx))
                dy = min(abs(y + 0.5 - by), H - abs(y + 0.5 - by))
                d = math.hypot(dx, dy)
                rim = max(0.0, 1.0 - abs(d - r) / 1.25)                  # soft ring
                fill = 0.14 * max(0.0, 1.0 - d / r)                     # nearly clear body
                hx, hy = bx - r * 0.35, by - r * 0.35                      # highlight up-left
                hd = math.hypot(min(abs(x + 0.5 - hx), W - abs(x + 0.5 - hx)), min(abs(y + 0.5 - hy), H - abs(y + 0.5 - hy)))
                spark = max(0.0, 1.0 - hd / max(0.8, r * 0.22))
                a = max(a, min(1.0, rim * 0.9 + fill + spark * 0.6))
            px[x, y] = (255, 255, 255, int(a * 255))
    return img


def _tile_noise(w, h, waves, seed):
    """Seamless noise: sum of sines with whole-number frequencies over the tile, domain-warped."""
    import numpy as np
    rng = np.random.default_rng(seed)
    y, x = np.mgrid[0:h, 0:w].astype(np.float32)
    u, v = x / w, y / h
    warp = 0.08 * np.sin(2 * math.pi * (2 * u + 1 * v) + rng.random() * 6.28)
    out = np.zeros((h, w), np.float32)
    for kx, ky, amp in waves:
        out += amp * np.sin(2 * math.pi * (kx * (u + warp) + ky * (v + warp)) + rng.random() * 6.28)
    return (out - out.min()) / max(1e-6, out.max() - out.min())


def _save(arr_alpha, path, rgb=(255, 255, 255)):
    import numpy as np
    a = np.clip(arr_alpha, 0, 1)
    img = np.zeros(a.shape + (4,), np.uint8)
    img[..., 0], img[..., 1], img[..., 2] = rgb
    img[..., 3] = (a * 255).astype(np.uint8)
    Image.fromarray(img, "RGBA").save(path)


def _value_noise(w, h, cells_x, cells_y, rng):
    """Seamless value noise: random lattice that wraps, smooth (cubic) interpolation."""
    lattice = rng.random((cells_y, cells_x)).astype(np.float32)
    y, x = np.mgrid[0:h, 0:w].astype(np.float32)
    gx, gy = x / w * cells_x, y / h * cells_y
    x0, y0 = np.floor(gx).astype(int), np.floor(gy).astype(int)
    fx, fy = gx - x0, gy - y0
    sx, sy = fx * fx * (3 - 2 * fx), fy * fy * (3 - 2 * fy)
    x1, y1 = (x0 + 1) % cells_x, (y0 + 1) % cells_y
    x0, y0 = x0 % cells_x, y0 % cells_y
    top = lattice[y0, x0] * (1 - sx) + lattice[y0, x1] * sx
    bottom = lattice[y1, x0] * (1 - sx) + lattice[y1, x1] * sx
    return top * (1 - sy) + bottom * sy


def _fbm(w, h, base_x, base_y, octaves, seed):
    rng = np.random.default_rng(seed)
    out = np.zeros((h, w), np.float32)
    amp, total = 1.0, 0.0
    for o in range(octaves):
        out += amp * _value_noise(w, h, base_x * 2 ** o, base_y * 2 ** o, rng)
        total += amp
        amp *= 0.5
    return out / total


def veins(path):
    """A few small, broken crack fragments (R-040: long cracks read as one continuous line):
    short jagged strokes scattered over the tile, wrapped so it repeats without a seam. The
    plugin tints them a darker shade of the liquid, so they sit inside it."""
    from PIL import ImageDraw, ImageFilter
    w, h, up = 64, 128, 4
    rng = random.Random(8)
    big = Image.new("L", (w * up, h * up), 0)
    d = ImageDraw.Draw(big)
    for _ in range(5):
        x, y = rng.uniform(0, w * up), rng.uniform(0, h * up)
        pts = [(x, y)]
        for _ in range(rng.randrange(2, 4)):
            x += rng.uniform(-10, 10)
            y += rng.uniform(12, 22)
            pts.append((x, y))
        for dx in (-w * up, 0, w * up):
            for dy in (-h * up, 0, h * up):
                d.line([(px + dx, py + dy) for px, py in pts], fill=220, width=4, joint="curve")
    small = big.filter(ImageFilter.GaussianBlur(1.4)).resize((w, h), Image.LANCZOS)
    _save(np.asarray(small, np.float32) / 255, path)


def mottle(path):
    """Slow, large, soft clouds of darker blood (drawn in black): depth, not texture."""
    n = _fbm(64, 128, 1, 2, 3, seed=33)
    _save(np.clip((n - 0.38) / 0.4, 0, 1) * 0.8, path, rgb=(0, 0, 0))


def burn(path):
    """The part being consumed: a hot band at the bottom (the surface that just moved) that
    breaks into grains and dissolves upward (bar-being-consumed reference). Repeats across,
    stretched along the consumed height."""
    w, h = 64, 64
    rng = np.random.default_rng(44)
    n = _fbm(w, h, 4, 4, 4, seed=45)
    grain = rng.random((h, w)).astype(np.float32)
    # Image row 0 is the top. The dense, hot rows are at the BOTTOM of the image: in game that
    # edge touches the liquid's surface, and the grains thin out upward into the emptied part.
    t = np.mgrid[0:h, 0:w][0].astype(np.float32) / (h - 1)
    density = np.power(t, 1.4)
    keep = (grain * 0.7 + n * 0.6) < density * 1.15                    # grains thin out upward
    alpha = np.where(keep, 0.55 + 0.45 * density, 0.0) + 0.35 * np.power(t, 6)
    _save(np.clip(alpha, 0, 1), path)


def burn_horizontal(path):
    """A horizontal stamina burn: a bright vertical leading edge with sparks trailing right.

    The left edge touches the liquid. Keeping its axis explicit avoids rotating the
    vertical resource-bar pattern into a horizontal channel.
    """
    w, h = 64, 32
    y, x = np.mgrid[0:h, 0:w].astype(np.float32)
    alpha = np.exp(-((x - 3.0) / 1.7) ** 2) * (0.72 + 0.28 * np.sin(y * 0.55) ** 2)
    alpha += 0.34 * np.exp(-((x - 8.0) / 5.0) ** 2)
    rng = np.random.default_rng(140)
    for _ in range(24):
        sx = rng.uniform(9, 61)
        sy = rng.uniform(2, h - 2)
        radius = rng.uniform(0.45, 1.4)
        strength = rng.uniform(0.45, 1.0) * (1 - sx / 80)
        alpha += strength * np.exp(-((x - sx) ** 2 + (y - sy) ** 2) / (2 * radius ** 2))
    rgba = np.zeros((h, w, 4), np.uint8)
    rgba[..., 0] = 255
    rgba[..., 1] = np.clip(195 + 60 * np.exp(-((x - 3) / 5) ** 2), 0, 255).astype(np.uint8)
    rgba[..., 2] = np.clip(95 + 130 * np.exp(-((x - 3) / 3) ** 2), 0, 255).astype(np.uint8)
    rgba[..., 3] = (np.clip(alpha, 0, 1) * 255).astype(np.uint8)
    Image.fromarray(rgba, "RGBA").save(path)


def ember(path):
    """A soft glowing dot for sparks and embers."""
    y, x = np.mgrid[0:16, 0:16].astype(np.float32)
    d = np.hypot(x - 7.5, y - 7.5) / 7.5
    _save(np.clip(1 - d, 0, 1) ** 1.8, path)


# name -> (pixel width, pixel height, wrap, 9-slice border in design units or None, maker)
PATTERNS = {
    "bar_bubbles": (64, 128, "repeat", None, None),
    "bar_veins": (64, 128, "repeat", None, veins),
    "bar_mottle": (64, 128, "repeat", None, mottle),
    "bar_burn": (64, 64, "repeat", None, burn),   # repeats across; stretched along the consumed part
    "bar_burn_h": (64, 32, "repeat", None, burn_horizontal),
    "ember": (16, 16, "clamp", None, ember),
}


def main(out_dir=None):
    out = out_dir or os.path.join(ROOT, "art", "out")
    os.makedirs(out, exist_ok=True)
    bubbles().save(os.path.join(out, "bar_bubbles.png"))
    for name, (_, _, _, _, maker) in PATTERNS.items():
        if maker is not None:
            maker(os.path.join(out, name + ".png"))
    print(f"{len(PATTERNS)} patterns written to {os.path.relpath(out, ROOT)}")


if __name__ == "__main__":
    main()
