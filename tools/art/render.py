"""Rasterizes art/src/*.svg (our own vector sources, docs/DECISIONS.md D-009).

    tools/.venv/bin/python tools/art/render.py

Outputs:
  icon.png                     package icon, exactly 256x256 (Hexium rule)
  art/out/<name>.png           UI sprites at 2x their design size
  art/out/sprites.json         name, file, design size and 9-slice border per sprite,
                               read by the plugin with JsonUtility (D-013)

Setup once:  python3 -m venv tools/.venv && tools/.venv/bin/pip install -r tools/art/requirements.txt
"""
import json
import os
import sys

import cairosvg

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "art", "out")
SCALE = 2  # sprites are rendered at 2x and loaded with pixelsPerUnit = 100 * SCALE

# name, source, design width, design height, border (left, bottom, right, top) in design px
SPRITES = [
    ("bar_frame", "art/src/bar_frame.svg", 48, 160, (10, 14, 10, 26)),
    ("bar_fill", "art/src/bar_fill.svg", 16, 64, (0, 0, 0, 0)),
    ("medallion", "art/src/medallion.svg", 64, 64, (0, 0, 0, 0)),
    ("slot", "art/src/slot.svg", 56, 56, (12, 12, 12, 12)),
    ("slot_active", "art/src/slot_active.svg", 56, 56, (12, 12, 12, 12)),
    ("tile", "art/src/tile.svg", 60, 60, (0, 0, 0, 0)),
    ("plate", "art/src/plate.svg", 96, 72, (24, 16, 24, 16)),
]


def main() -> int:
    cairosvg.svg2png(url=os.path.join(ROOT, "art/src/logo.svg"), write_to=os.path.join(ROOT, "icon.png"),
                     output_width=256, output_height=256)
    print("art/src/logo.svg -> icon.png (256x256)")

    os.makedirs(OUT, exist_ok=True)
    manifest = {"scale": SCALE, "sprites": []}
    for name, src, w, h, border in SPRITES:
        out = os.path.join(OUT, name + ".png")
        cairosvg.svg2png(url=os.path.join(ROOT, src), write_to=out, output_width=w * SCALE, output_height=h * SCALE)
        manifest["sprites"].append({"name": name, "file": name + ".png", "width": w, "height": h,
                                    "borderLeft": border[0], "borderBottom": border[1],
                                    "borderRight": border[2], "borderTop": border[3]})
        print(f"{src} -> art/out/{name}.png ({w * SCALE}x{h * SCALE})")

    with open(os.path.join(OUT, "sprites.json"), "w") as f:
        json.dump(manifest, f, indent=2)
    return 0


if __name__ == "__main__":
    sys.exit(main())
