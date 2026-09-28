"""Full-HUD mock at 1920x1080, every element at its default position, in one art style.

    tools/.venv/bin/python tools/art/mock.py [style] [output.png]      (style: carved | gold)

Composes the rendered sprites of art/out/<style> the way the plugin lays them out, reading
9-slice borders and content insets from the style's own sprites.json, so cohesion between
pieces can be judged before a build reaches Diego (AGENTS.md "Add art"). Approximate: no
animation, no exact TMP rendering. The backdrop is a concept image from Diego's local folder
(its UI-free middle; never committed) when present, else a busy green field. Keep positions in sync with the
modules' default config. Writes the full mock plus close-up crops next to it.
"""
import json
import os
import random
import sys

from PIL import Image, ImageChops, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
FONTS = os.path.join(ROOT, "art", "fonts")
BACKDROP = os.path.expanduser("~/GenesisUI-Concept/ConceptArt (3).png")
S = 2
W, H = 1920, 1080


class Style:
    def __init__(self, name):
        self.dir = os.path.join(ROOT, "art", "out", name)
        with open(os.path.join(self.dir, "sprites.json"), encoding="utf-8") as f:
            self.meta = {s["name"]: s for s in json.load(f)["sprites"]}

    def has(self, name):
        return name in self.meta

    def img(self, name):
        return Image.open(os.path.join(self.dir, name + ".png")).convert("RGBA")

    def border(self, name):
        m = self.meta[name]
        return m["borderLeft"], m["borderBottom"], m["borderRight"], m["borderTop"]

    def content(self, name, fallback):
        m = self.meta.get(name, {})
        if m.get("contentLeft", -1) >= 0:
            return m["contentLeft"], m["contentBottom"], m["contentRight"], m["contentTop"]
        return fallback

    def nine(self, name, w, h):
        im = self.img(name)
        l, b, r, t = (int(v * S) for v in self.border(name))
        w, h = int(w), int(h)
        sw, sh = im.size
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


def tiled(im, w, h, tile_w):
    t = im.resize((tile_w, tile_w * im.size[1] // im.size[0]))
    out = Image.new("RGBA", (w, h))
    for y in range(0, h, t.size[1]):
        for x in range(0, w, t.size[0]):
            out.paste(t, (x, y))
    return out


def font(name, size):
    return ImageFont.truetype(os.path.join(FONTS, name + ".ttf"), size)


def main(style_name, out_path):
    st = Style(style_name)
    rng = random.Random(1)
    if os.path.exists(BACKDROP):
        # The concept carries its own HUD; use its UI-free middle so only our pieces show.
        src = Image.open(BACKDROP).convert("RGBA")
        sw, sh = src.size
        box = (int(sw * 0.3), int(sh * 0.12), int(sw * 0.7), int(sh * 0.12 + sw * 0.4 * H / W))
        bg = src.crop(box).resize((W, H), Image.LANCZOS)
    else:
        bg = Image.new("RGBA", (W, H), (46, 58, 44, 255))
        d = ImageDraw.Draw(bg)
        for _ in range(900):
            x, y, c = rng.randrange(W), rng.randrange(H), rng.randrange(30, 70)
            d.ellipse((x, y, x + 40, y + 40), fill=(c, c + 18, c - 6, 255))

    def put(im, x, top):
        bg.alpha_composite(im, (int(x), int(top)))

    def text(x, y, s, f, fill=(251, 243, 218, 255), anchor="mm"):
        ImageDraw.Draw(bg).text((x, y), s, font=f, fill=fill, anchor=anchor, stroke_width=1, stroke_fill=(0, 0, 0, 200))

    # Vital bars [Vitals] OffsetX 36, OffsetY 72 (UI y grows upward).
    def bar(x, y, w, h, rgb, frac, trail, value, hot):
        top = H - (y + h)
        put(st.nine("bar_frame", w, h), x, top)
        cl, cb, cr, ct = st.content("bar_frame", (9.5, 24, 9.5, 32))
        aw, ah = int(w - cl - cr), int(h - cb - ct)
        fh, th = int(ah * frac), int(ah * trail)
        light = tuple(int(c + (255 - c) * 0.55) for c in rgb)
        liquid = tint(st.img("bar_fill").resize((aw, ah)), rgb)
        for name, color, alpha in (("bar_mottle", (0, 0, 0), 0.45), ("bar_veins", light, 0.55), ("bar_bubbles", light, 0.1)):
            if st.has(name):
                liquid.alpha_composite(tint(tiled(st.img(name), aw, ah, aw), color, alpha))
        put(liquid.crop((0, ah - fh, aw, ah)), x + cl, top + ct + ah - fh)
        if th > fh and st.has("bar_burn"):
            put(tint(st.img("bar_burn").resize((aw, th - fh)), hot, 0.95), x + cl, top + ct + ah - th)
        put(Image.new("RGBA", (aw, 2), light + (220,)), x + cl, top + ct + ah - fh)
        if st.has("bar_value"):
            py = top + ct + ah - int(ah * 0.28) - 13
            put(st.img("bar_value").resize((w + 10, 26), Image.LANCZOS), x - 5, py)
            text(x + w / 2, py + 12, str(value), font("Cinzel-SemiBold", 16))
        else:
            text(x + w / 2, top + ct + ah / 2, str(value), font("Cinzel-SemiBold", 17))

    vx, vy = 36, 72
    bar(vx, vy + 22, 46, 226, (0xB8, 0x1E, 0x20), 0.66, 0.78, 148, (255, 158, 66))
    bar(vx + 52, vy + 22, 38, 196, (0xC8, 0x92, 0x24), 0.8, 0.8, 132, (255, 236, 150))
    bar(vx + 96, vy + 22, 38, 196, (0x24, 0x8A, 0xB8), 0.5, 0.5, 66, (175, 238, 255))
    if st.has("medallion"):
        put(st.img("medallion").resize((58, 58), Image.LANCZOS), vx - 6, H - (vy + 58))

    # Food [Food] OffsetX 190, OffsetY 94.
    for i, t in enumerate(("13m", "23m", "28m")):
        x = 190 + i * 66
        put(st.img("slot").resize((58, 58), Image.LANCZOS), x, H - (94 + 58))
        text(x + 49, H - 94 - 8, t, font("Cinzel-SemiBold", 14), anchor="rs")

    # Hotbar [Hotbar] OffsetY 20, bottom centre.
    pw = 8 * 56 + 7 * 8 + 60
    px = W / 2 - pw / 2
    put(st.nine("plate", pw, 80), px, H - (20 + 80))
    for i in range(8):
        x = px + 30 + i * 64
        put(st.img("slot").resize((56, 56), Image.LANCZOS), x, H - (20 + 12 + 56))
        text(x + 8, H - (20 + 12 + 56) + 11, str(i + 1), font("Cinzel-Medium", 13), anchor="lm")
    put(st.img("slot_active").resize((62, 62), Image.LANCZOS), px + 30 + 2 * 64 - 3, H - (20 + 12 + 56) - 3)

    # Stamina readout [Sprint] OffsetY 120, 220 x 22.
    sw, sh = 220, 22
    sx, stop = W / 2 - sw / 2, H - (120 + sh)
    put(st.nine("sprint_frame", sw, sh), sx, stop)
    cl, cb, cr, ct = st.content("sprint_frame", (24, 12, 24, 12))
    if cb + ct >= sh:
        cb, ct = cb * sh / 36, ct * sh / 36
    tw, thh = int(sw - cl - cr), int(sh - cb - ct)
    put(tint(st.img("bar_fill").resize((tw, thh)), (0xC8, 0x92, 0x24)).crop((0, 0, int(tw * 0.62), thh)), sx + cl, stop + ct)

    # Minimap [Minimap] OffsetX 24, OffsetY 20, top-right.
    gx, ring_top = W - 24 - 250, 20 + 40
    world = Image.new("RGBA", (222, 222), (90, 120, 70, 255))
    wd = ImageDraw.Draw(world)
    for _ in range(120):
        x, y = rng.randrange(222), rng.randrange(222)
        wd.ellipse((x, y, x + 24, y + 24), fill=(70 + rng.randrange(40), 110 + rng.randrange(30), 60, 255))
    bg.paste(world, (gx + 14, ring_top + 14), st.img("map_mask").split()[3].resize((222, 222)))
    put(st.img("map_ring").resize((250, 250), Image.LANCZOS), gx, ring_top)
    crest_top = ring_top - 34
    put(st.img("map_crest").resize((200, 52), Image.LANCZOS), gx + 25, crest_top)
    put(st.img("wind_disk").resize((30, 30), Image.LANCZOS), gx + 125 - 64 - 15, crest_top + 52 - 8 - 30)
    put(st.img("wind_arrow").resize((24, 24), Image.LANCZOS).rotate(-40, resample=Image.BICUBIC), gx + 125 - 64 - 12, crest_top + 52 - 8 - 27)
    text(gx + 137, crest_top + 52 - 22, "Dia 4 · 07:26", font("Cinzel-SemiBold", 14))
    put(st.img("map_banner").resize((180, 32), Image.LANCZOS), gx + 35, ring_top + 250 - 18)
    text(gx + 125, ring_top + 250 - 2, "PRADO", font("Cinzel-Medium", 14), fill=(247, 226, 131, 255))
    text(gx + 125, ring_top + 22, "N", font("Cinzel-SemiBold", 13), fill=(247, 226, 131, 255))

    # Status [Status] OffsetX 24, OffsetY 340, right to left.
    for i, (name, t) in enumerate((("Eikthyr", "19:54"), ("Molhado(a)", "1:50"), ("Frio", ""))):
        x = W - 24 - 84 - i * 84
        put(st.img("tile").resize((60, 60), Image.LANCZOS), x + 12, 340)
        text(x + 42, 340 + 71, name, font("CormorantGaramond-SemiBold", 15))
        text(x + 42, 340 + 89, t, font("Cinzel-Medium", 13))
    put(st.img("badge_cooldown").resize((20, 20), Image.LANCZOS), W - 24 - 84 + 12 + 44, 338)

    # Boss plate [Boss] OffsetY 18, top centre, 520 x 74.
    if st.has("boss_plate"):
        bw, bh = 520, 74
        bx, btop = W / 2 - bw / 2, 18
        put(st.nine("boss_plate", bw, bh), bx, btop)
        cl, cb, cr, ct = st.content("boss_plate", (22, 12, 22, 12))
        ch = bh - cb - ct
        text(W / 2, btop + ct + ch * 0.25, "Troll das Montanhas", font("Cinzel-SemiBold", 20))
        barx, bary, barw, barh = bx + cl + 6, btop + ct + ch * 0.55 + 2, bw - cl - cr - 12, ch * 0.45 - 4
        put(Image.new("RGBA", (int(barw), int(barh)), (0, 0, 0, 140)), barx, bary)
        put(tint(st.img("bar_fill").resize((int(barw * 0.76), int(barh))), (0xB8, 0x1E, 0x20)), barx, bary)
        text(W / 2, bary + barh / 2, "1370 / 1800", font("Cinzel-Medium", 12))
        for i in range(2):
            put(st.img("star").resize((13, 13), Image.LANCZOS), W / 2 - 7.5 + (i - 0.5) * 15 - 6, btop - 6)

    # Hover card [Hover] OffsetX 60, OffsetY 30 from the centre.
    if st.has("card"):
        cw, chh = 190, 70
        hx, htop = W / 2 + 60, H / 2 - 30 - chh / 2
        put(st.nine("card", cw, chh), hx, htop)
        text(hx + 26, htop + 24, "Baú de Madeira", font("Cinzel-Medium", 16), fill=(247, 226, 131, 255), anchor="lm")
        text(hx + 26, htop + 47, "[E] Abrir", font("CormorantGaramond-SemiBold", 17), anchor="lm")

    # Notice [Notice] OffsetX 24, OffsetY 24, top-left.
    if st.has("card"):
        put(st.nine("card", 230, 46), 24, 24)
        text(24 + 58, 24 + 23, "Madeira  x5", font("CormorantGaramond-SemiBold", 18), anchor="lm")

    bg.save(out_path)
    base, ext = os.path.splitext(out_path)
    bg.crop((0, 640, 900, 1080)).resize((1350, 660), Image.LANCZOS).save(base + "-bottom-left" + ext)
    bg.crop((1250, 0, 1920, 480)).save(base + "-top-right" + ext)
    bg.crop((560, 0, 1360, 620)).save(base + "-centre" + ext)
    print("mock written to " + out_path)


if __name__ == "__main__":
    style = sys.argv[1] if len(sys.argv) > 1 else "carved"
    main(style, sys.argv[2] if len(sys.argv) > 2 else os.path.join(ROOT, "dist", f"hud-mock-{style}.png"))
