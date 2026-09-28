"""Cuts UI pieces out of Diego's concept images (D-026), cleans them and writes sprites.

    tools/.venv/bin/python tools/art/extract.py [piece ...]     (no argument: every piece)

Sources: ~/GenesisUI-Concept/4k/ConceptArt (N).png (made by upscale.py), never committed.
Output: art/src/concept/<piece>.png (the cleaned piece at 4K scale, committed) and the entry
render.py merges into art/out/sprites.json. Coordinates in PIECES are in the ORIGINAL
1672x941 concept pixels, so they can be read off the concept directly; the script scales them.

One recipe (`panel`): the piece's `outline` (shapes in 1x coordinates; default the crop box)
becomes its alpha, so the scene around it disappears; the gold ornament that grows from the
outline's edge (within `reach`) is kept; everything else inside is refilled with the panel's
own dark tone sampled from `patch` (icons, texts and bars of the concept disappear); `holes`
(the bar channel, the map window) become transparent and are also written as
<name>_opening, the shape the liquid or the map is clipped by.
"""
import json
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
CONCEPT = os.path.expanduser("~/GenesisUI-Concept")
BASE_W = 1672
OUT = os.path.join(ROOT, "art", "src", "concept")


def source(n):
    for path in (os.path.join(CONCEPT, "4k", f"ConceptArt ({n}).png"), os.path.join(CONCEPT, f"ConceptArt ({n}).png")):
        if os.path.exists(path):
            im = Image.open(path).convert("RGBA")
            return im, im.width / BASE_W
    sys.exit(f"concept {n} not found under {CONCEPT}")


def grow(mask, within, steps=10000):
    """Morphological reconstruction: grow `mask` through `within` until stable."""
    cur = mask & within
    for _ in range(steps):
        nxt = (np.asarray(Image.fromarray((cur * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(3))) > 0) & within
        if (nxt == cur).all():
            break
        cur = nxt
    return cur


def gold_mask(a, bright=48):
    """Warm, bright pixels: the gold ornament. `bright` rises for pieces over warm, dark scenes."""
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    return (r > 55) & (r > b * 1.3) & (g > 35) & ((r + g) / 2 > bright)


def dark_fill(a, patch_box, seed):
    """The panel's own dark tone with soft low-frequency variation."""
    h, w = a.shape[:2]
    x0, y0, x1, y1 = patch_box
    base = a[y0:y1, x0:x1, :3].reshape(-1, 3).mean(0)
    rng = np.random.default_rng(seed)
    cells = max(4, w // 24)
    noise = np.asarray(Image.fromarray((rng.random((h * cells // w + 2, cells + 2)) * 255).astype(np.uint8)).resize((w, h), Image.BICUBIC), np.float32) / 255
    return np.clip(base[None, None, :] * (0.9 + 0.2 * noise[..., None]), 0, 255)


def silhouette(size, p, s, origin):
    """The piece's outline: `outline` shapes (1x coordinates), or the whole crop box."""
    if "outline" in p:
        return polygon_mask(size, p["outline"], s, origin)
    return Image.new("L", size, 255)


def polygon_mask(size, shapes, s, origin):
    m = Image.new("L", size, 0)
    d = ImageDraw.Draw(m)
    ox, oy = origin
    for kind, pts in shapes:
        if kind == "poly":
            d.polygon([((x * s) - ox, (y * s) - oy) for x, y in pts], fill=255)
        elif kind == "ellipse":
            cx, cy, rx, ry = pts
            d.ellipse(((cx - rx) * s - ox, (cy - ry) * s - oy, (cx + rx) * s - ox, (cy + ry) * s - oy), fill=255)
        elif kind == "rect":
            x0, y0, x1, y1 = pts
            d.rectangle((x0 * s - ox, y0 * s - oy, x1 * s - ox, y1 * s - oy), fill=255)
        elif kind == "rrect":
            x0, y0, x1, y1, r = pts
            d.rounded_rectangle((x0 * s - ox, y0 * s - oy, x1 * s - ox, y1 * s - oy), radius=r * s, fill=255)
    return m


def inset_distance(mask, limit):
    """Distance (in pixels, capped at `limit`) of every pixel inside `mask` to its edge."""
    dist = np.zeros(mask.size[::-1], np.int32)
    # Pad with an empty ring: PIL's MinFilter repeats edge pixels, so without it the crop's
    # own border would never count as an edge.
    w, h = mask.size
    cur = Image.new("L", (w + 2, h + 2), 0)
    cur.paste(mask, (1, 1))
    for i in range(1, limit + 1):
        cur = cur.filter(ImageFilter.MinFilter(3))
        inside = np.asarray(cur)[1:-1, 1:-1] > 127
        dist[inside] = i
        if not inside.any():
            break
    return dist


def panel(im, s, p):
    """One recipe for every piece: cut the outline, keep the gold ornament that grows from the
    outline's edge (within `reach`), refill the rest of the inside with the panel's own dark
    tone, and make `holes` transparent (written as <name>_opening: the window's shape)."""
    x0, y0, x1, y1 = (int(round(v * s)) for v in p["box"])
    crop = im.crop((x0, y0, x1, y1))
    a = np.asarray(crop).astype(np.float32)
    h, w = a.shape[:2]
    outline = silhouette(crop.size, p, s, (x0, y0))
    holes = polygon_mask(crop.size, p["holes"], s, (x0, y0)) if "holes" in p else None
    # The edge the ornament grows from: the outline's edge and, when there is a window, its rim.
    region = outline if holes is None else Image.fromarray(np.minimum(np.asarray(outline), 255 - np.asarray(holes)))
    reach = int(p.get("reach", 40) * s)
    dist = inset_distance(region, reach + 2)
    inside = np.asarray(region) > 127
    gold = np.asarray(Image.fromarray((gold_mask(a, p.get("gold_bright", 48)) * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(3))) > 0
    keep = grow(gold & inside & (dist <= max(2, int(4 * s))), gold & inside) & (dist <= reach)
    if p.get("keep_all"):
        keep = inside                                  # a piece with nothing to remove
    if "keep_boxes" in p:                              # ornaments kept whole (the plate's end knots)
        kb = np.asarray(polygon_mask(crop.size, [("rect", b) for b in p["keep_boxes"]], s, (x0, y0))) > 127
        keep = keep | (gold & inside & kb)
    if "clear_boxes" in p:                             # leftovers of the concept's content (icons)
        cb = np.asarray(polygon_mask(crop.size, [("rect", b) for b in p["clear_boxes"]], s, (x0, y0))) > 127
        keep = keep & ~cb
    if "rim" in p:                                     # a thin rim of any colour, along the outline only
        rim = max(1, int(p["rim"] * s))
        odist = dist if holes is None else inset_distance(outline, rim + 2)
        keep = keep | (inside & (odist <= rim))
    keep = np.asarray(Image.fromarray((keep * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(3))) > 0
    px0, py0, px1, py1 = (int(round(v * s)) for v in p["patch"]) if "patch" in p else (x0, y0, x1, y1)
    fill = dark_fill(a, (px0 - x0, py0 - y0, px1 - x0, py1 - y0), 3) if "patch" in p else a[..., :3]
    if "fill_rgb" in p:                                # panels the concept draws see-through
        fill = dark_fill(np.full_like(a, 0) + np.array(list(p["fill_rgb"]) + [255], np.float32), (0, 0, 4, 4), 3)
    soft = np.asarray(Image.fromarray((keep * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.6 * s)), np.float32)[..., None] / 255
    rgb = fill * (1 - soft) + a[..., :3] * soft
    alpha = np.asarray(region.filter(ImageFilter.GaussianBlur(0.5 * s)), np.float32)
    out = {p["name"]: Image.fromarray(np.dstack([rgb, alpha]).astype(np.uint8), "RGBA")}
    if holes is not None:
        opening = Image.new("RGBA", crop.size, (255, 255, 255, 0))
        opening.putalpha(holes)
        out[p["name"] + "_opening"] = opening
    return out


def ring(im, s, p):
    """A round frame whose top is hidden in the concept (the biome pill covers it): rebuild it
    from the clean eastern 90-degree sector rotated four times, add the `knot` ornament (gold
    only, the scene between its strokes stays transparent), and write the window as the
    opening (the minimap's circular mask). `pad` (1x) leaves room for the knot."""
    cx, cy, r_out, r_in = p["circle"]
    pad = int(round(p.get("pad", 0) * s))
    R = int(round((r_out + 2) * s))
    size = 2 * (R + pad)
    c = R + pad                                            # centre in canvas pixels
    ox, oy = int(round(cx * s)) - c, int(round(cy * s)) - c
    src = im.crop((ox, oy, ox + size, oy + size))
    yy, xx = np.mgrid[0:size, 0:size].astype(np.float32)
    dx, dy = xx - c + 0.5, yy - c + 0.5
    rad = np.hypot(dx, dy)
    ang = np.degrees(np.arctan2(dy, dx))
    band = (rad <= r_out * s) & (rad >= r_in * s)
    sector = np.asarray(src).copy()
    sector[..., 3] = np.where(band & (np.abs(ang) <= 45), 255, 0)
    sector = Image.fromarray(sector, "RGBA")
    out = Image.new("RGBA", (size, size))
    for k in range(4):
        out.alpha_composite(sector.rotate(90 * k, resample=Image.BICUBIC, center=(c, c)))
    soft = np.asarray(Image.fromarray((band * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.6 * s)))
    ra = np.asarray(out).copy()
    ra[..., 3] = np.minimum(ra[..., 3], soft)
    out = Image.fromarray(ra, "RGBA")
    a = np.asarray(src).astype(np.float32)
    kpoly = np.asarray(polygon_mask((size, size), p["knot"], s, (ox, oy))) > 127
    g = gold_mask(a) & kpoly & (rad >= r_in * s)          # never the map inside
    ka = np.asarray(Image.fromarray((g * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(3)).filter(ImageFilter.GaussianBlur(0.5 * s)))
    knot = np.asarray(src).copy()
    knot[..., 3] = ka
    out.alpha_composite(Image.fromarray(knot, "RGBA"))
    om = Image.new("L", (size, size), 0)
    ImageDraw.Draw(om).ellipse((c - r_in * s, c - r_in * s, c + r_in * s, c + r_in * s), fill=255)
    opening = Image.new("RGBA", (size, size), (255, 255, 255, 0))
    opening.putalpha(om)
    return {p["name"]: out, p["name"] + "_opening": opening}


# name, source concept, recipe, 1x box, recipe parameters, 9-slice border and content insets
# (design units = 1x concept pixels).
PIECES = [
    # The interaction card (ConceptArt 8): notices and hover share it.
    {"name": "card", "concept": 8, "box": (991, 279, 1349, 513), "patch": (1175, 394, 1315, 432),
     "outline": [("rect", (991, 279, 1349, 512))], "reach": 40, "border": 46, "content": 16},
    # The minimap pills (biome above; wind and day/time below).
    {"name": "pill", "concept": 8, "box": (79, 305, 259, 350), "outline": [("rrect", (80, 306, 258, 349, 21))],
     "patch": (220, 313, 243, 340), "reach": 10, "border": 22, "content": 14},
    # Hotbar: the plate (its own slots removed), a slot and the selected slot (ConceptArt 8).
    {"name": "plate", "concept": 8, "box": (484, 795, 1255, 903), "outline": [("rect", (486, 797.5, 1253, 896))],
     "fill_rgb": (17, 15, 13), "reach": 8, "gold_bright": 90,
     "keep_boxes": [(486, 797, 526, 896), (1213, 797, 1253, 896)], "border": (46, 14, 46, 14), "content": (40, 12, 40, 12)},
    {"name": "slot", "concept": 8, "box": (620, 803, 703, 892), "outline": [("rrect", (623, 806, 700, 889, 6))],
     "fill_rgb": (17, 15, 13), "reach": 4, "rim": 3, "clear_boxes": [(626, 876, 698, 888)], "border": 14, "content": 6},
    {"name": "slot_active", "concept": 8, "box": (524, 801, 619, 894), "outline": [("rrect", (527, 804, 616, 891, 8))],
     "fill_rgb": (17, 15, 13), "reach": 7, "rim": 5, "border": 16, "content": 8},
    # Vital bar frame (ConceptArt 4, the stamina one: the three bars share it); the channel is
    # the liquid's shape, the running icon in the cap is removed.
    {"name": "bar_frame", "concept": 4, "box": (104, 554, 157, 807),
     "outline": [("poly", ((130.5, 555.3), (155.3, 573.2), (155.3, 790.5), (130.5, 805.3), (105.7, 790.5), (105.7, 573.2)))],
     "holes": [("poly", ((115, 587), (146, 587), (146, 772), (140, 772), (130.5, 766), (121, 772), (115, 772)))],
     "fill_rgb": (12, 10, 9), "reach": 5, "rim": 3.2, "keep_boxes": [(110, 764, 151, 806)], "clear_boxes": [(118, 563, 143, 584)],
     "border": (11, 35, 11, 33), "content": (11, 35, 11, 33)},
    # Stamina readout above the hotbar (ConceptArt 5): knot ends, a rounded channel.
    {"name": "sprint_frame", "concept": 5, "box": (594, 713, 1083, 766),
     "outline": [("poly", ((596.5, 741.5), (621, 716.5), (646, 741.5), (621, 765))),
                 ("poly", ((1031, 741.5), (1056, 716.5), (1080.5, 741.5), (1056, 765))),
                 ("rect", (640, 726, 1037, 757.5))],
     "holes": [("rrect", (645, 735.8, 1032, 749.4, 6.5))], "fill_rgb": (12, 10, 9), "reach": 5, "gold_bright": 95,
     "keep_boxes": [(596, 715, 648, 762), (1029, 715, 1081, 762)],
     "border": (52, 13, 52, 23), "content": (52, 13, 52, 23)},
    # Boss plate (ConceptArt 6): pointed ends with knots; title, stars and bar removed.
    {"name": "boss_plate", "concept": 6, "box": (537, 27, 1153, 109),
     "outline": [("poly", ((539.5, 67), (563, 29.3), (1123, 29.3), (1150.5, 68.4), (1116, 107), (563, 107)))],
     "fill_rgb": (14, 13, 12), "reach": 6, "gold_bright": 80,
     "keep_boxes": [(539, 29, 592, 107), (1105, 29, 1158, 107)], "clear_boxes": [(563, 78, 600, 104), (1040, 50, 1136, 104)],
     "border": (52, 18, 52, 18), "content": (44, 12, 44, 12)},
    # The minimap ring with its knot; the window is the map's round mask.
    {"name": "map_ring", "concept": 8, "recipe": "ring", "circle": (173.5, 170.5, 137.5, 133),
     "knot": [("poly", ((14, 121), (58, 121), (58, 224), (14, 224)))], "pad": 28},
]


UNIT = 1920 / BASE_W          # design units (1920-wide UI) per 1x concept pixel


def design(v):
    return int(round(v * UNIT))


def main(names):
    os.makedirs(OUT, exist_ok=True)
    manifest_path = os.path.join(OUT, "pieces.json")
    pieces = json.load(open(manifest_path)) if os.path.exists(manifest_path) else {}
    chosen = [p for p in PIECES if not names or p["name"] in names]
    for p in chosen:
        im, s = source(p["concept"])
        made = ring(im, s, p) if p.get("recipe") == "ring" else panel(im, s, p)
        for name, img in made.items():
            # PNGs are exactly 2x their design size (render.py's scale), so keep them even.
            w, h = img.size
            img = img.resize((w - w % 2, h - h % 2), Image.LANCZOS) if (w % 2 or h % 2) else img
            img.save(os.path.join(OUT, name + ".png"))
            b = p.get("border", 0)
            entry = {"from": p["name"], "concept": p["concept"], "border": [design(b)] * 4 if isinstance(b, (int, float)) else [design(v) for v in b]}
            if "content" in p and not name.endswith("_opening"):
                c = p["content"]
                entry["content"] = [design(c)] * 4 if isinstance(c, (int, float)) else [design(v) for v in c]
            pieces[name] = entry
            print(f"{name}: {img.size[0]}x{img.size[1]} from concept {p['concept']}")
    with open(manifest_path, "w") as f:
        json.dump(pieces, f, indent=1, sort_keys=True)


if __name__ == "__main__":
    main(sys.argv[1:])
