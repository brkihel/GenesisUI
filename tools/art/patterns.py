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


def main():
    out = os.path.join(ROOT, "art", "out")
    os.makedirs(out, exist_ok=True)
    bubbles().save(os.path.join(out, "bar_bubbles.png"))
    print(f"art/out/bar_bubbles.png ({W}x{H}, tiling)")


if __name__ == "__main__":
    main()
