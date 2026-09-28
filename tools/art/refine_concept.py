"""Build review candidates from the concept cutouts without touching shipped sprites.

    tools/.venv/bin/python tools/art/refine_concept.py

The cutouts preserve the concept's knots, but a colour threshold alone also preserves
parts of the photographed world. This pass uses one good cap/corner per symmetric part,
normalizes its gold, and draws the straight runs and circular ring geometrically. The
minimap's single left knot is deliberately asymmetric. Output is in dist/art-review/.
"""
import base64
import html
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageOps


ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "art/src/concept"
OUTPUT = ROOT / "dist/art-review"
BODY = np.array((14, 13, 11), dtype=np.float32)
GOLD = np.array((235, 171, 66), dtype=np.float32)


def load(name):
    return Image.open(SOURCE / f"{name}.png").convert("RGBA")


def gold_finish(im):
    """Remove baked scene lighting while retaining the extracted ornament's fine strokes."""
    a = np.asarray(im, np.float32)
    rgb = a[..., :3]
    brightness = (rgb[..., 0] * .45 + rgb[..., 1] * .45 + rgb[..., 2] * .10)
    warm = np.clip((rgb[..., 0] - rgb[..., 2] - 9) / 28, 0, 1)
    ink = np.clip((brightness - 24) / 125, 0, 1) * warm
    # The highlight remains soft, but every cap has the same colour scale.
    colour = BODY + (GOLD - BODY) * ink[..., None]
    return Image.fromarray(np.dstack((colour, a[..., 3])).astype("uint8"), "RGBA")


def base(im, keep_alpha=True):
    background = Image.new("RGBA", im.size, (*BODY.astype(int), 255))
    if keep_alpha:
        background.putalpha(im.getchannel("A"))
    return background


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


def frame_runs(im, top, bottom, left, right, width=3, sides=False):
    draw = ImageDraw.Draw(im)
    ink = (188, 126, 48, 255)
    draw.line((left, top, right, top), fill=ink, width=width)
    draw.line((left, bottom, right, bottom), fill=ink, width=width)
    if sides:
        draw.line((left, top, left, bottom), fill=ink, width=width)
        draw.line((right, top, right, bottom), fill=ink, width=width)


def symmetric_corners(name, xsize, ysize, top, bottom, left, right, width=3):
    src = load(name)
    w, h = src.size
    corner = gold_finish(src.crop((0, 0, xsize, ysize)))
    result = base(src)
    frame_runs(result, top, bottom, left, right, width, sides=True)
    result.paste(corner, (0, 0))
    result.paste(ImageOps.mirror(corner), (w - xsize, 0))
    result.paste(ImageOps.flip(corner), (0, h - ysize))
    result.paste(ImageOps.flip(ImageOps.mirror(corner)), (w - xsize, h - ysize))
    result.putalpha(src.getchannel("A"))
    return result


def symmetric_caps(name, side, cap, top, bottom, width=3, opening=False):
    src = load(name)
    w, h = src.size
    cap_image = src.crop((0, 0, cap, h)) if side == "left" else src.crop((w - cap, 0, w, h))
    cap_image = gold_finish(cap_image)
    result = base(src)
    frame_runs(result, top, bottom, cap - 4, w - cap + 4, width)
    if side == "left":
        result.paste(cap_image, (0, 0))
        result.paste(ImageOps.mirror(cap_image), (w - cap, 0))
    else:
        result.paste(ImageOps.mirror(cap_image), (0, 0))
        result.paste(cap_image, (w - cap, 0))
    if opening:
        mask = load(name + "_opening").getchannel("A")
        alpha = Image.fromarray(np.minimum(np.asarray(src.getchannel("A")), 255 - np.asarray(mask)).astype("uint8"), "L")
        result.putalpha(alpha)
    else:
        result.putalpha(mirrored_alpha(src, side))
    return result


def symmetric_bar():
    src = load("bar_frame")
    w, h = src.size
    left = gold_finish(src.crop((0, 0, w // 2, h)))
    result = Image.new("RGBA", (w, h))
    result.paste(left, (0, 0))
    result.paste(ImageOps.mirror(left), (w // 2, 0))
    # Geometry remains the mask source; pixel mirroring only fixes unequal ornament lines.
    result.putalpha(mirrored_alpha(src))
    return result


def symmetric_slot(name):
    src = load(name)
    w, h = src.size
    quarter = gold_finish(src.crop((0, 0, w // 2, h // 2)))
    result = Image.new("RGBA", (w, h))
    result.paste(quarter, (0, 0))
    result.paste(ImageOps.mirror(quarter), (w - quarter.width, 0))
    result.paste(ImageOps.flip(quarter), (0, h - quarter.height))
    result.paste(ImageOps.flip(ImageOps.mirror(quarter)), (w - quarter.width, h - quarter.height))
    return result


def rounded_pill():
    src = load("pill")
    w, h = src.size
    scale = 3
    canvas = Image.new("RGBA", (w * scale, h * scale), (0, 0, 0, 0))
    d = ImageDraw.Draw(canvas)
    box = (4 * scale, 4 * scale, (w - 5) * scale, (h - 5) * scale)
    d.rounded_rectangle(box, radius=(h // 2 - 5) * scale, fill=(14, 13, 11, 250), outline=(190, 128, 49), width=3 * scale)
    return canvas.resize(src.size, Image.Resampling.LANCZOS)


def round_ring():
    src = load("map_ring")
    w, h = src.size
    x = np.arange(w, dtype=np.float32)[None, :]
    y = np.arange(h, dtype=np.float32)[:, None]
    cx = (w - 1) / 2
    cy = (h - 1) / 2
    radius = np.hypot(x - cx, y - cy)
    # The extracted east sector put the ring at radius ~316 px. Analytical geometry
    # removes its four rotation seams and the photographed colours beneath the line.
    ring_alpha = np.clip((radius - 310) * .8, 0, 1) * np.clip((318 - radius) * .8, 0, 1)
    light = np.clip(.72 + .18 * (1 - (y / h)), .65, .95)
    rgb = np.broadcast_to(GOLD[None, None, :] * light[..., None], (h, w, 3)).copy()
    rgba = np.dstack((rgb, ring_alpha * 255)).astype("uint8")
    result = Image.fromarray(rgba, "RGBA")
    # The concept has a knot only on the left. Keep that feature, with a hand bounded
    # mask so the unrelated grey dots below it cannot become part of the sprite.
    knot = gold_finish(src)
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


def main():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    made = {
        "card": symmetric_corners("card", 125, 125, 4, 531, 4, 817),
        "plate": symmetric_caps("plate", "right", 130, 5, 228),
        "boss_plate": symmetric_caps("boss_plate", "left", 125, 6, 179),
        "sprint_frame": symmetric_caps("sprint_frame", "right", 125, 35, 91, opening=True),
        "bar_frame": symmetric_bar(),
        "pill": rounded_pill(),
        "map_ring": round_ring(),
        "slot": symmetric_slot("slot"),
        "slot_active": symmetric_slot("slot_active"),
    }
    for name, im in made.items():
        im.save(OUTPUT / (name + ".png"))
        print(f"{name}: {im.width}x{im.height}")
    preview(made)


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

    def uri(path):
        return "data:image/png;base64," + base64.b64encode(path.read_bytes()).decode("ascii")

    cards = []
    for name in made:
        before = uri(SOURCE / (name + ".png"))
        after = uri(OUTPUT / (name + ".png"))
        cards.append(f"""<section><h2>{html.escape(labels[name])}</h2><div class="pair">
          <figure><figcaption>Recorte atual</figcaption><div class="stage"><img src="{before}"></div></figure>
          <figure><figcaption>Estudo revisado</figcaption><div class="stage"><img src="{after}"></div></figure>
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
    .stage img{display:block;max-width:100%;max-height:100%;object-fit:contain}
    body.light .stage{background:linear-gradient(135deg,#b2ab94,#626b59 60%,#c0ae7d)}
    button{padding:9px 14px;border:1px solid #b8863e;border-radius:6px;background:#211d15;color:#facf72;cursor:pointer}
    @media(max-width:700px){.pair{grid-template-columns:1fr}.stage{height:220px}}
    </style><h1>Peças da concept · estudo de limpeza</h1>
    <p>Comparação dos recortes atuais com uma primeira revisão. Ornamentos das concepts,
    espelhados apenas nas peças simétricas; traços longos e anel reconstruídos para remover
    emendas, fundo do cenário e variação de iluminação. O nó esquerdo do minimapa permanece
    assimétrico de propósito. É uma prévia: ainda não está integrada ao jogo.</p>
    <button onclick="document.body.classList.toggle('light')">Alternar fundo</button>
    """ + "".join(cards) + "</html>"
    (OUTPUT / "preview.html").write_text(page, encoding="utf-8")


if __name__ == "__main__":
    main()
