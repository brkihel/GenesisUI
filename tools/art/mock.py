"""Full-HUD mock at 1920x1080, every element at its default position.

    tools/.venv/bin/python tools/art/mock.py [output.png]

Composes the rendered sprites of art/out the way the plugin lays them out, reading 9-slice
borders and content insets from art/out/sprites.json, so cohesion between
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
    def __init__(self):
        self.dir = os.path.join(ROOT, "art", "out")
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

    def nine(self, name, w, h, k=1.0):
        """9-slice at w x h; k scales the borders (the plugin's pixelsPerUnitMultiplier)."""
        im = self.img(name)
        sl, sb, sr, st_ = (int(v * S) for v in self.border(name))
        l, b, r, t = (int(round(v * S * k)) for v in self.border(name))
        w, h = int(w), int(h)
        sw, sh = im.size
        out = Image.new("RGBA", (w * S, h * S))
        for sx0, sx1, dx0, dx1 in ((0, sl, 0, l), (sl, sw - sr, l, w * S - r), (sw - sr, sw, w * S - r, w * S)):
            for sy0, sy1, dy0, dy1 in ((0, st_, 0, t), (st_, sh - sb, t, h * S - b), (sh - sb, sh, h * S - b, h * S)):
                if dx1 > dx0 and dy1 > dy0:
                    out.paste(im.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0), Image.LANCZOS), (dx0, dy0))
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


def main(out_path):
    st = Style()
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

    def size(name):
        return st.meta[name]["width"], st.meta[name]["height"]

    def dressed(name, w, h, fit=None, alpha=0.85):
        """Frame over the panel material clipped to <name>_shape (Widgets/Frame.Dress)."""
        k = fit / size(name)[1] if fit else 1.0
        w, h = int(round(w)), int(round(h))
        out = Image.new("RGBA", (w, h))
        if st.has(name + "_shape") and st.has("panel_bg"):
            mat = tiled(st.img("panel_bg"), w, h, size("panel_bg")[0])
            mask = st.nine(name + "_shape", w, h, k).split()[3].point(lambda v: int(v * alpha))
            out.paste(mat, (0, 0), mask)
        out.alpha_composite(st.nine(name, w, h, k))
        return out

    def ornament(frame_x, frame_top, w, h, name, edge, k=1.0):
        ow, oh = (v * k for v in size(name))
        inset = st.meta[name].get("inset", 0) * k
        cx, cy = {"left": (inset, h / 2), "right": (w - inset, h / 2), "top": (w / 2, inset), "bottom": (w / 2, h - inset)}[edge]
        put(st.img(name).resize((max(1, int(ow)), max(1, int(oh))), Image.LANCZOS), frame_x + cx - ow / 2, frame_top + cy - oh / 2)

    def liquid_v(name, aw, ah, density=1.4, offset=0.0):
        """A vertical liquid layer as the plugin's uvRect shows it (LiquidLayer)."""
        tex = st.img(name)
        tw, th = size(name)
        uw, uh = aw * density / tw, ah * density / th
        px = tex.width
        crop = tex.crop((int(offset * px), 0, int((offset + uw) * px), int(uh * tex.height) if uh <= 1 else tex.height))
        return crop.resize((aw, ah), Image.LANCZOS)

    def liquid_h(name, aw, ah, density=1.4, voff=0.3):
        tex = st.img(name)
        tw, th = size(name)
        uw, uh = aw * density / tw, ah * density / th
        reps = int(uw) + 1
        strip = Image.new("RGBA", (tex.width * reps, tex.height))
        for i in range(reps):
            strip.paste(tex, (i * tex.width, 0))
        y0 = int((1 - voff - uh) * tex.height)
        return strip.crop((0, y0, int(uw * tex.width), y0 + int(uh * tex.height))).resize((aw, ah), Image.LANCZOS)

    # Vital bars [Vitals] OffsetX 36, OffsetY 72 (UI y grows upward), Diego's three frames.
    def bar(x, y, frame, liq, frac, trail, value, hot):
        w, h = size(frame)
        top = H - (y + h)
        cl, cb, cr, ct = (int(round(v)) for v in st.content(frame, (10, 24, 10, 32)))
        aw, ah = int(w - cl - cr), int(h - cb - ct)
        fh, th = int(ah * frac), int(ah * trail)
        layer = Image.new("RGBA", (w, h))
        layer.alpha_composite(dressed(frame, w, h))                       # material behind the empty part
        body = liquid_v(liq, aw, ah)
        body.alpha_composite(tint(liquid_v(liq, aw, ah, 1.12, 0.2).transpose(Image.FLIP_LEFT_RIGHT), (255, 255, 255), 0.35))
        lq = Image.new("RGBA", (w, h))
        lq.paste(body.crop((0, ah - fh, aw, ah)), (cl, ct + ah - fh))
        if th > fh and st.has("bar_burn"):
            bh = min(th - fh, 9)
            lq.alpha_composite(tint(st.img("bar_burn").resize((aw, bh)), hot, 0.95), (cl, ct + ah - fh - bh))
        lq.alpha_composite(Image.new("RGBA", (aw, 2), (255, 255, 255, 90)), (cl, ct + ah - fh))
        opening = st.img(frame + "_opening").resize((w, h), Image.LANCZOS).split()[3]
        layer.paste(lq, (0, 0), ImageChops.multiply(opening, lq.split()[3]))
        layer.alpha_composite(st.img(frame).resize((w, h), Image.LANCZOS))
        put(layer, x, top)
        if st.has("bar_value"):
            pw = int(w * 0.92)
            ph = int(pw * st.meta["bar_value"]["height"] / st.meta["bar_value"]["width"])
            py = top + ct + ah - int(ah * 0.26) - ph // 2
            put(st.img("bar_value").resize((pw, ph), Image.LANCZOS), x + (w - pw) / 2, py)
            text(x + w / 2, py + ph / 2 - 1, str(value), font("Cinzel-SemiBold", 17 if w > 40 else 15))
        return w

    vx, vy = 36, 72
    wh = bar(vx, vy + 22, "vital_health", "liquid_health", 0.66, 0.74, 148, (255, 158, 66))
    ws = bar(vx + wh + 6, vy + 22, "vital_stamina", "liquid_stamina", 0.8, 0.8, 132, (255, 236, 150))
    bar(vx + wh + ws + 12, vy + 22, "vital_stamina", "liquid_eitr", 0.5, 0.5, 66, (175, 238, 255))

    # Food [Food] OffsetX 172, OffsetY 90.
    for i, t in enumerate(("13m", "23m", "28m")):
        x = 172 + i * 66
        put(dressed("hotslot", 58, 58, fit=58), x, H - (90 + 58))
        text(x + 49, H - 90 - 8, t, font("Cinzel-SemiBold", 14), anchor="rs")

    # Hotbar [Hotbar] OffsetY 20, bottom centre: Diego's new eight-cell frame (R-048 test).
    pw, ph = size("hotbar_frame")
    px, ptop = W / 2 - pw / 2, H - (20 + ph)
    put(dressed("hotbar_frame", pw, ph), px, ptop)
    cl, cb, cr, ct = st.content("hotbar_frame", (32, 12, 32, 12))
    gap = st.meta["hotbar_frame"].get("gap", 4)
    cw, chh = (pw - cl - cr - 7 * gap) / 8, ph - cb - ct
    for i in range(8):
        x = px + cl + i * (cw + gap)
        text(x + cw * 0.14, ptop + ct + chh * 0.18, str(i + 1), font("Cinzel-Medium", 13), anchor="lm")
        state = {0: "hotslot_equipped", 2: "hotslot_selected"}.get(i)
        if state:
            sw_, sh_ = size(state)
            scl, sbt, scr, sct = st.content(state, (0, 0, 0, 0))
            kk = cw / (sw_ - scl - scr)
            put(st.img(state).resize((int(sw_ * kk), int(sh_ * kk)), Image.LANCZOS), x - scl * kk, ptop + ct - sct * kk)

    # Stamina readout [Sprint] OffsetY 142: Diego's frame at its own proportions.
    fw, fh = size("sprint_frame")
    k = 1.0
    sw, sh = int(fw * k), int(fh * k)
    sx, stop = W / 2 - sw / 2, H - (142 + sh)
    frame_img = dressed("sprint_frame", sw, sh)
    cl, cb, cr, ct = (v * k for v in st.content("sprint_frame", (40, 10, 40, 33)))
    tw, thh = int(sw - cl - cr), int(sh - cb - ct)
    lq = Image.new("RGBA", (sw, sh))
    lq.paste(liquid_h("liquid_stamina_h", tw, thh, 1.2).crop((0, 0, int(tw * 0.62), thh)), (int(cl), int(ct)))
    lq.alpha_composite(st.img("bar_burn_h").resize((max(1, int(tw * 0.04)), thh), Image.LANCZOS),
                       (int(cl + tw * 0.62), int(ct)))
    opening = st.img("sprint_frame_opening").resize((sw, sh), Image.LANCZOS).split()[3]
    frame_img.paste(lq, (0, 0), ImageChops.multiply(opening, lq.split()[3]))
    frame_img.alpha_composite(st.img("sprint_frame").resize((sw, sh), Image.LANCZOS))
    put(frame_img, sx, stop)

    # Minimap [Minimap] OffsetX 24, OffsetY 20, top-right.
    gx, ring_top = W - 24 - 250, 1 + 40
    cl, cb, cr, ct = st.content("map_ring", (14, 14, 14, 14))
    ms = int(250 - cl - cr)
    world = Image.new("RGBA", (ms, ms), (90, 120, 70, 255))
    wd = ImageDraw.Draw(world)
    for _ in range(120):
        x, y = rng.randrange(ms), rng.randrange(ms)
        wd.ellipse((x, y, x + 24, y + 24), fill=(70 + rng.randrange(40), 110 + rng.randrange(30), 60, 255))
    bg.paste(world, (int(gx + cl), int(ring_top + ct)), st.img("map_mask").split()[3].resize((ms, ms)))
    put(st.img("map_ring").resize((250, 250), Image.LANCZOS), gx, ring_top)
    ctop = ring_top - 32 + 18                                          # same plate as the biome's, over the ring's top
    put(dressed("map_banner", 180, 32, fit=32), gx + 35, ctop)
    put(st.img("wind_disk").resize((22, 22), Image.LANCZOS), gx + 35 + 24, ctop + 5)
    put(st.img("wind_arrow").resize((18, 18), Image.LANCZOS).rotate(-40, resample=Image.BICUBIC), gx + 35 + 26, ctop + 7)
    text(gx + 35 + 50 + (180 - 72) / 2, ctop + 16, "Dia 4 · 07:26", font("Cinzel-Medium", 14))
    put(dressed("map_banner", 180, 32, fit=32), gx + 35, ring_top + 250 - 18)
    text(gx + 125, ring_top + 250 - 2, "PRADO", font("Cinzel-Medium", 14), fill=(247, 226, 131, 255))
    text(gx + 125, ring_top + 22, "N", font("Cinzel-SemiBold", 13), fill=(247, 226, 131, 255))

    # Status [Status] OffsetX 24, OffsetY 340, right to left.
    for i, (name, t) in enumerate((("Eikthyr", "19:54"), ("Molhado(a)", "1:50"), ("Frio", ""))):
        x = W - 24 - 70 - i * 70
        put(dressed("hotslot", 60, 60, fit=60), x + 5, 315)
        text(x + 35, 315 + 71, name, font("CormorantGaramond-SemiBold", 14))
        text(x + 35, 315 + 89, t, font("Cinzel-Medium", 13))
    put(st.img("badge_cooldown").resize((20, 20), Image.LANCZOS), W - 24 - 70 + 5 + 44, 313)

    # Boss plate [Boss] OffsetY 18, top centre, 520 x 74, Diego's plate.
    bw, bh = 520, 74
    bx, btop = W / 2 - bw / 2, 12
    put(dressed("boss_plate", bw, bh, fit=bh), bx, btop)
    cl, cb, cr, ct = st.content("boss_plate", (22, 12, 22, 12))
    ch = bh - cb - ct
    text(W / 2, btop + ct + ch * 0.25, "Troll das Montanhas", font("Cinzel-SemiBold", 20))
    barx, bary, barw, barh = bx + cl + 6, btop + ct + ch * 0.55 + 2, int(bw - cl - cr - 12), int(ch * 0.45 - 4)
    put(Image.new("RGBA", (barw, barh), (0, 0, 0, 140)), barx, bary)
    put(liquid_h("liquid_health_h", barw, barh).crop((0, 0, int(barw * 0.76), barh)), barx, bary)
    text(W / 2, bary + barh / 2, "1370 / 1800", font("Cinzel-Medium", 12))

    # Creature plate [Enemy]: the generated boss plate drawn at 0.3 (as approved in F3).
    ew, eh = 112, 18
    ex, etop = 700, 560
    k = 0.3
    put(st.nine("enemy_plate", ew, eh, k), ex, etop)
    cl, cb, cr, ct = (v * k for v in st.content("enemy_plate", (22, 12, 22, 12)))
    bx2, by2, bw2, bh2 = ex + cl + 1, etop + ct + 1, int(ew - cl - cr - 2), max(1, int(eh - cb - ct - 2))
    put(Image.new("RGBA", (bw2, bh2), (0, 0, 0, 140)), bx2, by2)
    put(tint(st.img("bar_fill").resize((int(bw2 * 0.8), bh2)), (0xA5, 0x1C, 0x1E)), bx2, by2)
    put(st.img("star").resize((10, 10), Image.LANCZOS), ex + ew / 2 - 5, etop - 5)
    text(ex + ew / 2, etop - 14, "Anão Cinzento", font("Cinzel-Medium", 14))
    text(ex - 8, etop + eh / 2, "!", font("Cinzel-SemiBold", 16), fill=(224, 100, 60, 255))

    # Hover card [Hover] OffsetX 60, OffsetY 30 from the centre.
    cw, chh = 200, 70
    hx, htop = W / 2 + 60, H / 2 - 30 - chh / 2
    put(dressed("card", cw, chh), hx, htop)
    ornament(hx, htop, cw, chh, "card_knot_left", "left")
    ornament(hx, htop, cw, chh, "card_knot_right", "right")
    text(hx + 28, htop + 25, "Baú de Madeira", font("Cinzel-Medium", 16), fill=(247, 226, 131, 255), anchor="lm")
    text(hx + 28, htop + 47, "[E] Abrir", font("CormorantGaramond-SemiBold", 17), anchor="lm")

    # Notice [Notice] OffsetX 24, OffsetY 24, top-left: the slim plate at 46 high.
    put(dressed("notice_plate", 230, 46, fit=46), 24, 24)
    text(24 + 64, 24 + 23, "Madeira  x5", font("CormorantGaramond-SemiBold", 18), anchor="lm")

    bg.save(out_path)
    base, ext = os.path.splitext(out_path)
    bg.crop((0, 640, 900, 1080)).resize((1350, 660), Image.LANCZOS).save(base + "-bottom-left" + ext)
    bg.crop((1250, 0, 1920, 480)).save(base + "-top-right" + ext)
    bg.crop((560, 0, 1360, 620)).save(base + "-centre" + ext)
    print("mock written to " + out_path)




def window_mock(out_path):
    """The window shell (F4.1): top bar with title and six tabs, key-hint bar, over the backdrop."""
    st = Style()
    src = Image.open(BACKDROP).convert("RGBA") if os.path.exists(BACKDROP) else Image.new("RGBA", (W, H), (46, 58, 44, 255))
    bg = src.resize((W, H), Image.LANCZOS)

    def size(name):
        return st.meta[name]["width"], st.meta[name]["height"]

    def dressed(name, w, h, fit=None, alpha=0.85):
        k = fit / size(name)[1] if fit else 1.0
        w, h = int(round(w)), int(round(h))
        out = Image.new("RGBA", (w, h))
        if st.has(name + "_shape") and st.has("panel_bg"):
            mat = tiled(st.img("panel_bg"), w, h, size("panel_bg")[0])
            out.paste(mat, (0, 0), st.nine(name + "_shape", w, h, k).split()[3].point(lambda v: int(v * alpha)))
        out.alpha_composite(st.nine(name, w, h, k))
        return out

    def text(x, y, s, f, fill=(251, 243, 218, 255), anchor="mm"):
        ImageDraw.Draw(bg).text((x, y), s, font=f, fill=fill, anchor=anchor, stroke_width=1, stroke_fill=(0, 0, 0, 200))

    def icon(name, cx, top, h):
        w0, h0 = size(name)
        im = st.img(name).resize((max(1, int(w0 * h / h0)), int(h)), Image.LANCZOS)
        bg.alpha_composite(im, (int(cx - im.width / 2), int(top)))

    def key(x, cy, label):
        w = 26 if len(label) <= 2 else max(40, 12 * len(label) + 6)
        bg.alpha_composite(dressed("keycap" if w == 26 else "keycap_wide", w, 26, fit=26), (int(x), int(cy - 13)))
        text(x + w / 2, cy, label, font("Cinzel-Medium", 13))
        return w

    # Match WindowCanvas (75% of both axes), WindowShell and InventoryWindowModule.
    area_w, area_h = int(W * 0.75), int(H * 0.75)
    side_, area_top = (W - area_w) // 2, (H - area_h) // 2
    top, bar_h, bw = area_top, 64, area_w
    bg.alpha_composite(dressed("window_topbar", bw, bar_h, fit=bar_h), (side_, top))
    k = bar_h / size("window_topbar")[1]
    cl, cb, cr, ct = (v * k for v in st.content("window_topbar", (150, 12, 150, 12)))
    key(side_ + cl + 4, top + bar_h / 2, "Q")
    x0, x1 = side_ + cl + 44, side_ + bw - cr - 44
    key(side_ + bw - cr + 4 - 26, top + bar_h / 2, "E")
    tabs = [("icon_inventory", "INVENTÁRIO"), ("icon_skills", "HABILIDADES"), ("icon_map", "MAPA"), ("icon_crafting", "CRIAÇÃO"),
            ("icon_achievements", "CONQUISTAS"), ("icon_settings", "CONFIGURAÇÕES")]
    tw = (x1 - x0) / len(tabs)
    kw, kh = size("tab_knot")
    for i, (ic, label) in enumerate(tabs):
        cx = x0 + tw * (i + 0.5)
        icon(ic, cx, top + ct + 3, 24)
        text(cx, top + bar_h - cb - 12, label, font("Cinzel-Medium", 14), fill=(247, 226, 131, 255) if i == 0 else (186, 153, 92, 255))
        if i:
            knot = st.img("tab_knot").resize((max(1, int(kw * 8 / kh)), 8), Image.LANCZOS)
            bg.alpha_composite(knot, (int(x0 + tw * i - knot.width / 2), int(top + bar_h / 2 - 4)))
        if i == 0:
            m = st.nine("tab_marker", int(tw * 0.76), 10, 10 / size("tab_marker")[1])
            bg.alpha_composite(m, (int(x0 + tw * 0.12), int(top + bar_h - cb - 7)))

    # Inventory tab (F4.2a): three panels between the bars, vanilla's slots dressed on the grid.
    top_, gap_ = area_top + 72, 8
    ph = area_h - 72 - 54
    shares = [0.47, 0.25, 0.28]
    widths = [int(area_w * shares[0] - gap_ / 2), int(area_w * shares[1] - gap_),
              int(area_w * shares[2] - gap_ / 2)]
    titles = ["INVENTÁRIO", "EQUIPAMENTO", "DETALHES DO ITEM"]
    x = side_
    for w, ttl in zip(widths, titles):
        bg.alpha_composite(dressed("window_panel", w, ph), (int(x), top_))
        rule = st.meta["window_panel_rule_knot"].get("inset", 38)
        text(x + w / 2, top_ + rule - 26 + 10, ttl, font("Cinzel-SemiBold", 16), fill=(247, 226, 131, 255))
        marker = st.nine("tab_marker", int(w * 0.6), 8, 8 / size("tab_marker")[1])
        bg.alpha_composite(marker, (int(x + w * 0.2), int(top_ + rule - 4)))
        x += w + gap_
    cell, cg = min(64, int((widths[0] - 60 - 7 * 6) / 8), int((ph - 80 - 70 - 5 * 6) / 6)), 6
    gw = 8 * cell + 7 * cg
    gx0, gy0 = side_ + widths[0] / 2 - gw / 2, top_ + 80
    text(side_ + 28, top_ + 50, "17/32", font("Cinzel-Medium", 13), fill=(186, 153, 92, 255), anchor="lm")
    bg.alpha_composite(dressed("keycap_wide", 150, 26, fit=26), (int(side_ + widths[0] - 24 - 150), top_ + 46))
    text(side_ + widths[0] - 24 - 75, top_ + 59, "Todos  ◆", font("CormorantGaramond-SemiBold", 15))
    for r in range(4):
        for c in range(8):
            bg.alpha_composite(dressed("hotslot", cell, cell, fit=cell), (int(gx0 + c * (cell + cg)), int(gy0 + r * (cell + cg))))
    wy = top_ + ph - 38
    text(side_ + 30, wy, "PESO", font("Cinzel-Medium", 13), fill=(186, 153, 92, 255), anchor="lm")
    ImageDraw.Draw(bg).rectangle((side_ + 94, wy - 2, side_ + widths[0] - 104, wy + 2), fill=(0, 0, 0, 140))
    ImageDraw.Draw(bg).rectangle((side_ + 94, wy - 2, side_ + 94 + (widths[0] - 198) * 0.7, wy + 2), fill=(190, 140, 60, 255))
    text(side_ + widths[0] - 30, wy, "151 / 300", font("Cinzel-Medium", 14), anchor="rm")
    ex = side_ + widths[0] + gap_
    equipment = ["CABEÇA", "TRINKET", "PEITO", "CINTO", "CAPA", "PERNAS"]
    for i, label in enumerate(equipment):
        column, row = i % 2, i // 2
        cx = ex + widths[1] * (0.23 if column == 0 else 0.77)
        cy = top_ + ph * (0.20 + row * 0.22)
        bg.alpha_composite(dressed("hotslot", cell, cell, fit=cell),
                           (int(cx - cell / 2), int(cy - cell / 2)))
        text(cx, cy + cell / 2 + 13, label, font("Cinzel-Medium", 11),
             fill=(186, 153, 92, 255))
    text(ex + widths[1] / 2, top_ + ph - 70, "PROTEÇÃO TOTAL", font("Cinzel-Medium", 12), fill=(186, 153, 92, 255))
    text(ex + widths[1] / 2, top_ + ph - 42, "24", font("Cinzel-SemiBold", 26))

    hint_h = 46
    hy = area_top + area_h - hint_h
    bg.alpha_composite(dressed("window_hintbar", bw, hint_h, fit=hint_h), (side_, hy))
    items = [("Esc", None, "Fechar"), (None, "icon_mouse_left", "Mover"), (None, "icon_mouse_right", "Usar / Equipar"),
             ("Shift", "icon_mouse_left", "Dividir pilha"), ("Ctrl", "icon_mouse_left", "Transferir"), ("Q/E", None, "Abas")]
    f = font("CormorantGaramond-SemiBold", 17)
    widths = []
    for kname, mouse, label in items:
        w = (len(kname) * 12 + 18 if kname and len(kname) > 2 else 26 if kname else 0) + (22 if mouse else 0) + (16 if kname and mouse else 0) + f.getlength(label) + 12
        widths.append(w)
    x = W / 2 - (sum(widths) + 26 * (len(items) - 1)) / 2
    cy = hy + hint_h / 2
    for (kname, mouse, label), w in zip(items, widths):
        if kname:
            x += key(x, cy, kname) + 6
        if kname and mouse:
            text(x + 4, cy, "+", font("CormorantGaramond-Medium", 16)); x += 16
        if mouse:
            icon(mouse, x + 8, cy - 12, 24); x += 22
        text(x, cy, label, f, anchor="lm")
        x += f.getlength(label) + 12 + 26
    bg.save(out_path)
    print("window mock written to " + out_path)


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "dist", "hud-mock.png"))
    window_mock(os.path.join(os.path.dirname(sys.argv[1]) if len(sys.argv) > 1 else os.path.join(ROOT, "dist"), "window-mock.png"))
