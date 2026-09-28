"""Full-HUD mock at 1920x1080 with every element at its default position.

    tools/.venv/bin/python tools/art/mock.py [output.png]

Composes the rendered sprites (art/out) roughly the way the plugin lays them out, over a
busy green background, so cohesion between pieces can be judged before a build reaches
Diego (AGENTS.md "Add art"). It is an approximation: 9-slice, tint and fonts follow the
game, animation and exact TMP rendering do not. Keep positions in sync with the modules'
default config when they change. Writes the full mock plus two close-up crops.
"""
import os
import random
import sys

from PIL import Image, ImageChops, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
ART = os.path.join(ROOT, "art", "out")
FONTS = os.path.join(ROOT, "art", "fonts")
S = 2  # sprites are rendered at 2x
W, H = 1920, 1080


def sprite(name):
    return Image.open(os.path.join(ART, name + ".png")).convert("RGBA")


def font(name, size):
    return ImageFont.truetype(os.path.join(FONTS, name + ".ttf"), size)


def nine(im, w, h, l, b, r, t):
    w, h = int(w), int(h)
    sw, sh = im.size
    l, b, r, t = (int(v * S) for v in (l, b, r, t))
    out = Image.new("RGBA", (w * S, h * S))
    for sx0, sx1, dx0, dx1 in ((0, l, 0, l), (l, sw - r, l, w * S - r), (sw - r, sw, w * S - r, w * S)):
        for sy0, sy1, dy0, dy1 in ((0, t, 0, t), (t, sh - b, t, h * S - b), (sh - b, sh, h * S - b, h * S)):
            if dx1 > dx0 and dy1 > dy0:
                out.paste(im.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0)), (dx0, dy0))
    return out.resize((w, h), Image.LANCZOS)


def tint(im, rgb, alpha=1.0):
    r, g, b, a = im.split()
    ch = [ImageChops.multiply(x, Image.new("L", im.size, v)) for x, v in zip((r, g, b), rgb)]
    return Image.merge("RGBA", (*ch, a.point(lambda v: int(v * alpha))))


def main(out_path):
    rng = random.Random(1)
    bg = Image.new("RGBA", (W, H), (46, 58, 44, 255))
    d = ImageDraw.Draw(bg)
    for _ in range(900):
        x, y, c = rng.randrange(W), rng.randrange(H), rng.randrange(30, 70)
        d.ellipse((x, y, x + 40, y + 40), fill=(c, c + 18, c - 6, 255))

    def put(im, x, top):
        bg.alpha_composite(im, (int(x), int(top)))

    def text(x, y, s, f, fill=(251, 243, 218, 255), anchor="mm"):
        ImageDraw.Draw(bg).text((x, y), s, font=f, fill=fill, anchor=anchor, stroke_width=1, stroke_fill=(0, 0, 0, 200))

    fill, bubbles = sprite("bar_fill"), sprite("bar_bubbles")

    def bar(x, y, w, h, rgb, frac, value):
        top = H - (y + h)
        put(nine(sprite("bar_frame"), w, h, 10, 22, 10, 32), x, top)
        aw, ah = int(w - 19), int(h - 56)
        fh = int(ah * frac)
        liquid = tint(fill.resize((aw, ah)), rgb)
        pattern = Image.new("RGBA", (aw, ah))
        tile = bubbles.resize((aw, aw * 2))
        for yy in range(0, ah, tile.size[1]):
            pattern.paste(tile, (0, yy))
        liquid.alpha_composite(tint(pattern, tuple(int(c + (255 - c) * 0.55) for c in rgb), 0.2))
        put(liquid.crop((0, ah - fh, aw, ah)), x + 9.5, top + 32 + (ah - fh))
        text(x + w / 2, top + 32 + ah / 2, str(value), font("Cinzel-SemiBold", 19 if len(str(value)) < 3 else 15))

    # Vitals [Vitals] OffsetX 36, OffsetY 72
    vx, vy = 36, 72
    bar(vx, vy + 22, 46, 226, (0xA5, 0x1C, 0x1E), 0.66, 148)
    bar(vx + 52, vy + 22, 38, 196, (0xC0, 0x8A, 0x22), 0.8, 132)
    bar(vx + 96, vy + 22, 38, 196, (0x22, 0x81, 0xAD), 0.5, 66)
    put(sprite("medallion").resize((58, 58)), vx - 6, H - (vy + 58))

    # Food [Food] OffsetX 190, OffsetY 94
    for i, t in enumerate(("13m", "23m", "28m")):
        x = 190 + i * 66
        put(sprite("slot").resize((58, 58)), x, H - (94 + 58))
        text(x + 49, H - 94 - 8, t, font("Cinzel-SemiBold", 14), anchor="rs")

    # Hotbar [Hotbar] OffsetY 20, bottom centre
    pw = 8 * 56 + 7 * 8 + 60
    px = W / 2 - pw / 2
    put(nine(sprite("plate"), pw, 80, 24, 16, 24, 16), px, H - (20 + 80))
    for i in range(8):
        x = px + 30 + i * 64
        put(sprite("slot").resize((56, 56)), x, H - (20 + 12 + 56))
        text(x + 8, H - (20 + 12 + 56) + 11, str(i + 1), font("Cinzel-Medium", 13), anchor="lm")
    put(sprite("slot_active").resize((62, 62)), px + 30 + 2 * 64 - 3, H - (20 + 12 + 56) - 3)

    # Minimap [Minimap] OffsetX 24, OffsetY 20, top-right
    gx, ring_top = W - 24 - 250, 20 + 40
    world = Image.new("RGBA", (222, 222), (90, 120, 70, 255))
    wd = ImageDraw.Draw(world)
    for _ in range(120):
        x, y = rng.randrange(222), rng.randrange(222)
        wd.ellipse((x, y, x + 24, y + 24), fill=(70 + rng.randrange(40), 110 + rng.randrange(30), 60, 255))
    bg.paste(world, (gx + 14, ring_top + 14), sprite("map_mask").split()[3].resize((222, 222)))
    put(sprite("map_ring").resize((250, 250)), gx, ring_top)
    crest_top = ring_top - 34
    put(sprite("map_crest").resize((200, 52)), gx + 25, crest_top)
    put(sprite("wind_disk").resize((30, 30)), gx + 125 - 64 - 15, crest_top + 52 - 8 - 30)
    put(sprite("wind_arrow").resize((24, 24)).rotate(-40, resample=Image.BICUBIC), gx + 125 - 64 - 12, crest_top + 52 - 8 - 27)
    text(gx + 137, crest_top + 52 - 22, "Dia 4 · 07:26", font("Cinzel-SemiBold", 14))
    put(sprite("map_banner").resize((180, 32)), gx + 35, ring_top + 250 - 18)
    text(gx + 125, ring_top + 250 - 2, "PRADO", font("Cinzel-Medium", 14), fill=(247, 226, 131, 255))
    text(gx + 125, ring_top + 22, "N", font("Cinzel-SemiBold", 13), fill=(247, 226, 131, 255))

    # Status [Status] OffsetX 24, OffsetY 340, right to left
    for i, (name, t) in enumerate((("Eikthyr", "19:54"), ("Molhado(a)", "1:50"), ("Frio", ""))):
        x = W - 24 - 84 - i * 84
        put(sprite("tile").resize((60, 60)), x + 12, 340)
        text(x + 42, 340 + 71, name, font("CormorantGaramond-SemiBold", 15))
        text(x + 42, 340 + 89, t, font("Cinzel-Medium", 13))
    put(sprite("badge_cooldown").resize((20, 20)), W - 24 - 84 + 12 + 44, 338)

    bg.save(out_path)
    base, ext = os.path.splitext(out_path)
    bg.crop((0, 700, 900, 1080)).resize((1350, 570), Image.LANCZOS).save(base + "-bottom-left" + ext)
    bg.crop((1300, 0, 1920, 480)).save(base + "-top-right" + ext)
    print("mock written to " + out_path)


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "dist", "hud-mock.png"))
