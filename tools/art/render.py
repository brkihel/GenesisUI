"""Builds the art: shapes (SVG) and patterns into PNGs plus one manifest.

    tools/.venv/bin/python tools/art/render.py

Outputs:
  icon.png                  package icon, exactly 256x256 (Hexium rule)
  art/out/<name>.png        sprites at 2x design size (patterns at their pixel size)
  art/out/sprites.json      name, file, size, 9-slice border, content insets, wrap;
                            read by the plugin through StrictJson (D-018)

Setup once:
  python3 -m venv tools/.venv && tools/.venv/bin/pip install -r tools/art/requirements.txt
"""
import json
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))

import cairosvg
from PIL import Image

import patterns
import shapes

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SCALE = 2  # sprites are rendered at 2x and loaded with pixelsPerUnit = 100 * SCALE
HAND_WRITTEN = {"bar_fill": (16, 128)}  # art/src textures that are not shapes (a white gradient tinted in game)


def entry(name, w, h, border, content=None, wrap="clamp"):
    e = {"name": name, "file": name + ".png", "width": w, "height": h,
         "borderLeft": border[0], "borderBottom": border[1], "borderRight": border[2], "borderTop": border[3]}
    if wrap != "clamp":
        e["wrap"] = wrap
    if content is not None:
        e.update({"contentLeft": content[0], "contentBottom": content[1], "contentRight": content[2], "contentTop": content[3]})
    return e


def main() -> int:
    shapes.main()
    cairosvg.svg2png(url=os.path.join(ROOT, "art/src/logo.svg"), write_to=os.path.join(ROOT, "icon.png"),
                     output_width=256, output_height=256)

    out = os.path.join(ROOT, "art", "out")
    os.makedirs(out, exist_ok=True)
    for f in os.listdir(out):
        os.remove(os.path.join(out, f))  # the folder only ever holds what this build wrote
    manifest = {"scale": SCALE, "sprites": []}

    sources = {name: (w, h, border, content) for name, (w, h, border, content) in shapes.SPRITES.items()}
    sources.update({name: (w, h, (0, 0, 0, 0), None) for name, (w, h) in HAND_WRITTEN.items()})
    for name, (w, h, border, content) in sources.items():
        cairosvg.svg2png(url=os.path.join(ROOT, "art", "src", name + ".svg"), write_to=os.path.join(out, name + ".png"),
                         output_width=w * SCALE, output_height=h * SCALE)
        manifest["sprites"].append(entry(name, w, h, border, content))

    # Pieces cut from Diego's concept images (D-026) replace the generated sprite of the same name.
    concept = os.path.join(ROOT, "art", "src", "concept")
    pieces_path = os.path.join(concept, "pieces.json")
    if os.path.exists(pieces_path):
        with open(pieces_path) as f:
            pieces = json.load(f)
        manifest["sprites"] = [e for e in manifest["sprites"] if e["name"] not in pieces]
        for name, meta in sorted(pieces.items()):
            img = Image.open(os.path.join(concept, name + ".png"))
            img.save(os.path.join(out, name + ".png"))
            manifest["sprites"].append(entry(name, img.width // SCALE, img.height // SCALE, meta["border"], meta.get("content")))

    patterns.main(out)
    for name, (pw, ph, wrap, border, _) in patterns.PATTERNS.items():
        manifest["sprites"].append(entry(name, pw // SCALE, ph // SCALE, border or (0, 0, 0, 0), wrap=wrap))

    with open(os.path.join(out, "sprites.json"), "w") as f:
        json.dump(manifest, f, indent=2)
    print(f"{len(manifest['sprites'])} sprites -> art/out")
    return 0


if __name__ == "__main__":
    sys.exit(main())
