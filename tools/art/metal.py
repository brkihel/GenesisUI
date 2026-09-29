"""Thin-line metal: the proposed GenesisUI frame language, rendered in Python.

    tools/.venv/bin/python tools/art/metal.py [out_dir]

Writes the shipped thin-line pieces to art/src/metal/ (render.py merges them over the sheet
pieces) and two review images to out_dir.

Prototype for Diego's review (2026-09-29): frames drawn as thin centre lines and small
ornaments, lit as polished metal (a rounded bead profile per line, light from the top left,
bronze-to-gold ramp, specular highlight and a travelling glint). This is what the planned
Unity UI shader "GenesisUI/Metal" will do at runtime from a distance field; the Python
version exists so the look can be judged before any Unity work. Writes window-metal.png
(the Inventory tab on the design board) and bars-burn.png (the new burn light).
"""
import json
import os
import random
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont
from scipy import ndimage

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
FONTS = os.path.join(ROOT, "art", "fonts")
OUT = os.path.join(ROOT, "art", "out")
BACKDROP = os.path.expanduser("~/GenesisUI-Concept/ConceptArt (3).png")

# Metal ramp (value 0..1 -> colour): deep bronze, bronze, old gold, pale highlight. Darker
# than the sheets (Diego: "a UI toda mais escura").
RAMP = [(0.0, (22, 14, 7)), (0.35, (78, 52, 24)), (0.62, (150, 108, 52)), (0.85, (206, 164, 94)), (1.0, (250, 230, 182))]
LIGHT = np.array([-0.45, -0.65, 0.62])  # from the top left (y grows downwards)


def ramp(v):
    v = np.clip(v, 0.0, 1.0)
    out = np.zeros(v.shape + (3,), np.float32)
    for (a, ca), (b, cb) in zip(RAMP, RAMP[1:]):
        t = np.clip((v - a) / (b - a), 0.0, 1.0)
        sel = (v >= a) & (v <= b)
        for i in range(3):
            out[..., i] = np.where(sel, ca[i] + (cb[i] - ca[i]) * t, out[..., i])
    return out


class Metal:
    """Collects centre lines and filled ornaments in design units, renders them as lit metal."""

    def __init__(self, w, h, ss=2):
        self.w, self.h, self.ss = w, h, ss
        self.lines = {}  # width -> centre-line mask
        self.fills = Image.new("L", (w * ss, h * ss))
        self.glints = []

    def _mask(self, width):
        if width not in self.lines:
            self.lines[width] = Image.new("L", (self.w * self.ss, self.h * self.ss))
        return ImageDraw.Draw(self.lines[width])

    def line(self, points, width=1.6, closed=False):
        pts = [(x * self.ss, y * self.ss) for x, y in points]
        if closed:
            pts.append(pts[0])
        self._mask(width).line(pts, fill=255, width=1)

    def fill(self, points):
        ImageDraw.Draw(self.fills).polygon([(x * self.ss, y * self.ss) for x, y in points], fill=255)

    def diamond(self, cx, cy, r, ry=None):
        ry = ry or r
        self.fill([(cx, cy - ry), (cx + r, cy), (cx, cy + ry), (cx - r, cy)])

    def dot(self, cx, cy, r):
        s = self.ss
        ImageDraw.Draw(self.fills).ellipse(((cx - r) * s, (cy - r) * s, (cx + r) * s, (cy + r) * s), fill=255)

    def chamfer_rect(self, x, y, w, h, cut, width=1.6):
        self.line([(x + cut, y), (x + w - cut, y), (x + w, y + cut), (x + w, y + h - cut), (x + w - cut, y + h),
                   (x + cut, y + h), (x, y + h - cut), (x, y + cut)], width, closed=True)

    def glint(self, cx, cy, strength=1.0):
        self.glints.append((cx, cy, strength))

    def _fields(self):
        """Coverage, height and unit normals (image space, y down) at the supersampled size."""
        s = self.ss
        H, W = self.h * s, self.w * s
        height = np.zeros((H, W), np.float32)
        cover = np.zeros((H, W), np.float32)
        for width, img in self.lines.items():
            centre = np.asarray(img) > 0
            if not centre.any():
                continue
            d = ndimage.distance_transform_edt(~centre) / s  # design units to the centre line
            r = width / 2.0
            cover = np.maximum(cover, np.clip((r - d) * s + 0.5, 0.0, 1.0))
            height = np.maximum(height, np.sqrt(np.clip(1.0 - (d / (r + 0.35)) ** 2, 0.0, 1.0)))
        fill = np.asarray(self.fills) > 0
        if fill.any():
            inside = ndimage.distance_transform_edt(fill) / s
            outside = ndimage.distance_transform_edt(~fill) / s
            cover = np.maximum(cover, np.clip(0.5 + (inside - outside) * s, 0.0, 1.0))
            height = np.maximum(height, np.clip(inside / 1.4, 0.0, 1.0) ** 0.6)
        height = ndimage.gaussian_filter(height, 0.6 * s)
        gy, gx = np.gradient(height * 1.4 * s)
        n = np.dstack([-gx, -gy, np.ones_like(height)])
        n /= np.linalg.norm(n, axis=2, keepdims=True)
        return cover, height, n

    def render(self, out_scale=1, glints=True):
        """The lit metal as colour (what the shader draws; also the fallback sprite)."""
        s = self.ss
        cover, height, n = self._fields()
        H, W = cover.shape
        l = LIGHT / np.linalg.norm(LIGHT)
        diffuse = np.clip((n * l).sum(axis=2), 0.0, 1.0)
        half = l + np.array([0.0, 0.0, 1.0])
        half /= np.linalg.norm(half)
        spec = np.clip((n * half).sum(axis=2), 0.0, 1.0) ** 48
        value = 0.18 + 0.62 * diffuse + 0.08 * n[..., 2]
        rgb = ramp(value)
        glint = np.zeros((H, W), np.float32)
        if glints and self.glints:
            ys, xs = np.mgrid[0:H, 0:W]
            for cx, cy, st in self.glints:
                band = np.exp(-(((xs / s - cx) + (ys / s - cy)) ** 2) / (2 * 14.0 ** 2))
                near = np.exp(-(((xs / s - cx) ** 2 + (ys / s - cy) ** 2)) / (2 * 120.0 ** 2))
                glint = np.maximum(glint, band * near * st)
        light = (spec * 0.9 + glint * (0.35 + 0.65 * diffuse)) * height
        rgb = rgb + light[..., None] * np.array([250, 230, 182]) * 0.8
        rgba = np.dstack([np.clip(rgb, 0, 255), cover * 255]).astype(np.uint8)
        return Image.fromarray(rgba, "RGBA").resize((self.w * out_scale, self.h * out_scale), Image.LANCZOS)

    def relief(self, out_scale=1):
        """The shader's input: RG = normal xy in Unity's space (y up), B = height, A = coverage. Linear data."""
        cover, height, n = self._fields()
        r = n[..., 0] * 0.5 + 0.5
        g = -n[..., 1] * 0.5 + 0.5  # image y grows down, Unity's up
        data = np.dstack([r * 255, g * 255, height * 255, cover * 255]).astype(np.float32)
        # Average in premultiplied form so the transparent surroundings do not bleed into the edge normals.
        a = data[..., 3:4] / 255.0
        pre = np.dstack([data[..., :3] * a, data[..., 3:4]])
        img = Image.fromarray(np.clip(pre, 0, 255).astype(np.uint8), "RGBA").resize((self.w * out_scale, self.h * out_scale), Image.BOX)
        arr = np.asarray(img).astype(np.float32)
        a2 = np.maximum(arr[..., 3:4] / 255.0, 1e-4)
        rgb = np.where(arr[..., 3:4] > 0, arr[..., :3] / a2, np.array([127.5, 127.5, 0.0]))
        return Image.fromarray(np.dstack([np.clip(rgb, 0, 255), arr[..., 3:4]]).astype(np.uint8), "RGBA")


def font(name, size):
    return ImageFont.truetype(os.path.join(FONTS, name + ".ttf"), size)


def window(out_path):
    BW, BH = 1580, 850
    m = Metal(BW, BH)
    board = Image.new("RGBA", (BW, BH))
    under = Image.new("RGBA", (BW, BH))
    title, flavor, body = (214, 178, 108, 255), (140, 118, 82, 255), (196, 186, 164, 255)

    bg = Image.open(os.path.join(OUT, "panel_bg.png")).convert("RGBA")

    def plate(x, y, w, h, cut, alpha=0.9):
        """The panel material inside a chamfered silhouette, darkened."""
        mask = Image.new("L", (w, h))
        ImageDraw.Draw(mask).polygon([(cut, 0), (w - cut, 0), (w, cut), (w, h - cut), (w - cut, h), (cut, h), (0, h - cut), (0, cut)],
                                     fill=int(255 * alpha))
        tile = bg.resize((512, 512 * bg.height // bg.width))
        mat = Image.new("RGBA", (w, h))
        for yy in range(0, h, tile.height):
            for xx in range(0, w, tile.width):
                mat.paste(tile, (xx, yy))
        mat = Image.blend(mat, Image.new("RGBA", (w, h), (6, 6, 5, 255)), 0.45)
        under.paste(mat, (x, y), mask)

    def frame(x, y, w, h, cut=14, knots=True):
        plate(x, y, w, h, cut)
        m.chamfer_rect(x + 0.5, y + 0.5, w - 1, h - 1, cut, 1.8)
        m.chamfer_rect(x + 6, y + 6, w - 12, h - 12, cut - 4, 0.8)
        if knots:
            for cx, cy, sx, sy in ((x, y, 1, 1), (x + w, y, -1, 1), (x, y + h, 1, -1), (x + w, y + h, -1, -1)):
                m.diamond(cx + sx * cut * 0.5, cy + sy * cut * 0.5, 3.2)
                m.line([(cx + sx * (cut + 6), cy + sy * 3), (cx + sx * (cut + 26), cy + sy * 3)], 0.7)
                m.line([(cx + sx * 3, cy + sy * (cut + 6)), (cx + sx * 3, cy + sy * (cut + 26))], 0.7)

    def rule(x, y, w):
        m.line([(x, y), (x + w / 2 - 9, y)], 0.8)
        m.line([(x + w / 2 + 9, y), (x + w, y)], 0.8)
        m.diamond(x + w / 2, y, 4.5, 3.2)
        m.dot(x, y, 1.3)
        m.dot(x + w, y, 1.3)

    def slot(x, y, c, state=None):
        plate(x, y, int(c), int(c), 5, 0.75)
        m.chamfer_rect(x + 0.5, y + 0.5, c - 1, c - 1, 5, 1.2 if state else 1.0)
        if state == "selected":
            m.chamfer_rect(x - 3, y - 3, c + 6, c + 6, 7, 0.8)
            for cx, cy in ((x + c / 2, y - 3), (x + c / 2, y + c + 3)):
                m.diamond(cx, cy, 3.4, 2.4)
        if state == "equipped":
            m.diamond(x + c / 2, y, 3.0, 2.2)

    def text(x, y, s, f, fill=body, anchor="mm", spacing=0):
        d = ImageDraw.Draw(board)
        if spacing:
            total = sum(f.getlength(ch) for ch in s) + spacing * (len(s) - 1)
            xx = x - total / 2 if anchor[0] == "m" else x
            for ch in s:
                d.text((xx, y), ch, font=f, fill=fill, anchor="l" + anchor[1])
                xx += f.getlength(ch) + spacing
        else:
            d.text((x, y), s, font=f, fill=fill, anchor=anchor)

    def item(x, y, c, colour):
        ImageDraw.Draw(board).ellipse((x + c * 0.3, y + c * 0.3, x + c * 0.7, y + c * 0.7), fill=colour)

    # Tab bar and hint bar.
    frame(0, 0, BW, 90, 16)
    for i, name in enumerate(["INVENTÁRIO", "HABILIDADES", "MAPA", "CRIAÇÃO", "CONQUISTAS", "CONFIGURAÇÕES"]):
        cx = 190 + i * 240
        text(cx, 58, name, font("Cinzel-Medium", 16), title if i == 0 else flavor, spacing=2)
        m.diamond(cx, 30, 6, 8) if i == 0 else m.diamond(cx, 30, 4, 6)
        if i:
            m.diamond(cx - 120, 45, 2.2)
        if i == 0:
            rule(cx - 80, 78, 160)
    for x, k in ((40, "Q"), (BW - 70, "E")):
        m.chamfer_rect(x, 30, 30, 30, 5, 1.0)
        text(x + 15, 45, k, font("Cinzel-Medium", 15), title)
    frame(0, 788, BW, 62, 14)
    hx = 170
    for key, label in (("Esc", "Fechar"), ("RMB", "Usar / Equipar"), ("LMB", "Mover"), ("Shift", "Dividir pilha"),
                       ("Ctrl", "Transferir"), ("R", "Organizar"), ("Q/E", "Abas")):
        kw = 30 if len(key) <= 2 else 16 + 11 * len(key)
        m.chamfer_rect(hx, 804, kw, 30, 5, 1.0)
        text(hx + kw / 2, 819, key, font("Cinzel-Medium", 13), title)
        text(hx + kw + 10, 819, label, font("CormorantGaramond-SemiBold", 19), body, anchor="lm")
        hx += kw + 10 + font("CormorantGaramond-SemiBold", 19).getlength(label) + 46

    # Panels.
    top = 102
    frame(0, top, 816, 673)
    frame(820, top, 412, 673)
    frame(1236, top, 344, 673)
    text(88, top + 32, "INVENTÁRIO", font("Cinzel-SemiBold", 25), title, anchor="lm", spacing=6)
    rule(82, top + 56, 300)
    text(820 + 54, top + 32, "EQUIPAMENTO", font("Cinzel-SemiBold", 22), title, anchor="lm", spacing=5)
    rule(820 + 48, top + 56, 250)
    text(1236 + 172, top + 32, "DETALHES DO ITEM", font("Cinzel-SemiBold", 18), title, spacing=4)
    rule(1236 + 60, top + 56, 224)
    text(46, top + 80, "17/32 SLOTS EM USO", font("Cinzel-Medium", 14), flavor, anchor="lm", spacing=2)
    for x, w, label in ((526, 140, "Todos  ◆"), (678, 100, "Organizar")):
        m.chamfer_rect(x, top + 30, w, 36, 6, 1.0)
        text(x + w / 2, top + 48, label, font("CormorantGaramond-SemiBold", 17), body)

    rng = random.Random(3)
    colours = [(150, 106, 62, 255), (122, 130, 138, 255), (104, 140, 80, 255), (168, 72, 62, 255)]
    for r in range(4):
        for c in range(8):
            x, y = 44 + c * 89, top + 96 + r * 92
            state = "selected" if (r, c) == (0, 0) else "equipped" if (r, c) == (0, 1) else None
            slot(x, y, 82, state)
            if rng.random() < 0.55 or r == 0 and c < 3:
                item(x, y, 82, rng.choice(colours))
                text(x + 74, y + 72, str(rng.randint(2, 50)), font("Cinzel-SemiBold", 16), body, anchor="rs")
            if r == 0:
                text(x + 9, y + 7, str(c + 1), font("Cinzel-Medium", 14), flavor, anchor="lt")
    m.line([(44 + 8 * 89 + 6, top + 96), (44 + 8 * 89 + 6, top + 96 + 358)], 0.6)
    m.line([(44 + 8 * 89 + 6, top + 96), (44 + 8 * 89 + 6, top + 196)], 1.6)
    text(56, top + 489, "CONSUMO RÁPIDO", font("Cinzel-Medium", 14), title, anchor="lm", spacing=3)
    text(445, top + 489, "SLOTS DE AÇÃO", font("Cinzel-Medium", 14), title, anchor="lm", spacing=3)
    m.line([(422, top + 482), (422, top + 586)], 0.7)
    m.diamond(422, top + 534, 2.6, 4)
    for i in range(4):
        slot(54 + i * 85, top + 512, 74)
        item(54 + i * 85, top + 512, 74, colours[3])
        text(54 + i * 85 + 66, top + 578, f"{rng.randint(2, 10)}/10", font("Cinzel-SemiBold", 14), body, anchor="rs")
        slot(445 + i * 82, top + 512, 74)
        item(445 + i * 82, top + 512, 74, colours[1])
        m.chamfer_rect(445 + i * 82 + 4, top + 516, 18, 18, 3, 0.8)
        text(445 + i * 82 + 13, top + 525, "ZXCV"[i], font("Cinzel-Medium", 11), title)
    text(56, top + 639, "PESO", font("Cinzel-Medium", 15), flavor, anchor="lm", spacing=2)
    m.line([(130, top + 639), (580, top + 639)], 0.7)
    m.dot(130, top + 639, 1.4)
    m.dot(580, top + 639, 1.4)
    ImageDraw.Draw(board).rectangle((131, top + 637, 131 + 280, top + 641), fill=(170, 124, 58, 255))
    text(600, top + 639, "186 / 300", font("Cinzel-Medium", 16), body, anchor="lm")

    for label, i in (("Cabeça", 0), ("Peito", 1), ("Capa", 2), ("Pernas", 3), ("Trinket", 4), ("Cinto", 5)):
        column, row = (0 if i < 4 else 1), i % 4
        x = 820 + (24 if column == 0 else 412 - 24 - 80)
        y = top + 96 + row * 120
        slot(x, y, 80, "equipped" if i < 2 else None)
        if i < 2:
            item(x, y, 80, colours[1])
        text(x + 40, y + 96, label, font("CormorantGaramond-SemiBold", 16), body)
    rule(820 + 106, top + 580, 200)
    text(820 + 206, top + 600, "PROTEÇÃO TOTAL", font("Cinzel-Medium", 13), flavor, spacing=3)
    text(820 + 206, top + 634, "24", font("Cinzel-SemiBold", 32), title)

    dx = 1236
    ImageDraw.Draw(board).ellipse((dx + 112, top + 90, dx + 232, top + 210), fill=colours[0])
    text(dx + 24, top + 252, "TOCHA", font("Cinzel-SemiBold", 23), title, anchor="lm", spacing=3)
    text(dx + 24, top + 279, "Arma", font("CormorantGaramond-SemiBold", 17), flavor, anchor="lm")
    text(dx + 24, top + 308, "Ilumina os arredores e mantém", font("CormorantGaramond-Medium", 16), body, anchor="lm")
    text(dx + 24, top + 328, "os perigos da escuridão afastados.", font("CormorantGaramond-Medium", 16), body, anchor="lm")
    rule(dx + 24, top + 372, 296)
    for i, (a, b) in enumerate([("Peso", "1,0"), ("Durabilidade", "80 / 100"), ("Qualidade", "1 / 4"), ("Dano", "19"), ("Valor", "0")]):
        y = top + 399 + i * 34
        text(dx + 24, y, a, font("CormorantGaramond-SemiBold", 17), body, anchor="lm")
        text(dx + 320, y, b, font("CormorantGaramond-SemiBold", 17), (226, 214, 188, 255), anchor="rm")
        ImageDraw.Draw(board).line((dx + 24, y + 17, dx + 320, y + 17), fill=(150, 110, 60, 40))
    ImageDraw.Draw(board).rectangle((dx + 157, top + 445, dx + 157 + 130, top + 448), fill=(170, 124, 58, 255))

    m.glint(700, 100, 1.0)
    m.glint(40, top + 60, 0.8)
    m.glint(1180, top + 640, 0.7)
    metal = m.render()

    src = Image.open(BACKDROP).convert("RGBA") if os.path.exists(BACKDROP) else Image.new("RGBA", (1920, 1080), (40, 50, 40, 255))
    screen = src.resize((1920, 1080), Image.LANCZOS)
    screen = Image.blend(screen, Image.new("RGBA", screen.size, (0, 0, 0, 255)), 0.35)  # the world dims behind an open window
    full = Image.new("RGBA", (BW, BH))
    full.alpha_composite(under)
    full.alpha_composite(metal)
    full.alpha_composite(board)
    scale = min(1920 * 0.75 / BW, 1080 * 0.75 / BH)
    full = full.resize((int(BW * scale), int(BH * scale)), Image.LANCZOS)
    screen.alpha_composite(full, ((1920 - full.width) // 2, (1080 - full.height) // 2))
    screen.save(out_path)
    return out_path


def bars(out_path):
    """The burn as light: a hot core where the value just dropped, a halo leaking past the frame, embers."""
    W, H = 900, 420
    out = Image.new("RGBA", (W, H), (14, 14, 12, 255))
    rng = random.Random(5)

    def bar(x, y, w, h, liquid, fast, slow, label):
        m = Metal(W, H)
        mask = Image.new("L", (W, H))
        ImageDraw.Draw(mask).rounded_rectangle((x, y, x + w, y + h), radius=h / 2, fill=255)
        tex = Image.open(os.path.join(OUT, liquid)).convert("RGBA").resize((w, h))
        layer = Image.new("RGBA", (W, H))
        layer.paste(tex.crop((0, 0, int(w * fast), h)), (x, y))
        dark = Image.new("RGBA", (W, H))
        ImageDraw.Draw(dark).rectangle((x, y, x + w, y + h), fill=(8, 8, 7, 255))
        out.paste(dark, (0, 0), mask)
        out.alpha_composite(Image.composite(layer, Image.new("RGBA", (W, H)), mask))
        # Loss trail: faint, the same liquid dimmed between fast and slow.
        trail = Image.new("RGBA", (W, H))
        ImageDraw.Draw(trail).rectangle((x + w * fast, y, x + w * slow, y + h), fill=(255, 200, 120, 38))
        out.alpha_composite(Image.composite(trail, Image.new("RGBA", (W, H)), mask))
        # Burn: additive light at the edge, a white-hot core, orange halo beyond the frame.
        ex = x + w * fast
        glow = np.zeros((H, W, 3), np.float32)
        ys, xs = np.mgrid[0:H, 0:W]
        cy = y + h / 2
        core = np.exp(-((xs - ex) ** 2) / (2 * 1.6 ** 2)) * (np.abs(ys - cy) <= h / 2 + 0.5)
        halo = np.exp(-((xs - ex) ** 2) / (2 * 9.0 ** 2) - ((ys - cy) ** 2) / (2 * (h * 0.9) ** 2))
        tail = np.exp(-np.clip(xs - ex, 0, None) / (w * (slow - fast) * 0.35 + 1)) * (xs >= ex) * \
            np.exp(-((ys - cy) ** 2) / (2 * (h * 0.35) ** 2))
        glow += core[..., None] * np.array([255, 246, 220]) * 1.1
        glow += halo[..., None] * np.array([255, 150, 50]) * 0.85
        glow += tail[..., None] * np.array([255, 120, 30]) * 0.35
        for _ in range(9):
            px, py = ex + rng.uniform(-4, 18), cy - rng.uniform(0, h * 1.6)
            r = rng.uniform(0.6, 1.4)
            spark = np.exp(-((xs - px) ** 2 + (ys - py) ** 2) / (2 * r ** 2))
            glow += spark[..., None] * np.array([255, 200, 120]) * rng.uniform(0.5, 1.0)
        base = np.asarray(out).astype(np.float32)
        base[..., :3] = 255 - (255 - base[..., :3]) * (1 - np.clip(glow, 0, 255) / 255)  # screen blend
        out.paste(Image.fromarray(base.astype(np.uint8), "RGBA"))
        # Thin metal frame around it.
        m.line([(x + h / 2, y - 2), (x + w - h / 2, y - 2)], 1.4)
        m.line([(x + h / 2, y + h + 2), (x + w - h / 2, y + h + 2)], 1.4)
        m.diamond(x - 5, y + h / 2, 4, h / 2 + 3)
        m.diamond(x + w + 5, y + h / 2, 4, h / 2 + 3)
        m.glint(x + w * 0.3, y, 0.8)
        out.alpha_composite(m.render())
        ImageDraw.Draw(out).text((x, y - 26), label, font=font("CormorantGaramond-SemiBold", 18), fill=(196, 186, 164, 255))

    bar(70, 90, 760, 14, "liquid_stamina_h.png", 0.46, 0.72, "Vigor horizontal — queima como luz")
    bar(70, 230, 760, 22, "liquid_health_h.png", 0.63, 0.80, "Vida — mesma luz, mais larga")
    bar(70, 350, 760, 14, "liquid_eitr_h.png", 0.30, 0.41, "Eitr")
    out.save(out_path)
    return out_path


# ---------------------------------------------------------------- shipped pieces (D-033)

SCALE = 2  # like render.py: sprites at 2x design units
PIECES_DIR = os.path.join(ROOT, "art", "src", "metal")
# Sheet pieces the thin-line language replaces without a direct successor.
RETIRE = ["hotbar_frame", "hotbar_frame_shape"]


def chamfer(x, y, w, h, cut):
    return [(x + cut, y), (x + w - cut, y), (x + w, y + cut), (x + w, y + h - cut), (x + w - cut, y + h),
            (x + cut, y + h), (x, y + h - cut), (x, y + cut)]


def frame_piece(w, h, cut, tick_h=26, tick_v=26, outer=1.8, inner=0.8, gap=6, knot=3.2):
    """A window frame: chamfered outer line, fine inner line, a small diamond and two ticks per corner."""
    m = Metal(w, h, ss=4)
    o = 1.5
    m.line(chamfer(o, o, w - 2 * o, h - 2 * o, cut), outer, closed=True)
    if inner:
        m.line(chamfer(o + gap, o + gap, w - 2 * (o + gap), h - 2 * (o + gap), max(2, cut - 4)), inner, closed=True)
    if knot:
        for cx, cy, sx, sy in ((o, o, 1, 1), (w - o, o, -1, 1), (o, h - o, 1, -1), (w - o, h - o, -1, -1)):
            m.diamond(cx + sx * cut * 0.5, cy + sy * cut * 0.5, knot)
            if tick_h:
                m.line([(cx + sx * (cut + 6), cy + sy * 3), (cx + sx * (cut + tick_h), cy + sy * 3)], 0.7)
            if tick_v:
                m.line([(cx + sx * 3, cy + sy * (cut + 6)), (cx + sx * 3, cy + sy * (cut + tick_v))], 0.7)
    shape = Image.new("L", (w * SCALE, h * SCALE))
    ImageDraw.Draw(shape).polygon([(x * SCALE, y * SCALE) for x, y in chamfer(o, o, w - 2 * o, h - 2 * o, cut)], fill=255)
    return m, shape


def slot_piece(w, cut, line=1.0):
    m = Metal(w, w, ss=4)
    o = 1.0
    m.line(chamfer(o, o, w - 2 * o, w - 2 * o, cut), line, closed=True)
    shape = Image.new("L", (w * SCALE, w * SCALE))
    ImageDraw.Draw(shape).polygon([(x * SCALE, y * SCALE) for x, y in chamfer(o, o, w - 2 * o, w - 2 * o, cut)], fill=255)
    return m, shape


def pieces():
    """Writes art/src/metal/: <name>.png (lit, the fallback), <name>_relief.png (the shader's input),
    <name>_shape.png (the panel material's silhouette) and pieces.json for render.py."""
    os.makedirs(PIECES_DIR, exist_ok=True)
    for f in os.listdir(PIECES_DIR):
        os.remove(os.path.join(PIECES_DIR, f))
    meta = {}

    def save(name, m, shape, border, content=None):
        m.render(SCALE, glints=False).save(os.path.join(PIECES_DIR, name + ".png"))
        m.relief(SCALE).save(os.path.join(PIECES_DIR, name + "_relief.png"))
        meta[name] = {"border": border}
        if content:
            meta[name]["content"] = content
        meta[name + "_relief"] = {"border": border, "color": "linear"}
        if shape is not None:
            rgba = Image.new("RGBA", shape.size, (255, 255, 255, 0))
            rgba.putalpha(shape)
            rgba.save(os.path.join(PIECES_DIR, name + "_shape.png"))
            meta[name + "_shape"] = {"border": border}

    m, sh = frame_piece(160, 160, 14)
    save("window_panel", m, sh, [46, 46, 46, 46], [18, 18, 18, 18])
    m, sh = frame_piece(200, 90, 16, tick_v=0)
    save("window_topbar", m, sh, [46, 44, 46, 44], [40, 10, 40, 10])
    m, sh = frame_piece(200, 62, 14, tick_v=0)
    save("window_hintbar", m, sh, [46, 30, 46, 30], [40, 8, 40, 8])
    m, sh = frame_piece(100, 100, 8, tick_h=16, tick_v=16, outer=1.4, inner=0.6, gap=4, knot=2.4)
    save("card", m, sh, [30, 30, 30, 30], [10, 10, 10, 10])

    m, sh = slot_piece(56, 5)
    save("hotslot", m, sh, [12, 12, 12, 12], [4, 4, 4, 4])
    m, sh = slot_piece(30, 5)
    save("keycap", m, sh, [9, 9, 9, 9], [6, 6, 6, 6])
    m = Metal(60, 30, ss=4)
    m.line(chamfer(1, 1, 58, 28, 5), 1.0, closed=True)
    sh = Image.new("L", (60 * SCALE, 30 * SCALE))
    ImageDraw.Draw(sh).polygon([(x * SCALE, y * SCALE) for x, y in chamfer(1, 1, 58, 28, 5)], fill=255)
    save("keycap_wide", m, sh, [12, 9, 12, 9], [10, 6, 10, 6])

    # States over a 56-unit cell (content insets = where the cell sits).
    m = Metal(64, 64, ss=4)
    m.line(chamfer(1.2, 1.2, 61.6, 61.6, 8), 0.8, closed=True)
    m.line(chamfer(4, 4, 56, 56, 5), 1.3, closed=True)
    m.diamond(32, 2.2, 3.6, 2.2)
    m.diamond(32, 61.8, 3.6, 2.2)
    save("hotslot_selected", m, None, [0, 0, 0, 0], [4, 4, 4, 4])
    m = Metal(60, 60, ss=4)
    m.diamond(30, 2.4, 3.8, 2.4)
    for sx in (-1, 1):
        m.line([(30 + sx * 6, 2), (30 + sx * 14, 2)], 0.8)
    save("hotslot_equipped", m, None, [0, 0, 0, 0], [2, 2, 2, 2])

    # The rule under titles and tabs (its knot is the separate tab_knot, never stretched).
    m = Metal(120, 8, ss=4)
    m.line([(2.5, 4), (117.5, 4)], 0.8)
    m.dot(2.2, 4, 1.4)
    m.dot(117.8, 4, 1.4)
    save("tab_marker", m, None, [8, 0, 8, 0])
    m = Metal(10, 14, ss=4)
    m.diamond(5, 7, 4.2, 6.2)
    save("tab_knot", m, None, [0, 0, 0, 0])

    with open(os.path.join(PIECES_DIR, "pieces.json"), "w") as f:
        json.dump({"pieces": meta, "retire": RETIRE}, f, indent=1, sort_keys=True)
    return len(meta)


if __name__ == "__main__":
    print(str(pieces()) + " metal pieces -> art/src/metal")
    out_dir = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "dist")
    os.makedirs(out_dir, exist_ok=True)
    print(window(os.path.join(out_dir, "window-metal.png")))
    print(bars(os.path.join(out_dir, "bars-burn.png")))
