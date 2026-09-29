"""Contact sheet of art/src/sheets on dark and light backdrops (AGENTS.md §2b quality gate).

    tools/.venv/bin/python tools/art/sheets_preview.py [output.png] [zoom]

Each piece is drawn at its design size times `zoom` (default 1 = the size it has on a
1920x1080 screen at GUI scale 1) over a dark and a light swatch, with its name.
"""
import json
import os
import sys

from PIL import Image, ImageDraw

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
DIR = os.path.join(ROOT, "art", "src", "sheets")


def main(out, zoom):
    meta = json.load(open(os.path.join(DIR, "pieces.json")))
    tiles = []
    for name in sorted(meta):
        im = Image.open(os.path.join(DIR, name + ".png")).convert("RGBA")
        w, h = max(1, int(im.width / 2 * zoom)), max(1, int(im.height / 2 * zoom))
        im = im.convert("RGBa").resize((w, h), Image.LANCZOS).convert("RGBA")
        cell = Image.new("RGBA", (2 * w + 30, h + 30), (30, 30, 30, 255))
        for i, bg in enumerate(((22, 26, 20), (205, 205, 200))):
            sw = Image.new("RGBA", (w + 10, h + 10), bg + (255,))
            sw.alpha_composite(im, (5, 5))
            cell.alpha_composite(sw, (i * (w + 10) + 5, 20))
        ImageDraw.Draw(cell).text((5, 4), name, fill=(240, 240, 240, 255))
        tiles.append(cell)
    W = 1800
    rows, row, x, rh = [], [], 0, 0
    for t in tiles:
        if x + t.width > W and row:
            rows.append((row, rh)); row, x, rh = [], 0, 0
        row.append(t); x += t.width + 6; rh = max(rh, t.height)
    rows.append((row, rh))
    sheet = Image.new("RGBA", (W, sum(r[1] + 6 for r in rows)), (30, 30, 30, 255))
    y = 0
    for row, rh in rows:
        x = 0
        for t in row:
            sheet.alpha_composite(t, (x, y)); x += t.width + 6
        y += rh + 6
    sheet.save(out)
    print("preview written to", out)


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "dist", "sheets-preview.png"),
         float(sys.argv[2]) if len(sys.argv) > 2 else 1.0)
