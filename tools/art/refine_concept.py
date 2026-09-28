"""Build review candidates from the concept cutouts without touching shipped sprites.

    tools/.venv/bin/python tools/art/refine_concept.py

The cutouts preserve the concept's knots, but a colour threshold alone also preserves
parts of the photographed world. This pass uses one good cap/corner per symmetric part,
keeps the varying highlights of its metal, and draws the straight runs and circular ring
geometrically. Panel bodies are translucent and clipped to their real silhouettes.
The selected slot keeps its original metallic edge without mirroring; the minimap's
single left knot is deliberately asymmetric. Output is in dist/art-review/.
"""
import base64
import html
from pathlib import Path

import numpy as np
from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont, ImageOps


ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "art/src/concept"
OUTPUT = ROOT / "dist/art-review"
BODY = (12, 11, 9)


def load(name):
    return Image.open(SOURCE / f"{name}.png").convert("RGBA")


def metal_layer(im, floor=27, range_width=75):
    """Extract warm linework, retain its changing highlights, discard its dark backdrop."""
    a = np.asarray(im, np.float32)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    value = .45 * r + .45 * g + .10 * b
    warm = np.clip((r - b - 8) / 30, 0, 1)
    strength = np.clip((value - floor) / range_width, 0, 1) * warm * (a[..., 3] / 255)
    t = np.clip((value - 30) / 180, 0, 1)
    bronze = np.array((92, 49, 17), np.float32)
    gold = np.array((222, 144, 42), np.float32)
    glint = np.array((255, 238, 164), np.float32)
    low = np.clip(t / .68, 0, 1)[..., None]
    high = np.clip((t - .68) / .32, 0, 1)[..., None]
    palette = (bronze * (1 - low) + gold * low) * (1 - high) + glint * high
    # Keep the source's metal variations; only pull its warm colours toward one family.
    rgb = .30 * a[..., :3] + .70 * palette
    rgba = np.dstack((rgb, np.clip(strength ** .7 * 255, 0, 255)))
    return Image.fromarray(rgba.astype("uint8"), "RGBA")


def composite_metal(canvas, metal, xy, glow=.28):
    if glow:
        halo = Image.new("RGBA", metal.size, (255, 157, 39, 0))
        halo.putalpha(metal.getchannel("A").filter(ImageFilter.GaussianBlur(3)).point(lambda a: round(a * glow)))
        canvas.alpha_composite(halo, xy)
    canvas.alpha_composite(metal, xy)


def mask_shape(size, kind, box, radius=0, opacity=175):
    """Antialiased body. Outside the actual rounded or pointed silhouette is alpha zero."""
    scale = 3
    w, h = size
    mask = Image.new("L", (w * scale, h * scale))
    d = ImageDraw.Draw(mask)
    if kind == "round":
        x0, y0, x1, y1 = box
        d.rounded_rectangle((x0 * scale, y0 * scale, x1 * scale, y1 * scale),
                            radius=radius * scale, fill=opacity)
    elif kind == "poly":
        d.polygon([(x * scale, y * scale) for x, y in box], fill=opacity)
    return mask.resize(size, Image.Resampling.LANCZOS)


def base(size, alpha):
    result = Image.new("RGBA", size, (*BODY, 0))
    result.putalpha(alpha)
    return result


def mirrored_alpha(im, side="left"):
    alpha = im.getchannel("A")
    w, h = im.size
    half = alpha.crop((0, 0, w // 2, h)) if side == "left" else alpha.crop((w - w // 2, 0, w, h))
    out = Image.new("L", (w, h))
    if side == "left":
        out.paste(half, (0, 0))
        out.paste(ImageOps.mirror(half), (w - half.width, 0))
    else:
        out.paste(ImageOps.mirror(half), (0, 0))
        out.paste(half, (w - half.width, 0))
    return out


def metal_run(canvas, start, end, thickness=4):
    """Bronze shadow, gold body and thin cream reflection: a metal rail, not a flat line."""
    overlay = Image.new("RGBA", canvas.size)
    d = ImageDraw.Draw(overlay)
    d.line((start, end), fill=(107, 53, 17, 125), width=thickness + 5)
    d.line((start, end), fill=(190, 104, 30, 235), width=thickness + 2)
    d.line((start, end), fill=(239, 171, 58, 245), width=thickness)
    if start[1] == end[1]:
        y = start[1] - 1
        d.line(((start[0], y), (end[0], y)), fill=(255, 226, 135, 215), width=max(1, thickness // 2))
        length = end[0] - start[0]
        for fraction in (.28, .72):
            gx = start[0] + length * fraction
            d.line(((gx - 12, y), (gx + 12, y)), fill=(255, 248, 205, 240), width=1)
    else:
        x = start[0] - 1
        d.line(((x, start[1]), (x, end[1])), fill=(255, 226, 135, 190), width=max(1, thickness // 2))
    canvas.alpha_composite(overlay)


def rounded_panel(name, corner_size, body_box, radius, body_alpha, rails, cap_side="corner"):
    src = load(name)
    w, h = src.size
    result = base(src.size, mask_shape(src.size, "round", body_box, radius, body_alpha))
    for start, end, thickness in rails:
        metal_run(result, start, end, thickness)
    if cap_side == "corner":
        cw, ch = corner_size
        corner = metal_layer(src.crop((0, 0, cw, ch)))
        composite_metal(result, corner, (0, 0))
        composite_metal(result, ImageOps.mirror(corner), (w - cw, 0))
        composite_metal(result, ImageOps.flip(corner), (0, h - ch))
        composite_metal(result, ImageOps.flip(ImageOps.mirror(corner)), (w - cw, h - ch))
    else:
        cap = corner_size[0]
        piece = src.crop((0, 0, cap, h)) if cap_side == "left" else src.crop((w - cap, 0, w, h))
        piece = metal_layer(piece)
        if cap_side == "left":
            composite_metal(result, piece, (0, 0))
            composite_metal(result, ImageOps.mirror(piece), (w - cap, 0))
        else:
            composite_metal(result, ImageOps.mirror(piece), (0, 0))
            composite_metal(result, piece, (w - cap, 0))
    # The extracted corner may contain a bright pixel from the photographed scene at
    # (0,0). Clip the *whole* composition to the frame's rounded silhouette.
    clip = mask_shape(src.size, "round", (0, 0, w - 1, h - 1), radius, 255)
    result.putalpha(ImageChops.multiply(result.getchannel("A"), clip))
    return result


def symmetric_bar():
    src = load("bar_frame")
    w, h = src.size
    opening = bar_opening().getchannel("A")
    body_alpha = np.asarray(mirrored_alpha(src), np.float32) * (1 - np.asarray(opening, np.float32) / 255) * .72
    result = base(src.size, Image.fromarray(body_alpha.astype("uint8"), "L"))
    left = metal_layer(src.crop((0, 0, w // 2, h)), floor=40)
    allowed = ImageOps.invert(opening.crop((0, 0, w // 2, h)))
    left.putalpha(ImageChops.multiply(left.getchannel("A"), allowed))
    composite_metal(result, left, (0, 0), 0)
    composite_metal(result, ImageOps.mirror(left), (w // 2, 0), 0)
    return result


def normal_slot():
    name = "slot"
    src = load(name)
    w, h = src.size
    result = base(src.size, mask_shape(src.size, "round", (7, 7, w - 8, h - 8), 16, 110))
    # A quiet bronze border. The selected slot below keeps its own bright metal from the concept.
    d = ImageDraw.Draw(result)
    d.rounded_rectangle((8, 8, w - 9, h - 9), radius=16, outline=(115, 79, 39, 215), width=3)
    d.rounded_rectangle((12, 12, w - 13, h - 13), radius=13, outline=(180, 131, 65, 115), width=1)
    return result


def selected_slot():
    src = load("slot_active")
    w, h = src.size
    result = base(src.size, mask_shape(src.size, "round", (15, 9, w - 16, h - 10), 14, 95))
    metal = metal_layer(src, floor=20)
    # The source has warm pixels from the world in the lower corners. Confine the metal
    # to the actual border instead of admitting a rectangular patch around it.
    outer = mask_shape(src.size, "round", (8, 4, w - 9, h - 5), 21, 255)
    inner = mask_shape(src.size, "round", (27, 23, w - 28, h - 24), 10, 255)
    ring = ImageChops.subtract(outer, inner)
    metal.putalpha(ImageChops.multiply(metal.getchannel("A"), ring))
    glow = metal.copy()
    glow.putalpha(metal.getchannel("A").filter(ImageFilter.GaussianBlur(6)).point(lambda a: int(a * .38)))
    result.alpha_composite(glow)
    result.alpha_composite(metal)
    return result


def bar_opening():
    """Liquid reaches into the arch and lower point without covering the medallion."""
    size = load("bar_frame").size
    w, h = size
    mask = mask_shape(size, "poly",
                      [(24, 78), (w // 2, 50), (w - 24, 78),
                       (w - 24, h - 91), (w // 2, h - 110), (24, h - 91)],
                      opacity=255)
    result = Image.new("RGBA", size, (255, 255, 255, 0))
    result.putalpha(mask)
    return result


def rounded_pill():
    src = load("pill")
    w, h = src.size
    scale = 3
    canvas = Image.new("RGBA", (w * scale, h * scale), (0, 0, 0, 0))
    d = ImageDraw.Draw(canvas)
    box = (4 * scale, 4 * scale, (w - 5) * scale, (h - 5) * scale)
    d.rounded_rectangle(box, radius=(h // 2 - 5) * scale, fill=(*BODY, 167),
                        outline=(105, 58, 20, 220), width=7 * scale)
    d.rounded_rectangle(box, radius=(h // 2 - 5) * scale, outline=(218, 149, 49, 250), width=4 * scale)
    inner = (7 * scale, 7 * scale, (w - 8) * scale, (h - 8) * scale)
    d.arc(inner, 185, 355, fill=(255, 232, 144, 230), width=2 * scale)
    return canvas.resize(src.size, Image.Resampling.LANCZOS)


def round_ring():
    src = load("map_ring")
    w, h = src.size
    x = np.arange(w, dtype=np.float32)[None, :]
    y = np.arange(h, dtype=np.float32)[:, None]
    cx = (w - 1) / 2
    cy = (h - 1) / 2
    radius = np.hypot(x - cx, y - cy)
    # Three radial tones and a directional reflection retain the concept's metallic ring.
    ring_alpha = np.clip((radius - 309.5) * .9, 0, 1) * np.clip((319 - radius) * .9, 0, 1)
    angle = np.arctan2(y - cy, x - cx)
    shine = np.clip((np.cos(angle + .9) + 1) * .5, 0, 1)
    ridge = np.exp(-((radius - 313.0) / 1.8) ** 2)
    brightness = np.clip(.28 + .35 * shine + .35 * ridge, 0, 1)
    bronze = np.array((100, 54, 19), np.float32)
    gold = np.array((234, 160, 49), np.float32)
    rgb = bronze[None, None, :] + (gold - bronze)[None, None, :] * brightness[..., None]
    rgb += ridge[..., None] * shine[..., None] * np.array((18, 36, 56), np.float32)
    rgb = np.clip(rgb, 0, 255)
    rgba = np.dstack((rgb, ring_alpha * 255)).astype("uint8")
    result = Image.fromarray(rgba, "RGBA")
    # The concept has a knot only on the left. Keep that feature, with a hand bounded
    # mask so the unrelated grey dots below it cannot become part of the sprite.
    knot = metal_layer(src)
    mask = Image.new("L", (w, h), 0)
    ImageDraw.Draw(mask).polygon([(4, 346), (61, 265), (110, 340), (105, 428), (58, 454), (4, 414)], fill=255)
    mask = mask.filter(ImageFilter.GaussianBlur(1))
    ka = np.minimum(np.asarray(knot.getchannel("A")), np.asarray(mask))
    knot.putalpha(Image.fromarray(ka.astype("uint8"), "L"))
    result.alpha_composite(knot)
    # Keep invisible pixels black for image viewers and PNG compression.
    finished = np.asarray(result).copy()
    finished[finished[..., 3] == 0, :3] = 0
    return Image.fromarray(finished, "RGBA")


def boss_plate():
    src = load("boss_plate")
    w, h = src.size
    silhouette = [(4, 95), (58, 5), (w - 58, 5), (w - 4, 95), (w - 58, h - 4), (58, h - 4)]
    result = base(src.size, mask_shape(src.size, "poly", silhouette, opacity=166))
    metal_run(result, (116, 7), (w - 116, 7), 4)
    metal_run(result, (116, h - 7), (w - 116, h - 7), 4)
    left = metal_layer(src.crop((0, 0, 125, h)))
    composite_metal(result, left, (0, 0))
    composite_metal(result, ImageOps.mirror(left), (w - 125, 0))
    return result


def sprint_frame():
    src = load("sprint_frame")
    w, h = src.size
    result = base(src.size, mask_shape(src.size, "round", (108, 30, w - 109, 98), 18, 119))
    metal_run(result, (118, 35), (w - 118, 35), 4)
    metal_run(result, (118, 91), (w - 118, 91), 4)
    d = ImageDraw.Draw(result)
    d.rounded_rectangle((116, 48, w - 117, 79), radius=12, outline=(106, 55, 18, 240), width=7)
    d.rounded_rectangle((118, 50, w - 119, 77), radius=11, outline=(236, 163, 56, 242), width=4)
    d.arc((121, 51, w - 122, 76), 185, 345, fill=(255, 238, 160, 224), width=2)
    # Only the knot from the clean right cap is mirrored. The centre is redrawn as rails,
    # so the old screenshot's stamina fill and scenery cannot contaminate the frame.
    cap = metal_layer(src.crop((w - 125, 0, w, h)), floor=135, range_width=50)
    composite_metal(result, ImageOps.mirror(cap), (0, 0), .06)
    composite_metal(result, cap, (w - 125, 0), .06)
    return result


def sprint_opening():
    size = load("sprint_frame").size
    w, _ = size
    result = Image.new("RGBA", size, (255, 255, 255, 0))
    result.putalpha(mask_shape(size, "round", (122, 54, w - 123, 73), 8, 255))
    return result


def bar_variant(frame, opening, target_width, target_height):
    """Scale each ornament uniformly; repeat a straight middle row to change height."""
    source_w, source_h = frame.size
    k = target_width / source_w
    top_src, bottom_src, shaft_y = 115, 110, 300
    top_h = round(top_src * k)
    bottom_h = round(bottom_src * k)
    middle_h = target_height - top_h - bottom_h
    if middle_h <= 0:
        raise ValueError("bar height cannot hold both undistorted caps")

    def assemble(im):
        top = im.crop((0, 0, source_w, top_src)).resize((target_width, top_h), Image.Resampling.LANCZOS)
        bottom = im.crop((0, source_h - bottom_src, source_w, source_h)).resize((target_width, bottom_h), Image.Resampling.LANCZOS)
        row = im.crop((0, shaft_y, source_w, shaft_y + 1)).resize((target_width, 1), Image.Resampling.LANCZOS)
        result = Image.new("RGBA", (target_width, target_height))
        result.paste(top, (0, 0))
        for y in range(top_h, top_h + middle_h):
            result.paste(row, (0, y))
        result.paste(bottom, (0, top_h + middle_h))
        return result

    return assemble(frame), assemble(opening)


def bar_demo(made):
    width, height = 380, 510
    result = Image.new("RGBA", (width, height))
    specs = [("bar_frame_health", 19, (160, 25, 28), .97),
             ("bar_frame_small", 151, (195, 151, 58), .65),
             ("bar_frame_small", 260, (40, 148, 212), .88)]
    for name, x, color, fraction in specs:
        frame = made[name]
        mask = made[name + "_opening"].getchannel("A")
        w, h = frame.size
        liquid = Image.new("RGBA", (w, h))
        pix = np.zeros((h, w, 4), np.uint8)
        start = round(h * (1 - fraction))
        for y in range(start, h):
            shade = .68 + .34 * (1 - (y - start) / max(1, h - start))
            pix[y, :, :3] = np.clip(np.array(color) * shade, 0, 255)
            pix[y, :, 3] = 255
        liquid = Image.fromarray(pix, "RGBA")
        liquid.putalpha(Image.fromarray(np.minimum(np.asarray(liquid.getchannel("A")), np.asarray(mask)), "L"))
        y = height - h - 15
        result.alpha_composite(liquid, (x, y))
        result.alpha_composite(frame, (x, y))
    return result


def sprint_demo(frame, opening):
    w, h = frame.size
    result = Image.new("RGBA", (w, h + 64))
    d = ImageDraw.Draw(result)
    font_path = ROOT / "art/fonts/Cinzel-SemiBold.ttf"
    font = ImageFont.truetype(str(font_path), 38) if font_path.exists() else ImageFont.load_default()
    label = "V I G O R"
    box = d.textbbox((0, 0), label, font=font)
    tx = (w - (box[2] - box[0])) // 2
    d.text((tx, 0), label, font=font, fill=(245, 205, 123, 255), stroke_width=1, stroke_fill=(83, 48, 20, 255))
    for x in (tx - 30, tx + box[2] - box[0] + 30):
        d.polygon([(x, 16), (x + 7, 23), (x, 30), (x - 7, 23)], fill=(203, 143, 54, 255))
    fill = Image.new("RGBA", (w, h))
    p = np.zeros((h, w, 4), np.uint8)
    stop = round(122 + (w - 244) * .64)
    for y in range(54, 75):
        tone = (255, 232, 126) if y < 61 else (219, 148, 42)
        p[y, 122:stop, :3] = tone
        p[y, 122:stop, 3] = 255
    fill = Image.fromarray(p, "RGBA")
    fill.putalpha(Image.fromarray(np.minimum(np.asarray(fill.getchannel("A")), np.asarray(opening.getchannel("A"))), "L"))
    result.alpha_composite(fill, (0, 64))
    result.alpha_composite(frame, (0, 64))
    return result


def main():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    bar = symmetric_bar()
    opening = bar_opening()
    health, health_opening = bar_variant(bar, opening, 92, 452)
    small, small_opening = bar_variant(bar, opening, 76, 392)
    sprint = sprint_frame()
    sprint_mask = sprint_opening()
    made = {
        "card": rounded_panel("card", (125, 125), (2, 2, 819, 533), 24, 168,
                              [((124, 4), (698, 4), 4), ((124, 531), (698, 531), 4),
                               ((4, 123), (4, 413), 4), ((817, 123), (817, 413), 4)]),
        "plate": rounded_panel("plate", (130, 0), (25, 5, 1744, 232), 24, 150,
                               [((127, 7), (1643, 7), 5), ((127, 228), (1643, 228), 5)],
                               cap_side="right"),
        "boss_plate": boss_plate(),
        "sprint_frame": sprint,
        "sprint_frame_opening": sprint_mask,
        "bar_frame": bar,
        "bar_frame_opening": opening,
        "bar_frame_health": health,
        "bar_frame_health_opening": health_opening,
        "bar_frame_small": small,
        "bar_frame_small_opening": small_opening,
        "pill": rounded_pill(),
        "map_ring": round_ring(),
        "slot": normal_slot(),
        "slot_active": selected_slot(),
    }
    made["sprint_demo"] = sprint_demo(sprint, sprint_mask)
    made["vitals_demo"] = bar_demo(made)
    verify(made)
    for name, im in made.items():
        im.save(OUTPUT / (name + ".png"))
        print(f"{name}: {im.width}x{im.height}")
    preview(made)


def verify(made):
    for name in ("card", "plate", "boss_plate", "slot", "slot_active"):
        im = made[name]
        w, h = im.size
        corners = ((0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1))
        if any(im.getpixel(point)[3] > 1 for point in corners):
            raise ValueError(name + ": rectangular background in a transparent corner")
        if im.getpixel((w // 2, h // 2))[3] >= 255:
            raise ValueError(name + ": opaque centre; panel must show the world beneath")
    for name, size in (("bar_frame_health", (92, 452)), ("bar_frame_small", (76, 392))):
        if made[name].size != size or made[name + "_opening"].size != size:
            raise ValueError(name + ": wrong size or opening mask")


def preview(made):
    """One portable review page, with both versions embedded and a background switch."""
    labels = {
        "card": "Cartão de interação",
        "plate": "Placa da hotbar",
        "boss_plate": "Placa do chefe",
        "sprint_frame": "Barra de vigor",
        "bar_frame": "Moldura de HP, vigor e eitr",
        "pill": "Etiqueta do minimapa",
        "map_ring": "Anel do minimapa",
        "slot": "Slot normal",
        "slot_active": "Slot selecionado",
    }

    concept_dir = Path.home() / "GenesisUI-Concept"
    sprint_reference = Image.open(concept_dir / "ConceptArt (5).png").crop((570, 686, 1105, 775))
    vitals_reference = Image.open(concept_dir / "ConceptArt (4).png").crop((20, 516, 250, 812))
    sprint_reference.save(OUTPUT / "sprint_reference.png")
    vitals_reference.save(OUTPUT / "vitals_reference.png")

    def uri(path):
        return "data:image/png;base64," + base64.b64encode(path.read_bytes()).decode("ascii")

    cards = []
    for name in labels:
        before = uri(SOURCE / (name + ".png"))
        after = uri(OUTPUT / (name + ".png"))
        cards.append(f"""<section><h2>{html.escape(labels[name])}</h2><div class="pair">
          <figure><figcaption>Recorte atual</figcaption><div class="stage"><img src="{before}"></div></figure>
          <figure><figcaption>Estudo revisado</figcaption><div class="stage"><img src="{after}"></div></figure>
        </div></section>""")
    compositions = []
    for title, reference, study in (("Barra central · Concept 5", "sprint_reference", "sprint_demo"),
                                    ("Barras verticais · Concept 4", "vitals_reference", "vitals_demo")):
        original = uri(OUTPUT / (reference + ".png"))
        revised = uri(OUTPUT / (study + ".png"))
        compositions.append(f"""<section><h2>{title}</h2><div class="pair">
          <figure><figcaption>Trecho da concept</figcaption><div class="stage tall"><img src="{original}"></div></figure>
          <figure><figcaption>Composição proposta</figcaption><div class="stage tall"><img src="{revised}"></div></figure>
        </div></section>""")
    page = """<!doctype html><html lang="pt-BR"><meta charset="utf-8">
    <meta name="viewport" content="width=device-width,initial-scale=1">
    <title>GenesisUI · revisão das peças da concept</title>
    <style>
    :root {color-scheme:dark;font-family:system-ui;background:#111210;color:#ede8dd}
    body {max-width:1300px;margin:0 auto;padding:24px 4vw 80px}
    h1{font-size:28px;margin-bottom:8px}h2{font-size:19px;margin:0 0 16px;color:#facf72}
    p{line-height:1.55;max-width:950px;color:#d5d2ca}section{margin:34px 0;padding:24px;border:1px solid #705b3f;border-radius:10px;background:#171917}
    .pair{display:grid;grid-template-columns:1fr 1fr;gap:18px}figure{min-width:0;margin:0}figcaption{margin-bottom:8px;color:#d09d4b}
    .stage{height:260px;display:flex;align-items:center;justify-content:center;padding:14px;overflow:hidden;border-radius:6px;background:linear-gradient(135deg,#323c30,#121514 50%,#524d3a)}
    .stage.tall{height:360px}
    .stage img{display:block;max-width:100%;max-height:100%;object-fit:contain}
    body.light .stage{background:linear-gradient(135deg,#b2ab94,#626b59 60%,#c0ae7d)}
    button{padding:9px 14px;border:1px solid #b8863e;border-radius:6px;background:#211d15;color:#facf72;cursor:pointer}
    @media(max-width:700px){.pair{grid-template-columns:1fr}.stage{height:220px}}
    </style><h1>Peças da concept · estudo de limpeza</h1>
    <p>Segunda revisão: fundo escuro translúcido dentro do contorno real, cantos de fora
    transparentes, metal com sombra bronze e reflexos claros. A barra central mostra o título,
    preenchimento e nós da Concept 5. As barras verticais usam adornos redimensionados na
    mesma proporção e só o trecho reto é repetido. Prévia visual; ainda não integrada ao jogo.</p>
    <button onclick="document.body.classList.toggle('light')">Alternar fundo</button>
    """ + "".join(compositions) + "".join(cards) + "</html>"
    (OUTPUT / "preview.html").write_text(page, encoding="utf-8")


if __name__ == "__main__":
    main()
