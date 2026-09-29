"""Cuts GenesisUI sprites out of Diego's isolated texture sheets (AGENTS.md §2b, D-027).

    tools/.venv/bin/python tools/art/sheets.py [piece ...]      (no argument: everything)

Sources (never committed): ~/GenesisUI-Concept/GenesisUI-textures/
  UI_elements1..5-upscaled.png   isolated gold elements with alpha
  background_texture.png          the dark panel material (RGB, not seamless)
  hp/stamina/eitr_texture.png     the three liquids (RGB, static, not seamless)

Output: art/src/sheets/<name>.png at 2x design size, plus art/src/sheets/pieces.json with
each sprite's 9-slice border, content insets, wrap mode and the few layout numbers a view
needs (`inset` of an edge ornament, `gap` between hotbar cells). render.py merges them into
art/out/sprites.json; a piece here replaces a generated or concept sprite of the same name.

Rules this script keeps (AGENTS.md §2b):
- The metal is resampled, never redrawn: one uniform scale per piece, premultiplied so the
  dark outline does not bleed. Pieces of one family share a scale, so rails stay one thickness.
- Variable-length frames are 9-sliced only where the art is a plain straight rail. An ornament
  in the middle of an edge is erased from the frame (the clean rail beside it is copied over)
  and cut as its own fixed-size sprite that the view places at the edge's midpoint.
- Every frame also gets <name>_shape (its outer silhouette, same size and border): the view
  clips the panel background to it, so the background has its own opacity and never leaks
  past rounded or pointed corners. Frames with a window get <name>_opening (the liquid's or
  the map's shape).
- The liquids and the background are made seamless (cross-faded wrap) before they tile or
  scroll; the originals are not loops.
"""
import json
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageOps

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SRC = os.path.expanduser("~/GenesisUI-Concept/GenesisUI-textures")
# Every element is also written here at the sheet's full resolution, before scaling, so Diego can
# retouch it (colour, light) and hand it back: a file with the same name and size in `edited/`
# replaces the cut from the sheet. Outside the repository, like the sheets.
CUTS = os.path.expanduser("~/GenesisUI-Concept/GenesisUI-cuts")
OUT = os.path.join(ROOT, "art", "src", "sheets")
SCALE = 2                     # PNGs are written at 2x design size (render.py)
K = 226 / 712                 # design units per sheet pixel: the health frame is 226 tall (approved size)
SOLID = 150                   # alpha above which a pixel is metal, not the soft outer shadow


def sheet(n):
    path = os.path.join(SRC, f"UI_elements{n}-upscaled.png")
    if not os.path.exists(path):
        sys.exit(f"sheet {n} not found under {SRC}")
    return Image.open(path).convert("RGBA")


WINDOWS = os.path.expanduser("~/GenesisUI-Concept/windows-textures-genesisui")
KW = 0.8                      # design units per window-sheet pixel: window pieces, finer than the concept's 1.15


def window_sheet(n):
    path = os.path.join(WINDOWS, f"WindowsTextures ({n}).png")
    if not os.path.exists(path):
        sys.exit(f"window sheet {n} not found under {WINDOWS}")
    return Image.open(path).convert("RGBA")


def source_of(p):
    return window_sheet(p["win"]) if "win" in p else sheet(p["sheet"])


def texture(name):
    path = os.path.join(SRC, name)
    if not os.path.exists(path):
        sys.exit(f"{name} not found under {SRC}")
    return Image.open(path).convert("RGB")


# ---------------------------------------------------------------- masks

def outside(alpha, solid=SOLID):
    """Pixels connected to the crop's border through non-metal pixels (flood fill)."""
    h, w = alpha.shape
    m = Image.new("L", (w + 2, h + 2), 0)
    m.paste(Image.fromarray(((alpha >= solid) * 255).astype(np.uint8)), (1, 1))
    ImageDraw.floodfill(m, (0, 0), 128)
    return np.asarray(m)[1:-1, 1:-1] == 128


def region_at(alpha, seed, solid=SOLID):
    """The see-through region containing `seed` (x, y in crop pixels)."""
    h, w = alpha.shape
    m = Image.fromarray(((alpha >= solid) * 255).astype(np.uint8)).copy()
    x, y = seed
    if np.asarray(m)[y, x] != 0:
        sys.exit(f"opening seed {seed} is on metal")
    ImageDraw.floodfill(m, (x, y), 128)
    return np.asarray(m) == 128


def grow(mask, px):
    im = Image.fromarray((mask * 255).astype(np.uint8))
    for _ in range(px):
        im = im.filter(ImageFilter.MaxFilter(3))
    return np.asarray(im) > 127


def shrink(mask, px):
    im = Image.fromarray((mask * 255).astype(np.uint8))
    for _ in range(px):
        im = im.filter(ImageFilter.MinFilter(3))
    return np.asarray(im) > 127


def trim_box(alpha, pad=2, threshold=12):
    ys, xs = np.nonzero(alpha > threshold)
    h, w = alpha.shape
    return max(0, xs.min() - pad), max(0, ys.min() - pad), min(w, xs.max() + 1 + pad), min(h, ys.max() + 1 + pad)


# ---------------------------------------------------------------- colour

def defringe(a):
    """The sheets carry coloured halos (red/green) in the soft outer shadow. Below metal alpha
    the colour becomes the shadow's own dark bronze; the alpha (the shadow's shape) is kept."""
    out = a.copy()
    alpha = a[..., 3].astype(np.float32)
    t = np.clip((SOLID - alpha) / (SOLID - 40), 0, 1)[..., None]          # 1 = pure shadow
    shadow = np.array([38, 24, 8], np.float32)
    out[..., :3] = (a[..., :3].astype(np.float32) * (1 - t) + shadow * t).astype(np.uint8)
    return out


# One gold for every piece (D-029): a ramp taken from Diego's colour reference. Each piece's own
# light and shade (its luminance) is kept, re-ranked onto the reference's luminance and coloured
# by the ramp, so sheets drawn in different tones come out as one darker, discreet gold. Shape,
# alpha, symmetry and highlights are untouched: only the colour of each pixel changes.
GOLD = json.load(open(os.path.join(os.path.dirname(__file__), "gold_ramp.json")))
TONE_DARKEN = 0.62          # the reference's luminance, pulled well down: dark, discreet gold (Diego, R-046)
TONE_SATURATION = 1.0       # the ramp's own warmth: no extra colour, quieter (Diego, R-046)


def tone(a):
    ramp = np.asarray(GOLD["ramp"], np.float32)
    lum = ramp.mean(1, keepdims=True)
    ramp = np.clip(lum + (ramp - lum) * TONE_SATURATION, 0, 255)
    target = np.asarray(GOLD["quantiles"], np.float32) * TONE_DARKEN
    rgb = a[..., :3].astype(np.float32)
    l = 0.299 * rgb[..., 0] + 0.587 * rgb[..., 1] + 0.114 * rgb[..., 2]
    metal = a[..., 3] > 200
    if metal.sum() < 50:
        return a
    source = np.percentile(l[metal], np.linspace(0, 100, 101))
    source = np.maximum.accumulate(source + np.arange(101) * 1e-4)    # strictly increasing for interp
    mapped = np.interp(l, source, target)
    out = a.copy()
    out[..., :3] = ramp[np.clip(mapped, 0, 255).astype(int)].astype(np.uint8)
    return out


def resize(img, size):
    """Premultiplied resampling: transparent pixels do not darken or tint the metal's edge."""
    return img.convert("RGBa").resize(size, Image.LANCZOS).convert("RGBA")


def even(v):
    return max(2, int(round(v / 2.0)) * 2)


# ---------------------------------------------------------------- pieces

def frame(p):
    """Cut one frame: crop, erase mid-edge ornaments, clean, scale; write frame, fill, opening."""
    img = source_of(p).crop(p["box"])
    a = np.asarray(img).copy()
    trimmed = trim_box(a[..., 3])
    a = a[trimmed[1]:trimmed[3], trimmed[0]:trimmed[2]].copy()
    ox, oy = p["box"][0] + trimmed[0], p["box"][1] + trimmed[1]
    a, edited = retouched(p.get("cut", p["name"]), a)
    if not edited and p.get("tone", True):
        a = tone(a)
    original = a.copy()

    ornaments = {}
    for o in p.get("ornaments", []):
        x0, y0 = max(0, o["box"][0] - ox), max(0, o["box"][1] - oy)
        x1, y1 = min(a.shape[1], o["box"][2] - ox), min(a.shape[0], o["box"][3] - oy)
        piece = original[y0:y1, x0:x1].copy()
        ornaments[o["name"]] = (piece, o, (x0, y0, x1, y1))
        # Erase it from the frame: copy the clean rail beside it (same edge, same cross-section).
        # A thin clean slice is repeated: some edges have little plain rail beside the ornament.
        n = o.get("slice", 4)
        if o["edge"] in ("left", "right"):
            sy = o["clean"] - oy
            strip = original[sy:sy + n, x0:x1]
            a[y0:y1, x0:x1] = np.tile(strip, (-(-(y1 - y0) // n), 1, 1))[:y1 - y0]
        else:
            sx = o["clean"] - ox
            strip = original[y0:y1, sx:sx + n]
            a[y0:y1, x0:x1] = np.tile(strip, (1, -(-(x1 - x0) // n), 1))[:, :x1 - x0]

    if "clean_middle" in p:
        # A bar whose middle carries dividers, a diamond and light flares: the whole stretchable
        # middle becomes a repeat of one clean slice of rail, so 9-slicing never stretches a flare.
        x_clean, n = p["clean_middle"]
        xc = x_clean - ox
        strip = original[:, xc:xc + n]
        bl, br = p["border"][0], p["border"][2]
        span = a.shape[1] - bl - br
        a[:, bl:bl + span] = np.tile(strip, (1, -(-span // n), 1))[:, :span]
    if p.get("mirror"):
        a = a[:, ::-1].copy()
    if "recolor" in p:
        # A state of another colour (equipped = green) drawn from the same toned piece: each pixel's
        # luminance carried by the new colour, so shape and light stay the gold piece's own.
        rgb = a[..., :3].astype(np.float32)
        lum = (0.299 * rgb[..., 0] + 0.587 * rgb[..., 1] + 0.114 * rgb[..., 2])[..., None] / 255.0
        a[..., :3] = np.clip(lum * np.asarray(p["recolor"], np.float32) * 1.6, 0, 255).astype(np.uint8)

    # Re-trim after erasing (a top ornament may have been the highest pixel).
    t2 = trim_box(a[..., 3])
    a = a[t2[1]:t2[3], t2[0]:t2[2]].copy()
    ox, oy = ox + t2[0], oy + t2[1]
    if not p.get("glow"):
        a = defringe(a)

    alpha = a[..., 3]
    h, w = alpha.shape
    s = p.get("scale", KW if "win" in p else K)
    size = (even(w * s * SCALE), even(h * s * SCALE))
    dw, dh = size[0] // SCALE, size[1] // SCALE
    sx, sy = dw / w, dh / h                       # the exact design units per crop pixel

    out = {p["name"]: Image.fromarray(a, "RGBA")}
    out_meta = {}

    # The fill: inside the outer silhouette (metal and everything it encloses).
    fill = ~outside(alpha)
    fill = shrink(fill, 1)
    fm = Image.new("RGBA", (w, h), (255, 255, 255, 0))
    fm.putalpha(Image.fromarray((fill * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.8)))
    out[p["name"] + "_shape"] = fm

    content = None
    if "opening" in p:
        seed = (p["opening"][0] - ox, p["opening"][1] - oy)
        hole = region_at(alpha, seed)
        # Reach under the inner rim a little: the frame is drawn over the liquid's edge.
        hole = grow(hole, p.get("opening_grow", 3)) & fill
        om = Image.new("RGBA", (w, h), (255, 255, 255, 0))
        om.putalpha(Image.fromarray((hole * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.6)))
        out[p["name"] + "_opening"] = om
        ys, xs = np.nonzero(hole)
        content = (xs.min() * sx, (h - 1 - ys.max()) * sy, (w - 1 - xs.max()) * sx, ys.min() * sy)

    if "cells" in p:
        # The cells' own windows: content spans them, gap is the mean space between neighbours.
        boxes = holes(alpha, p["cells"])
        if len(boxes) != p["cells"]:
            sys.exit(f"{p['name']}: found {len(boxes)} cells, expected {p['cells']}")
        x0, y0 = min(b[0] for b in boxes), min(b[1] for b in boxes)
        x1, y1 = max(b[2] for b in boxes), max(b[3] for b in boxes)
        content = (x0 * sx, (h - y1) * sy, (w - x1) * sx, y0 * sy)
        gaps = [boxes[i + 1][0] - boxes[i][2] for i in range(len(boxes) - 1)]
        p = dict(p, gap=sum(gaps) / len(gaps))
        print(f"  {p['name']}: cells {[b[2] - b[0] for b in boxes]} px wide, gaps {gaps}")
    if "content" in p:                           # explicit insets in crop pixels (left, bottom, right, top)
        c = p["content"]
        content = (c[0] * sx, c[1] * sy, c[2] * sx, c[3] * sy)
    border = p.get("border", (0, 0, 0, 0))
    border = [int(round(border[0] * sx)), int(round(border[1] * sy)), int(round(border[2] * sx)), int(round(border[3] * sy))]

    for name, img in out.items():
        meta = {"border": border, "from": p["name"], "sheet": p.get("sheet", 100 + p.get("win", 0))}
        if name == p["name"] and content is not None:
            meta["content"] = [int(round(v)) for v in content]
        if name == p["name"] and "gap" in p:
            meta["gap"] = int(round(p["gap"] * sx))
        out_meta[name] = (resize(img, size), meta)

    for name, (piece, o, (x0, y0, x1, y1)) in ornaments.items():
        if not p.get("glow"):
            piece = defringe(piece)
        ow, oh = piece.shape[1], piece.shape[0]
        osize = (even(ow * sx * SCALE), even(oh * sy * SCALE))
        # The ornament's centre sits on the edge's rail: its distance from the frame's outer edge.
        cx, cy = (x0 + x1) / 2 - t2[0], (y0 + y1) / 2 - t2[1]
        inset = {"left": cx * sx, "right": (w - cx) * sx, "top": cy * sy, "bottom": (h - cy) * sy}[o["edge"]]
        out_meta[name] = (resize(Image.fromarray(piece, "RGBA"), osize),
                          {"border": [0, 0, 0, 0], "from": p["name"], "sheet": p.get("sheet", 100 + p.get("win", 0)), "inset": int(round(inset))})
    return out_meta


def retouched(name, a):
    """Exports the element as cut; returns (pixels, edited): Diego's version instead when there is one."""
    os.makedirs(os.path.join(CUTS, "original"), exist_ok=True)
    Image.fromarray(a, "RGBA").save(os.path.join(CUTS, "original", name + ".png"))
    edited = os.path.join(CUTS, "edited", name + ".png")
    if not os.path.exists(edited):
        return a, False
    e = np.asarray(Image.open(edited).convert("RGBA")).copy()
    if e.shape != a.shape:
        sys.exit(f"{edited} is {e.shape[1]}x{e.shape[0]}, the cut is {a.shape[1]}x{a.shape[0]}: keep the size")
    print(f"  {name}: using Diego's edited cut (kept as he coloured it)")
    return e, True


def holes(alpha, count, min_area=400):
    """The `count` largest see-through regions enclosed by metal (the hotbar's cells), left to right."""
    free = (alpha < SOLID) & ~outside(alpha)
    found = []
    for _ in range(400):
        ys, xs = np.nonzero(free)
        if len(xs) == 0:
            break
        region = region_at(alpha, (xs[0], ys[0]))
        free &= ~region
        if region.sum() >= min_area:
            ry, rx = np.nonzero(region)
            found.append((region.sum(), (rx.min(), ry.min(), rx.max() + 1, ry.max() + 1)))
    found.sort(key=lambda f: -f[0])
    return sorted((b for _, b in found[:count]), key=lambda b: b[0])


def ring(p):
    """A round frame at any diameter from a small one: every pixel samples the source ring at
    the same angle and the same distance from the rail (so the rail keeps its thickness, bevel
    and light direction); the four cardinal diamonds are cut whole and put back at scale."""
    cut, edited = retouched(p["name"], np.asarray(sheet(p["sheet"]).crop(p["box"])).copy())
    src = Image.fromarray(cut if edited else tone(cut), "RGBA")
    a = np.asarray(src).astype(np.float32)
    cx, cy, r0 = p["centre"][0] - p["box"][0], p["centre"][1] - p["box"][1], p["radius"]
    s = p.get("scale", K) * SCALE                  # output px per sheet px (rail thickness)
    R = p["diameter"] * SCALE / 2                  # rail centre radius in output px
    half = p["diameter"] * SCALE // 2 + int(p["pad"] * SCALE)
    size = 2 * half
    yy, xx = np.mgrid[0:size, 0:size].astype(np.float32) + 0.5
    dx, dy = xx - half, yy - half
    d = np.hypot(dx, dy)
    ang = np.arctan2(dy, dx)
    # Diamonds sit at 0/90/180/270 degrees: there, sample the plain rail just beside them.
    theta = np.degrees(ang) % 360
    phi = np.clip(theta % 90, p["guard"], 90 - p["guard"])
    ang2 = np.radians(np.floor(theta / 90) * 90 + phi)
    rs = r0 + (d - R) / s
    sx = cx + rs * np.cos(ang2)
    sy = cy + rs * np.sin(ang2)
    out = bilinear(a, sx, sy)
    # Only the rail's own band: beyond it the mapping would reach the far side of the source.
    out[..., 3] *= np.clip((p["band"] - np.abs(rs - r0)) / 2.0, 0, 1)
    img = Image.fromarray(np.clip(out, 0, 255).astype(np.uint8), "RGBA")

    dsz = p["diamond"]                              # half size of the diamond's crop (sheet px)
    for k in range(4):
        th = np.radians(90 * k)
        px, py = cx + r0 * np.cos(th), cy + r0 * np.sin(th)
        crop = src.crop((int(px - dsz), int(py - dsz), int(px + dsz), int(py + dsz)))
        n = even(2 * dsz * s)
        crop = resize(crop, (n, n))
        qx, qy = half + R * np.cos(th), half + R * np.sin(th)
        img.alpha_composite(crop, (int(round(qx - n / 2)), int(round(qy - n / 2))))
    a2 = defringe(np.asarray(img).copy())
    img = Image.fromarray(a2, "RGBA")

    inner = R - p["window"] * s                     # the window's radius in output px
    om = Image.new("L", (size, size), 0)
    ImageDraw.Draw(om).ellipse((half - inner, half - inner, half + inner, half + inner), fill=255)
    opening = Image.new("RGBA", (size, size), (255, 255, 255, 0))
    opening.putalpha(om)
    meta = {"border": [0, 0, 0, 0], "from": p["name"], "sheet": p["sheet"]}
    window = int(round(inner * 2 / SCALE))
    pad = (size // SCALE - window) // 2
    return {p["name"]: (img, dict(meta, content=[pad, pad, pad, pad])), p["name"] + "_opening": (opening, meta)}


def bilinear(a, x, y):
    h, w = a.shape[:2]
    x = np.clip(x, 0, w - 1.001)
    y = np.clip(y, 0, h - 1.001)
    x0, y0 = np.floor(x).astype(int), np.floor(y).astype(int)
    fx, fy = (x - x0)[..., None], (y - y0)[..., None]
    pm = a.copy()
    pm[..., :3] *= pm[..., 3:4] / 255.0                 # premultiplied
    v = (pm[y0, x0] * (1 - fx) * (1 - fy) + pm[y0, x0 + 1] * fx * (1 - fy)
         + pm[y0 + 1, x0] * (1 - fx) * fy + pm[y0 + 1, x0 + 1] * fx * fy)
    al = np.maximum(v[..., 3:4], 1e-3)
    v[..., :3] = np.where(al > 0.5, v[..., :3] * 255.0 / al, 0)
    return v


def glow(p):
    """The selected cell's light: a soft gold ring inside the cell, from the sheet's lit slot."""
    w, h = p["size"]
    W, H = w * SCALE, h * SCALE
    m = Image.new("L", (W, H), 0)
    r = p["radius"] * SCALE
    ImageDraw.Draw(m).rounded_rectangle((4, 4, W - 5, H - 5), radius=r, outline=255, width=3 * SCALE)
    m = m.filter(ImageFilter.GaussianBlur(2.5 * SCALE))
    inner = Image.new("L", (W, H), 0)
    ImageDraw.Draw(inner).rounded_rectangle((6, 6, W - 7, H - 7), radius=r, fill=70)
    inner = inner.filter(ImageFilter.GaussianBlur(8 * SCALE))
    alpha = ImageOps_add(m, inner)
    img = Image.new("RGBA", (W, H), tuple(p["rgb"]) + (0,))
    img.putalpha(alpha)
    return {p["name"]: (img, {"border": [0, 0, 0, 0], "from": p["name"], "sheet": 0})}


def ImageOps_add(a, b):
    return Image.fromarray(np.clip(np.asarray(a, np.int32) + np.asarray(b, np.int32), 0, 255).astype(np.uint8))


# ---------------------------------------------------------------- materials

def seamless_x(a, overlap):
    """Wraps horizontally: the last `overlap` columns fade into the first ones."""
    w = a.shape[1]
    n = int(w * overlap)
    t = np.linspace(0, 1, n, dtype=np.float32)[None, :, None]
    t = t * t * (3 - 2 * t)                                    # smoothstep: no visible band edge
    body = a[:, :w - n].astype(np.float32).copy()
    body[:, :n] = a[:, w - n:].astype(np.float32) * (1 - t) + a[:, :n].astype(np.float32) * t
    return body


def seamless_xy(a, overlap):
    b = seamless_x(a, overlap)
    return np.transpose(seamless_x(np.transpose(b, (1, 0, 2)), overlap), (1, 0, 2))


def liquid(p):
    """A bar's liquid: the calm middle band of Diego's texture, looped along its length, in two
    orientations (vertical bars scroll up, horizontal bars sideways)."""
    img = texture(p["file"])
    w, h = img.size
    y0, y1 = int(h * p["band"][0]), int(h * p["band"][1])
    a = np.asarray(img)[y0:y1].astype(np.float32)
    a = seamless_x(a, 0.22)
    loop = Image.fromarray(np.clip(a, 0, 255).astype(np.uint8), "RGB").convert("RGBA")
    across = p["across"] * SCALE                                   # pixels across the bar
    along = even(across * loop.width / loop.height)
    horizontal = loop.resize((along, across), Image.LANCZOS)
    vertical = horizontal.rotate(90, expand=True)                  # the loop runs bottom to top
    meta = {"border": [0, 0, 0, 0], "from": p["name"], "sheet": 0, "wrap": "repeat"}
    return {p["name"]: (vertical, meta), p["name"] + "_h": (horizontal, meta)}


def background(p):
    img = texture(p["file"])
    x0, y0, x1, y1 = p["box"]
    a = np.asarray(img.crop((x0, y0, x1, y1))).astype(np.float32)
    a = seamless_xy(a, 0.2)
    tile = Image.fromarray(np.clip(a, 0, 255).astype(np.uint8), "RGB").convert("RGBA")
    n = p["size"] * SCALE
    tile = tile.resize((n, n), Image.LANCZOS)
    return {p["name"]: (tile, {"border": [0, 0, 0, 0], "from": p["name"], "sheet": 0, "wrap": "repeat"})}


# ---------------------------------------------------------------- recipes
# Boxes are in sheet pixels (1448x1086 sheets). `border` and `content` are crop pixels of the
# trimmed piece (left, bottom, right, top). `opening` is a point inside the window. Ornament
# `clean` is where the same edge is plain rail (a y for left/right edges, an x for top/bottom).

PIECES = [
    # Vital bars (sheet 3): three sizes as Diego drew them, one scale, no slicing.
    {"name": "vital_health", "kind": "frame", "sheet": 3, "box": (36, 104, 184, 824), "opening": (110, 460)},
    {"name": "vital_stamina", "kind": "frame", "sheet": 3, "box": (200, 152, 332, 820), "opening": (266, 480)},
    # Eitr uses the stamina frame: only health is bigger (Diego, R-042).
    # The stamina readout (sheet 3, first bar, Diego R-042): knot ends, one channel for the liquid.
    {"name": "sprint_frame", "kind": "frame", "sheet": 3, "box": (488, 172, 1424, 300), "opening": (956, 236)},
    # The hotbar (sheet 4, top): eight cells in one fixed piece; `content` spans the cells.
    {"name": "hotbar_frame", "kind": "frame", "sheet": 4, "box": (12, 72, 1444, 280), "scale": 0.4, "cells": 8},
    # Cell states (sheet 4, second row): lit gold = selected (and hover, later), green = equipped.
    # Drawn over a cell, their window lined up with the cell's; the glow is kept as drawn.
    {"name": "slot_selected", "kind": "frame", "sheet": 4, "box": (404, 320, 720, 620), "scale": 0.4,
     "opening": (562, 470), "opening_grow": 0, "glow": True},
    {"name": "slot_equipped", "kind": "frame", "sheet": 4, "box": (748, 320, 1068, 620), "scale": 0.4,
     "opening": (908, 470), "opening_grow": 0, "glow": True, "tone": False},
    # Food slot (sheet 2) and status tile (sheet 5): square frames with four diamonds.
    {"name": "slot", "kind": "frame", "sheet": 2, "box": (256, 600, 480, 820), "scale": 0.29,
     "border": (46, 46, 46, 46), "content": (26, 26, 26, 26)},
    {"name": "tile", "kind": "frame", "sheet": 5, "box": (1192, 252, 1424, 476), "scale": 0.29,
     "border": (46, 46, 46, 46), "content": (30, 30, 30, 30)},
    # Hover card (sheet 5, bottom left): knot corners; the side diamonds become ornaments.
    {"name": "card", "kind": "frame", "sheet": 5, "box": (32, 752, 612, 1048),
     "border": (86, 86, 86, 86), "content": (26, 22, 26, 22),
     "ornaments": [{"name": "card_knot_left", "edge": "left", "box": (30, 876, 80, 922), "clean": 862},
                   {"name": "card_knot_right", "edge": "right", "box": (564, 876, 614, 922), "clean": 862}]},
    # Notice plate (sheet 4, third row, first): a slim plate with a diamond at its left end.
    {"name": "notice_plate", "kind": "frame", "sheet": 4, "box": (36, 664, 264, 784),
     "border": (70, 40, 44, 40), "content": (40, 22, 26, 22)},
    # Minimap: the crest (day/time, diamond on top) and the biome banner (diamonds at the ends).
    {"name": "map_crest", "kind": "frame", "sheet": 4, "box": (264, 656, 488, 784),
     "border": (40, 30, 40, 50), "content": (26, 20, 26, 34),
     "ornaments": [{"name": "map_crest_knot", "edge": "top", "box": (352, 656, 402, 716), "clean": 300}]},
    {"name": "map_banner", "kind": "frame", "sheet": 4, "box": (490, 664, 730, 784),
     "border": (60, 36, 60, 36), "content": (40, 20, 40, 20)},
    # Boss plate (sheet 3, second bar): knot ends. (The creature plate stays the generated one, R-042.)
    {"name": "boss_plate", "kind": "frame", "sheet": 3, "box": (476, 384, 1428, 560), "scale": 0.44,
     "border": (116, 40, 116, 40), "content": (100, 30, 100, 30)},
    # The minimap ring, rebuilt at 250 from the small ring of sheet 2.
    {"name": "map_ring", "kind": "ring", "sheet": 2, "box": (488, 604, 708, 820), "centre": (598, 712), "scale": 0.5,
     "radius": 92, "diameter": 226, "pad": 12, "guard": 16, "diamond": 20, "window": 9, "band": 16},
    {"name": "cell_glow", "kind": "glow", "size": (60, 56), "radius": 8, "rgb": (255, 214, 120)},
    # ---- Window shell (F4.1), from the window sheets (win = WindowsTextures (n)); finest pieces.
    # Top bar and key-hint bar (sheet 1): end caps kept, the middle rebuilt from clean rail.
    {"name": "window_topbar", "kind": "frame", "win": 1, "box": (30, 26, 1642, 134),
     "border": (190, 20, 190, 20), "content": (190, 16, 190, 16), "clean_middle": (740, 6)},
    {"name": "window_hintbar", "kind": "frame", "win": 1, "box": (32, 838, 1642, 920),
     "border": (90, 18, 90, 18), "content": (80, 12, 80, 12), "clean_middle": (400, 6)},
    # A divider of the top bar, a thin marker line (selected tab), blank key caps (sheet 2).
    {"name": "window_divider", "kind": "frame", "win": 1, "box": (466, 46, 494, 108)},
    {"name": "tab_marker", "kind": "frame", "win": 1, "box": (924, 644, 1360, 678),
     "border": (40, 0, 40, 0)},
    {"name": "keycap", "kind": "frame", "win": 2, "box": (262, 678, 334, 750),
     "border": (14, 14, 14, 14), "content": (10, 10, 10, 10), "tone": False},
    {"name": "keycap_wide", "kind": "frame", "win": 2, "box": (412, 678, 556, 750),
     "border": (22, 14, 22, 14), "content": (14, 10, 14, 10), "tone": False},
    # The hotbar and food slots (window sheet 3): single thin slots instead of the heavy 8-cell plate
    # (Diego, R-046: "grosseira"). Selected = the lit thin slot; equipped = the same, in green.
    {"name": "hotslot", "kind": "frame", "win": 3, "box": (28, 28, 196, 198), "scale": 0.35,
     "border": (30, 30, 30, 30), "content": (14, 14, 14, 14)},
    {"name": "hotslot_selected", "kind": "frame", "win": 3, "box": (206, 24, 386, 200), "scale": 0.35,
     "opening": (296, 112), "opening_grow": 0, "glow": True},
    {"name": "hotslot_equipped", "kind": "frame", "win": 3, "box": (206, 24, 386, 200), "scale": 0.35,
     "opening": (296, 112), "opening_grow": 0, "glow": True, "recolor": (90, 200, 70), "cut": "hotslot_selected"},
    # Tab icons and mouse hints (sheet 7).
    {"name": "icon_logo", "kind": "frame", "win": 7, "box": (28, 8, 194, 160), "scale": 0.3},
    {"name": "icon_inventory", "kind": "frame", "win": 7, "box": (236, 26, 336, 140), "scale": 0.3},
    {"name": "icon_skills", "kind": "frame", "win": 7, "box": (378, 16, 494, 140), "scale": 0.3},
    {"name": "icon_map", "kind": "frame", "win": 7, "box": (520, 8, 656, 144), "scale": 0.3},
    {"name": "icon_crafting", "kind": "frame", "win": 7, "box": (678, 20, 796, 138), "scale": 0.3},
    {"name": "icon_achievements", "kind": "frame", "win": 7, "box": (816, 24, 928, 136), "scale": 0.3},
    {"name": "icon_settings", "kind": "frame", "win": 7, "box": (956, 20, 1066, 132), "scale": 0.3},
    {"name": "icon_mouse_left", "kind": "frame", "win": 7, "box": (1274, 150, 1340, 248), "scale": 0.3},
    {"name": "icon_mouse_right", "kind": "frame", "win": 7, "box": (1274, 150, 1340, 248), "scale": 0.3,
     "mirror": True, "cut": "icon_mouse_left"},
    # Materials.
    {"name": "liquid_health", "kind": "liquid", "file": "hp_texture.png", "band": (0.12, 0.86), "across": 96},
    {"name": "liquid_stamina", "kind": "liquid", "file": "stamina_texture.png", "band": (0.08, 0.8), "across": 96},
    {"name": "liquid_eitr", "kind": "liquid", "file": "eitr_texture.png", "band": (0.1, 0.88), "across": 96},
    {"name": "panel_bg", "kind": "background", "file": "background_texture.png", "box": (360, 120, 1260, 900), "size": 192},
]

KINDS = {"frame": frame, "ring": ring, "glow": glow, "liquid": liquid, "background": background}


def main(names):
    os.makedirs(OUT, exist_ok=True)
    manifest_path = os.path.join(OUT, "pieces.json")
    pieces = json.load(open(manifest_path)) if os.path.exists(manifest_path) and names else {}
    for p in PIECES:
        if names and p["name"] not in names:
            continue
        for name, (img, meta) in KINDS[p["kind"]](p).items():
            if img.width % 2 or img.height % 2:
                img = img.resize((img.width - img.width % 2, img.height - img.height % 2), Image.LANCZOS)
            img.save(os.path.join(OUT, name + ".png"), optimize=True)
            pieces[name] = meta
            print(f"{name}: {img.width // SCALE}x{img.height // SCALE} design, border {meta['border']}"
                  + (f", content {meta['content']}" if "content" in meta else "")
                  + (f", inset {meta['inset']}" if "inset" in meta else ""))
    with open(manifest_path, "w") as f:
        json.dump(pieces, f, indent=1, sort_keys=True)


if __name__ == "__main__":
    main(sys.argv[1:])
