"""Builds every art style: shapes (SVG) and patterns into PNGs and one manifest per style.

    tools/.venv/bin/python tools/art/render.py

Outputs:
  icon.png                         package icon, exactly 256x256 (Hexium rule)
  art/out/<style>/<name>.png       sprites at 2x design size (patterns at their pixel size)
  art/out/<style>/sprites.json     name, file, size, 9-slice border, content insets, wrap;
                                   read by the plugin through StrictJson (D-018)

Styles share sprite names (D-022). Setup once:
  python3 -m venv tools/.venv && tools/.venv/bin/pip install -r tools/art/requirements.txt
"""
import json
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))

import cairosvg
from PIL import Image

import carved_style
import patterns
import shapes

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SCALE = 2  # sprites are rendered at 2x and loaded with pixelsPerUnit = 100 * SCALE
COMMON = {"bar_fill": ("art/src/common/bar_fill.svg", 16, 128)}  # hand-written textures shared by all styles


def entry(name, w, h, border, content=None, wrap="clamp"):
    e = {"name": name, "file": name + ".png", "width": w, "height": h,
         "borderLeft": border[0], "borderBottom": border[1], "borderRight": border[2], "borderTop": border[3]}
    if wrap != "clamp":
        e["wrap"] = wrap
    if content is not None:
        e.update({"contentLeft": content[0], "contentBottom": content[1], "contentRight": content[2], "contentTop": content[3]})
    return e


def build_style(style, module):
    out = os.path.join(ROOT, "art", "out", style)
    os.makedirs(out, exist_ok=True)
    for f in os.listdir(out):
        os.remove(os.path.join(out, f))  # a style folder only ever holds what this build wrote
    manifest = {"scale": SCALE, "sprites": []}

    for name, (w, h, border, content, grain) in module.SPRITES.items():
        png = os.path.join(out, name + ".png")
        cairosvg.svg2png(url=os.path.join(ROOT, "art", "src", style, name + ".svg"), write_to=png,
                         output_width=w * SCALE, output_height=h * SCALE)
        if grain:
            carved_style.grain(Image.open(png), vertical=(grain == "v")).save(png)
        manifest["sprites"].append(entry(name, w, h, border, content))

    for name, (src, w, h) in COMMON.items():
        cairosvg.svg2png(url=os.path.join(ROOT, src), write_to=os.path.join(out, name + ".png"),
                         output_width=w * SCALE, output_height=h * SCALE)
        manifest["sprites"].append(entry(name, w, h, (0, 0, 0, 0)))

    patterns.main(out)
    for name, (pw, ph, wrap, border, _) in patterns.PATTERNS.items():
        manifest["sprites"].append(entry(name, pw // SCALE, ph // SCALE, border or (0, 0, 0, 0), wrap=wrap))

    with open(os.path.join(out, "sprites.json"), "w") as f:
        json.dump(manifest, f, indent=2)
    print(f"{style}: {len(manifest['sprites'])} sprites -> art/out/{style}")


def main() -> int:
    shapes.main()
    cairosvg.svg2png(url=os.path.join(ROOT, "art/src/logo.svg"), write_to=os.path.join(ROOT, "icon.png"),
                     output_width=256, output_height=256)
    for style, module in shapes.STYLES.items():
        build_style(style, module)
    return 0


if __name__ == "__main__":
    sys.exit(main())
