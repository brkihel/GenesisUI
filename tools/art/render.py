"""Rasterizes art/src/*.svg (our own vector sources, docs/DECISIONS.md D-009).

    tools/.venv/bin/python tools/art/render.py

Today it renders the package icon only; the ornament atlas joins it in F2.
Setup once:  python3 -m venv tools/.venv && tools/.venv/bin/pip install -r tools/art/requirements.txt
"""
import os
import sys

import cairosvg

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))

TARGETS = [
    # source, output, width, height
    ("art/src/logo.svg", "icon.png", 256, 256),  # Hexium requires exactly 256x256
]


def main() -> int:
    for src, out, w, h in TARGETS:
        cairosvg.svg2png(url=os.path.join(ROOT, src), write_to=os.path.join(ROOT, out), output_width=w, output_height=h)
        print(f"{src} -> {out} ({w}x{h})")
    return 0


if __name__ == "__main__":
    sys.exit(main())
